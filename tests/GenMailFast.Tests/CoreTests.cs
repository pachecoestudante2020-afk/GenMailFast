using GenMailFast.Dedupe;
using GenMailFast.Generation;
using GenMailFast.IO;
using GenMailFast.Models;
using GenMailFast.Reports;

namespace GenMailFast.Tests;

public class CoreTests
{
    [Fact]
    public void RemovesVietnameseAccents() => Assert.Equal("nguyen van a", NameNormalizer.Normalize("Nguyễn Văn A"));

    [Theory]
    [InlineData("jdoe", true)]
    [InlineData("john.smith", true)]
    [InlineData("john@example.com", false)]
    [InlineData("http://example.com", false)]
    [InlineData("two words", false)]
    public void DirectUsernameDetect(string value, bool expected) => Assert.Equal(expected, DirectUsernameDetector.IsDirectUsername(value));

    [Fact]
    public void RuleGenerationWorks()
    {
        string[] t = ["nguyen", "van", "a"];
        var got = UsernameGenerator.Generate(t, ["firstlast", "first.dot.last", "first_last", "first-last", "flast", "firstl", "all", "all.dot"]).ToList();
        Assert.Contains("nguyena", got);
        Assert.Contains("nguyen.a", got);
        Assert.Contains("na", got);
        Assert.Contains("nguyen.van.a", got);
    }

    [Fact]
    public void QualityRejectsInvalid() => Assert.False(QualityFilter.IsValidUsername(".bad"));

    [Fact]
    public void EmailBuilderValidatesDomain() => Assert.Throws<ArgumentException>(() => EmailBuilder.ValidateAndNormalizeDomain("bad@domain"));

    [Fact]
    public void DedupeSameRun()
    {
        CompressedHashDedupeStore s = new(Path.GetTempFileName());
        Assert.True(s.TryAdd("a@example.com"));
        Assert.False(s.TryAdd("a@example.com"));
    }

    [Fact]
    public void DedupePersistsAcrossRuns()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".gz");
        var s1 = new CompressedHashDedupeStore(path);
        Assert.True(s1.TryAdd("x@example.com"));
        s1.SaveAtomic();

        var s2 = new CompressedHashDedupeStore(path);
        s2.Load();
        Assert.False(s2.TryAdd("x@example.com"));
    }

    [Fact]
    public void OutputSplitsByRows()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        using SplitEmailWriter w = new(dir, 2, false);
        w.WriteLine("a@e.com"); w.WriteLine("b@e.com"); w.WriteLine("c@e.com");
        Assert.Equal(2, Directory.GetFiles(dir, "emails_*.txt").Length);
    }

    [Fact]
    public void GzipDedupeLoadsAfterSave()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".gz");
        var s = new CompressedHashDedupeStore(path);
        s.TryAdd("hello@example.com");
        s.SaveAtomic();
        var re = new CompressedHashDedupeStore(path);
        re.Load();
        Assert.Single(re.Hashes);
    }

    [Fact]
    public void SummaryWritten()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        SummaryWriter.Write(dir, new RunStats { StartedAt = DateTimeOffset.UtcNow, FinishedAt = DateTimeOffset.UtcNow, InputPath = "i", Domain = "d.com", Rules = "all" });
        Assert.True(File.Exists(Path.Combine(dir, "summary.txt")));
    }
}
