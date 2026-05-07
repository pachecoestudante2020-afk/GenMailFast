using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

public class NameCombinerSuperFast
{
    private readonly string fileA;
    private readonly string fileB;
    private readonly string emailDomain;
    private readonly string stateDir;
    private readonly string progressFile;

    public readonly List<string> namesA;
    public readonly List<string> namesB;
    public readonly string[] formats = ["ab", "a-b", "a.b", "a_b"];

    public readonly long totalPairs;
    public readonly long totalCombinations;

    private sealed class ProgressData
    {
        public long current_pos { get; set; }
        public string last_update { get; set; } = string.Empty;
        public long total_done { get; set; }
        public long total_possible { get; set; }
        public string email_domain { get; set; } = string.Empty;
    }

    public NameCombinerSuperFast(
        string fileA = "a.txt",
        string fileB = "b.txt",
        string emailDomain = "",
        string stateDir = "state")
    {
        this.fileA = fileA;
        this.fileB = fileB;
        this.emailDomain = emailDomain;
        this.stateDir = stateDir;
        progressFile = Path.Combine(stateDir, "progress.json");

        Directory.CreateDirectory(stateDir);

        namesA = ReadNamesFast(fileA);
        namesB = ReadNamesFast(fileB);

        totalPairs = (long)namesA.Count * namesB.Count;
        totalCombinations = totalPairs * 4;

        Console.WriteLine($"\n{new string('=', 60)}");
        Console.WriteLine("THÔNG TIN HỆ THỐNG");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"File A: {namesA.Count:N0} tên");
        Console.WriteLine($"File B: {namesB.Count:N0} tên");
        Console.WriteLine($"Đuôi email: {(string.IsNullOrWhiteSpace(emailDomain) ? "KHÔNG DÙNG" : emailDomain)}");
        Console.WriteLine($"Số cặp tên: {totalPairs:N0}");
        Console.WriteLine($"Tổng tổ hợp (4 định dạng): {totalCombinations:N0}");
        if (totalCombinations > 0)
        {
            var ratio = (200_000_000d / totalCombinations) * 100;
            Console.WriteLine($"Tỉ lệ 200tr dòng so với tổng: {ratio:F6}%");
        }
        Console.WriteLine(new string('=', 60));
    }

    public List<string> ReadNamesFast(string filename)
    {
        var names = new List<string>();
        try
        {
            if (!File.Exists(filename)) return names;

            using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20, FileOptions.SequentialScan);
            if (fs.Length == 0) return names;

            using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1 << 20);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                names.Add(line.Trim());
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Lỗi đọc file {filename}: {e.Message}");
        }

        return names;
    }

    public string BuildStringFast(long idxA, long idxB, int formatIdx)
    {
        var nameA = namesA[(int)idxA];
        var nameB = namesB[(int)idxB];

        var combined = formats[formatIdx] switch
        {
            "ab" => $"{nameA}{nameB}",
            "a-b" => $"{nameA}-{nameB}",
            "a.b" => $"{nameA}.{nameB}",
            "a_b" => $"{nameA}_{nameB}",
            _ => $"{nameA}{nameB}"
        };

        if (!string.IsNullOrWhiteSpace(emailDomain))
        {
            var clean = combined.Replace(" ", "").Replace("--", "-").Replace("..", ".").Replace("__", "_");
            return $"{clean}{emailDomain}";
        }

        return combined;
    }

    public long LoadProgress()
    {
        if (!File.Exists(progressFile)) return 0;

        try
        {
            var json = File.ReadAllText(progressFile, Encoding.UTF8);
            var data = JsonSerializer.Deserialize<ProgressData>(json);
            return data?.current_pos ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    public void SaveProgress(long position)
    {
        var data = new ProgressData
        {
            current_pos = position,
            last_update = DateTime.Now.ToString("O", CultureInfo.InvariantCulture),
            total_done = position,
            total_possible = totalCombinations,
            email_domain = emailDomain
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(progressFile, json, Encoding.UTF8);
    }

    public string MakeOutputName(int? batchIndex = null, int? totalBatches = null, string outputDir = "output")
    {
        Directory.CreateDirectory(outputDir);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var domainClean = "nodomain";
        if (!string.IsNullOrWhiteSpace(emailDomain))
        {
            domainClean = emailDomain.Replace("@", "").Replace(".", "_");
        }

        string filename;
        if (batchIndex.HasValue && totalBatches.HasValue)
            filename = $"ket_qua_{domainClean}_{timestamp}_part_{batchIndex.Value:D3}_of_{totalBatches.Value:D3}.txt";
        else if (batchIndex.HasValue)
            filename = $"ket_qua_{domainClean}_{timestamp}_part_{batchIndex.Value:D3}.txt";
        else
            filename = $"ket_qua_{domainClean}_{timestamp}.txt";

        return Path.Combine(outputDir, filename);
    }

    public long GenerateBatch(long targetCount = 200_000_000, string? outputFile = null, bool autoMode = false, int? batchIndex = null, int? totalBatches = null)
    {
        if (totalCombinations <= 0)
        {
            Console.WriteLine("❌ Không có dữ liệu để sinh.");
            return 0;
        }

        outputFile ??= MakeOutputName(batchIndex, totalBatches);
        var startPos = LoadProgress();
        if (startPos >= totalCombinations)
        {
            Console.WriteLine("\n⚠ Đã sinh hết toàn bộ dữ liệu không trùng.");
            return 0;
        }

        var remainingTotal = totalCombinations - startPos;
        var actualTarget = Math.Min(targetCount, remainingTotal);

        var startPair = startPos / 4;
        var startFormat = (int)(startPos % 4);

        Console.WriteLine($"\n{new string('=', 60)}");
        Console.WriteLine(batchIndex.HasValue && totalBatches.HasValue ? $"BẮT ĐẦU FILE {batchIndex:N0}/{totalBatches:N0}" : "BẮT ĐẦU SINH FILE");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"File output: {outputFile}");
        Console.WriteLine($"Đã sinh trước đó: {startPos:N0} dòng");
        Console.WriteLine($"Còn lại tối đa: {remainingTotal:N0} dòng");
        Console.WriteLine($"Sẽ sinh file này: {actualTarget:N0} dòng");
        Console.WriteLine($"Vị trí bắt đầu: cặp {startPair:N0}, định dạng {formats[startFormat]}");
        Console.WriteLine(new string('=', 60));

        if (actualTarget <= 0) return 0;

        if (!autoMode)
        {
            if (targetCount > remainingTotal) Console.WriteLine($"⚠ Yêu cầu {targetCount:N0} nhưng chỉ còn {remainingTotal:N0} dòng.");
            Console.Write("Nhấn Enter để tiếp tục (hoặc gõ CANCEL để hủy): ");
            var confirm = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (confirm == "CANCEL") return 0;
        }

        Console.WriteLine("\nMẪU KẾT QUẢ (5 dòng đầu):");
        var samplePair = startPair;
        var sampleFormat = startFormat;
        for (var i = 0; i < 5 && samplePair < totalPairs; i++)
        {
            var idxA = samplePair / namesB.Count;
            var idxB = samplePair % namesB.Count;
            Console.WriteLine($"  {BuildStringFast(idxA, idxB, sampleFormat)}");
            sampleFormat++;
            if (sampleFormat >= 4)
            {
                sampleFormat = 0;
                samplePair++;
            }
        }

        long generated = 0;
        var sw = Stopwatch.StartNew();
        var lastSavedSecond = 0L;

        using var outFs = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.Read, 1 << 20, FileOptions.SequentialScan);
        using var writer = new StreamWriter(outFs, Encoding.UTF8, 1 << 20);

        var currentPair = startPair;
        var currentFormat = startFormat;

        while (generated < actualTarget)
        {
            if (currentPair >= totalPairs)
            {
                Console.WriteLine($"\nĐÃ HẾT DỮ LIỆU! Đã sinh {generated:N0}/{actualTarget:N0} dòng");
                break;
            }

            var idxA = currentPair / namesB.Count;
            var idxB = currentPair % namesB.Count;
            writer.WriteLine(BuildStringFast(idxA, idxB, currentFormat));
            generated++;

            currentFormat++;
            if (currentFormat >= 4)
            {
                currentFormat = 0;
                currentPair++;
            }

            var elapsedSec = sw.ElapsedMilliseconds / 1000;
            if (generated % 1_000_000 == 0 || elapsedSec - lastSavedSecond >= 30)
            {
                SaveProgress(startPos + generated);
                lastSavedSecond = elapsedSec;

                var elapsed = sw.Elapsed.TotalSeconds;
                var speed = elapsed > 0 ? generated / elapsed : 0;
                var eta = speed > 0 ? (actualTarget - generated) / speed : 0;
                var percent = (double)generated / actualTarget * 100;

                var desc = batchIndex.HasValue && totalBatches.HasValue
                    ? $"File {batchIndex}/{totalBatches}"
                    : "Đang sinh";
                Console.WriteLine($"[{desc}] {percent:F2}% | {generated:N0}/{actualTarget:N0} | {speed:F0} dòng/s | ETA {eta / 60:F1} phút | format {formats[currentFormat]}");
            }
        }

        writer.Flush();
        var totalElapsed = sw.Elapsed.TotalSeconds;
        var globalEnd = startPos + generated;
        SaveProgress(globalEnd);

        Console.WriteLine($"\n{new string('=', 60)}");
        Console.WriteLine("HOÀN THÀNH FILE");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"Output: {outputFile}");
        Console.WriteLine($"Thời gian: {sw.Elapsed.Hours}h{sw.Elapsed.Minutes}m{sw.Elapsed.Seconds}s");
        Console.WriteLine(totalElapsed > 0 ? $"Tốc độ TB: {generated / totalElapsed:F0} dòng/s" : "Tốc độ TB: 0 dòng/s");
        Console.WriteLine($"File này: {generated:N0} dòng");
        Console.WriteLine($"Tổng đã sinh: {globalEnd:N0}");
        Console.WriteLine($"Còn lại: {totalCombinations - globalEnd:N0} dòng");
        Console.WriteLine($"Đạt: {(double)globalEnd / totalCombinations * 100:F6}% tổng số");
        Console.WriteLine(new string('=', 60));

        var historyFile = Path.Combine(stateDir, "history.txt");
        File.AppendAllText(historyFile,
            $"[{DateTime.Now}] Batch {(batchIndex.HasValue ? batchIndex.Value : "-")} | {generated:N0} dòng | Tổng: {globalEnd:N0} ({(double)globalEnd / totalCombinations * 100:F4}%) | Tốc độ: {(totalElapsed > 0 ? generated / totalElapsed : 0):F0}/s | Đuôi: {(string.IsNullOrWhiteSpace(emailDomain) ? "Không" : emailDomain)} | File: {Path.GetFileName(outputFile)}{Environment.NewLine}",
            Encoding.UTF8);

        return generated;
    }

    public void GenerateMultipleBatches(long linesPerFile = 200_000_000, int numFiles = 20, string outputDir = "output")
    {
        Console.WriteLine($"\n{new string('=', 60)}");
        Console.WriteLine("CHẾ ĐỘ TỰ CHẠY LIÊN TỤC");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"Số file cần sinh: {numFiles:N0}");
        Console.WriteLine($"Số dòng mỗi file: {linesPerFile:N0}");
        Console.WriteLine($"Thư mục output: {outputDir}");
        Console.WriteLine(new string('=', 60));

        long totalGeneratedAll = 0;
        var finishedFiles = 0;
        var session = Stopwatch.StartNew();

        for (var i = 1; i <= numFiles; i++)
        {
            var currentPos = LoadProgress();
            var remaining = totalCombinations - currentPos;
            if (remaining <= 0)
            {
                Console.WriteLine("\n⚠ Đã hết toàn bộ dữ liệu không trùng, dừng sớm.");
                break;
            }

            Console.WriteLine($"\n\n{new string('#', 70)}");
            Console.WriteLine($"ĐANG CHẠY FILE {i}/{numFiles}");
            Console.WriteLine($"Còn lại trong kho: {remaining:N0} dòng");
            Console.WriteLine(new string('#', 70));

            var outputFile = MakeOutputName(i, numFiles, outputDir);
            var generated = GenerateBatch(linesPerFile, outputFile, autoMode: true, i, numFiles);
            if (generated <= 0)
            {
                Console.WriteLine($"⚠ File {i}/{numFiles} không sinh được thêm dữ liệu, dừng.");
                break;
            }

            totalGeneratedAll += generated;
            finishedFiles++;

            currentPos = LoadProgress();
            remaining = totalCombinations - currentPos;
            Console.WriteLine($"\n✅ XONG FILE {i}/{numFiles}");
            Console.WriteLine($"File này: {generated:N0} dòng");
            Console.WriteLine($"Tổng trong phiên này: {totalGeneratedAll:N0} dòng");
            Console.WriteLine($"Còn lại trong kho: {remaining:N0} dòng");

            if (generated < linesPerFile)
            {
                Console.WriteLine("\n⚠ File cuối cùng bị thiếu do đã hết dữ liệu không trùng.");
                break;
            }
        }

        Console.WriteLine($"\n{new string('=', 70)}");
        Console.WriteLine("HOÀN THÀNH CHẾ ĐỘ TỰ CHẠY");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"Số file đã xong: {finishedFiles:N0}/{numFiles:N0}");
        Console.WriteLine($"Tổng số dòng đã sinh trong phiên này: {totalGeneratedAll:N0}");
        Console.WriteLine($"Thời gian toàn phiên: {session.Elapsed.Hours}h{session.Elapsed.Minutes}m{session.Elapsed.Seconds}s");
        if (session.Elapsed.TotalSeconds > 0)
            Console.WriteLine($"Tốc độ TB toàn phiên: {totalGeneratedAll / session.Elapsed.TotalSeconds:F0} dòng/s");
        Console.WriteLine(new string('=', 70));
    }

    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine(new string('=', 60));
        Console.WriteLine("PHẦN MỀM GHÉP TÊN - SIÊU TỐC v6.0");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("Đặc điểm:");
        Console.WriteLine("✅ 4 định dạng: ab, a-b, a.b, a_b");
        Console.WriteLine("✅ Không trùng giữa các lần chạy");
        Console.WriteLine("✅ Tự lưu tiến độ");
        Console.WriteLine("✅ Có thể nhập số file, ví dụ 20 => tự sinh liên tục 20 file");
        Console.WriteLine("✅ Mỗi file có thể đặt 200,000,000 dòng");
        Console.WriteLine(new string('=', 60));

        EnsureSampleFile("a.txt", "TenA");
        EnsureSampleFile("b.txt", "TenB");

        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("CẤU HÌNH ĐUÔI EMAIL");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("Ví dụ: @gmx.de, @gmail.com, @yahoo.com");
        Console.WriteLine("(Nhấn Enter để bỏ qua)");
        Console.Write("Nhập đuôi email: ");
        var emailDomain = (Console.ReadLine() ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(emailDomain) && !emailDomain.StartsWith('@'))
            emailDomain = "@" + emailDomain;

        Console.WriteLine(string.IsNullOrWhiteSpace(emailDomain) ? "✓ Không thêm đuôi email" : $"✓ Đuôi email: {emailDomain}");

        var combiner = new NameCombinerSuperFast("a.txt", "b.txt", emailDomain, "state");
        if (combiner.namesA.Count == 0 || combiner.namesB.Count == 0)
        {
            Console.WriteLine("\n❌ Lỗi: File không chứa tên hợp lệ!");
            Console.Write("Nhấn Enter để thoát...");
            Console.ReadLine();
            return;
        }

        try
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("CẤU HÌNH SINH DỮ LIỆU");
            Console.WriteLine(new string('=', 60));

            Console.Write("Số dòng mỗi file (Enter = 200000000): ");
            var linesInput = (Console.ReadLine() ?? string.Empty).Trim();
            var linesPerFile = string.IsNullOrWhiteSpace(linesInput)
                ? 200_000_000L
                : long.Parse(linesInput.Replace(",", "").Replace("_", ""), CultureInfo.InvariantCulture);

            Console.Write("Số file cần sinh liên tục (Enter = 1): ");
            var filesInput = (Console.ReadLine() ?? string.Empty).Trim();
            var numFiles = string.IsNullOrWhiteSpace(filesInput)
                ? 1
                : int.Parse(filesInput.Replace(",", "").Replace("_", ""), CultureInfo.InvariantCulture);

            if (linesPerFile <= 0 || numFiles <= 0)
            {
                Console.WriteLine("❌ Số dòng và số file phải lớn hơn 0");
                return;
            }

            var needTotal = linesPerFile * numFiles;
            var remainingTotal = combiner.totalCombinations - combiner.LoadProgress();

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("TÓM TẮT");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"Số dòng mỗi file: {linesPerFile:N0}");
            Console.WriteLine($"Số file: {numFiles:N0}");
            Console.WriteLine($"Tổng cần sinh: {needTotal:N0}");
            Console.WriteLine($"Tổng còn khả dụng: {remainingTotal:N0}");
            Console.WriteLine("Nếu bạn nhập 20 ở ô số file, tool sẽ tự chạy liên tục 20 file.");
            Console.WriteLine(new string('=', 60));

            Console.Write("Gõ YES để bắt đầu: ");
            var confirm = (Console.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();
            if (confirm != "YES")
            {
                Console.WriteLine("Đã hủy.");
                return;
            }

            Console.CancelKeyPress += (_, e) =>
            {
                Console.WriteLine("\n\n⏸ Đã nhận Ctrl+C. Tiến trình sẽ dừng an toàn sau khi lưu progress.");
                e.Cancel = false;
            };

            combiner.GenerateMultipleBatches(linesPerFile, numFiles, "output");
        }
        catch (FormatException)
        {
            Console.WriteLine("\n❌ Lỗi: Vui lòng nhập số hợp lệ");
        }
        catch (Exception e)
        {
            Console.WriteLine($"\n❌ Lỗi: {e.Message}");
        }

        Console.Write("\n" + new string('=', 60) + "\nNhấn Enter để thoát...");
        Console.ReadLine();
    }

    private static void EnsureSampleFile(string fileName, string prefix)
    {
        if (File.Exists(fileName)) return;

        Console.WriteLine($"\n⚠ Không tìm thấy file {fileName}");
        Console.WriteLine("Tạo file mẫu với 1,000 tên...");

        using var fs = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 20);
        using var writer = new StreamWriter(fs, Encoding.UTF8, 1 << 20);
        for (var i = 0; i < 1000; i++) writer.WriteLine($"{prefix}_{i}");

        Console.WriteLine($"✓ Đã tạo file {fileName}");
    }
}
