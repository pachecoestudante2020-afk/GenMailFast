using GenMailFast.Dedupe;
using GenMailFast.Generation;
using GenMailFast.IO;
using GenMailFast.Models;
using GenMailFast.Reports;
using System.Text;

if (args.Length == 0)
{
    Console.WriteLine("Usage: --input <path> --domain <domain> [--out output] [--dedupe dedupe/dedupe.hashes.gz]");
    return;
}

AppOptions options = ParseArgs(args);
RunStats stats = new()
{
    StartedAt = DateTimeOffset.UtcNow,
    InputPath = options.InputPath,
    Domain = EmailBuilder.ValidateAndNormalizeDomain(options.Domain),
    Rules = string.Join(',', options.Rules),
    DedupeFile = options.DedupePath,
    CompressOutput = options.CompressOutput,
    RowsPerFile = options.RowsPerFile
};

CompressedHashDedupeStore dedupe = new(options.DedupePath);
dedupe.Load();
stats.DedupeEntriesBefore = dedupe.Hashes.Count;

long lastPrintLine = 0;
DateTime lastPrintAt = DateTime.UtcNow;
using SplitEmailWriter writer = new(options.OutputFolder, options.RowsPerFile, options.CompressOutput);
using StreamReader reader = new(options.InputPath, Encoding.UTF8, true, 4 * 1024 * 1024);

string? line;
while ((line = reader.ReadLine()) is not null)
{
    stats.InputLinesRead++;
    string trimmed = line.Trim();
    if (trimmed.Length == 0) continue;

    HashSet<string> candidates = [];
    if (DirectUsernameDetector.IsDirectUsername(trimmed))
    {
        candidates.Add(trimmed.ToLowerInvariant());
    }
    else
    {
        string normalized = NameNormalizer.Normalize(trimmed);
        string[] tokens = NameNormalizer.Tokenize(normalized);
        foreach (string c in UsernameGenerator.Generate(tokens, options.Rules)) candidates.Add(c);
    }

    foreach (string candidate in candidates)
    {
        stats.CandidatesGenerated++;
        if (!QualityFilter.IsValidUsername(candidate))
        {
            stats.QualityRejected++;
            continue;
        }

        string email = EmailBuilder.Build(candidate, stats.Domain);
        if (!dedupe.TryAdd(email))
        {
            stats.DuplicatesSkipped++;
            continue;
        }

        writer.WriteLine(email);
        stats.EmailsWritten++;
    }

    if (stats.InputLinesRead - lastPrintLine >= 100000 || (DateTime.UtcNow - lastPrintAt).TotalSeconds >= 2)
    {
        Console.WriteLine($"Read: {stats.InputLinesRead} | Written: {stats.EmailsWritten} | Duplicates: {stats.DuplicatesSkipped} | Rejected: {stats.QualityRejected}");
        lastPrintLine = stats.InputLinesRead;
        lastPrintAt = DateTime.UtcNow;
    }
}

dedupe.SaveAtomic();
stats.DedupeEntriesAfter = dedupe.Hashes.Count;
stats.OutputFilesCreated = writer.FilesCreated;
stats.FinishedAt = DateTimeOffset.UtcNow;
SummaryWriter.Write(options.OutputFolder, stats);
Console.WriteLine("Done.");

static AppOptions ParseArgs(string[] args)
{
    Dictionary<string, string> map = [];
    for (int i = 0; i < args.Length; i++)
    {
        string key = args[i];
        if (!key.StartsWith("--", StringComparison.Ordinal)) continue;
        string value = i + 1 < args.Length ? args[++i] : string.Empty;
        map[key] = value;
    }

    if (!map.TryGetValue("--input", out string? input) || string.IsNullOrWhiteSpace(input))
        throw new ArgumentException("--input is required");
    if (!map.TryGetValue("--domain", out string? domain) || string.IsNullOrWhiteSpace(domain))
        throw new ArgumentException("--domain is required");

    return new AppOptions
    {
        InputPath = input,
        Domain = domain,
        OutputFolder = map.GetValueOrDefault("--out", "output"),
        DedupePath = map.GetValueOrDefault("--dedupe", Path.Combine("dedupe", "dedupe.hashes.gz")),
        RowsPerFile = int.TryParse(map.GetValueOrDefault("--rows-per-file", "50000"), out int rows) ? rows : 50000,
        CompressOutput = bool.TryParse(map.GetValueOrDefault("--compress-output", "false"), out bool c) && c,
        Rules = map.TryGetValue("--rules", out string? rules)
            ? rules.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : ["firstlast", "first.dot.last", "all", "all.dot"],
        NumbersMode = map.GetValueOrDefault("--numbers", "none"),
        NumberRange = map.GetValueOrDefault("--number-range")
    };
}
