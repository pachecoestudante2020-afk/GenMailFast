namespace GenMailFast.Models;

public sealed class RunStats
{
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset FinishedAt { get; set; }
    public string InputPath { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Rules { get; set; } = string.Empty;
    public long InputLinesRead { get; set; }
    public long CandidatesGenerated { get; set; }
    public long EmailsWritten { get; set; }
    public long DuplicatesSkipped { get; set; }
    public long QualityRejected { get; set; }
    public int OutputFilesCreated { get; set; }
    public string DedupeFile { get; set; } = string.Empty;
    public int DedupeEntriesBefore { get; set; }
    public int DedupeEntriesAfter { get; set; }
    public bool CompressOutput { get; set; }
    public int RowsPerFile { get; set; }
}
