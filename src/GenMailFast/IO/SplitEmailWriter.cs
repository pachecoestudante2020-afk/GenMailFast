using System.IO.Compression;
using System.Text;

namespace GenMailFast.IO;

public sealed class SplitEmailWriter : IDisposable
{
    private readonly string _folder;
    private readonly int _rowsPerFile;
    private readonly bool _compress;
    private StreamWriter? _writer;
    private int _fileIndex;
    private int _rowsInCurrent;
    public int FilesCreated { get; private set; }

    public SplitEmailWriter(string folder, int rowsPerFile, bool compress)
    {
        _folder = folder;
        _rowsPerFile = rowsPerFile;
        _compress = compress;
        Directory.CreateDirectory(folder);
    }

    public void WriteLine(string email)
    {
        EnsureWriter();
        _writer!.WriteLine(email);
        _rowsInCurrent++;
        if (_rowsInCurrent >= _rowsPerFile)
        {
            _writer.Dispose();
            _writer = null;
            _rowsInCurrent = 0;
        }
    }

    private void EnsureWriter()
    {
        if (_writer is not null) return;
        _fileIndex++;
        FilesCreated++;
        string suffix = _compress ? ".txt.gz" : ".txt";
        string path = Path.Combine(_folder, $"emails_{_fileIndex:000}{suffix}");
        FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4 * 1024 * 1024);
        Stream stream = _compress ? new GZipStream(fs, CompressionMode.Compress) : fs;
        _writer = new StreamWriter(stream, new UTF8Encoding(false), 4 * 1024 * 1024);
    }

    public void Dispose() => _writer?.Dispose();
}
