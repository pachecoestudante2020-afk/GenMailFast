namespace GenMailFast.Models;

public sealed class AppOptions
{
    public required string InputPath { get; init; }
    public required string Domain { get; init; }
    public string OutputFolder { get; init; } = "output";
    public string DedupePath { get; init; } = Path.Combine("dedupe", "dedupe.hashes.gz");
    public int RowsPerFile { get; init; } = 50000;
    public bool CompressOutput { get; init; }
    public List<string> Rules { get; init; } = ["firstlast", "first.dot.last", "all", "all.dot"];
    public string NumbersMode { get; init; } = "none";
    public string? NumberRange { get; init; }
}
