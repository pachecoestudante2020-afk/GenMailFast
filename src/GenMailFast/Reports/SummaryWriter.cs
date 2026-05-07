using GenMailFast.Models;

namespace GenMailFast.Reports;

public static class SummaryWriter
{
    public static void Write(string outputFolder, RunStats s)
    {
        string path = Path.Combine(outputFolder, "summary.txt");
        List<string> lines =
        [
            $"started_at={s.StartedAt:O}",
            $"finished_at={s.FinishedAt:O}",
            $"elapsed={(s.FinishedAt - s.StartedAt)}",
            $"input_path={s.InputPath}",
            $"domain={s.Domain}",
            $"rules={s.Rules}",
            $"input_lines_read={s.InputLinesRead}",
            $"candidates_generated={s.CandidatesGenerated}",
            $"emails_written={s.EmailsWritten}",
            $"duplicates_skipped={s.DuplicatesSkipped}",
            $"quality_rejected={s.QualityRejected}",
            $"output_files_created={s.OutputFilesCreated}",
            $"dedupe_file={s.DedupeFile}",
            $"dedupe_entries_before={s.DedupeEntriesBefore}",
            $"dedupe_entries_after={s.DedupeEntriesAfter}",
            $"compress_output={s.CompressOutput}",
            $"rows_per_file={s.RowsPerFile}"
        ];
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }
}
