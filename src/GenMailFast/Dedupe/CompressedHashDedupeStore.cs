using System.IO.Compression;

namespace GenMailFast.Dedupe;

public sealed class CompressedHashDedupeStore
{
    private static readonly byte[] Magic = [0x47, 0x4D, 0x46, 0x44]; // GMFD
    private const int Version = 1;
    private readonly string _path;
    public HashSet<ulong> Hashes { get; } = [];

    public CompressedHashDedupeStore(string path) => _path = path;

    public void Load()
    {
        if (!File.Exists(_path)) return;
        using FileStream fs = new(_path, FileMode.Open, FileAccess.Read, FileShare.Read, 4 * 1024 * 1024);
        using GZipStream gz = new(fs, CompressionMode.Decompress);
        using BinaryReader reader = new(gz);
        byte[] m = reader.ReadBytes(4);
        if (!m.SequenceEqual(Magic) || reader.ReadInt32() != Version) throw new InvalidDataException("Invalid dedupe file.");
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++) Hashes.Add(reader.ReadUInt64());
    }

    public bool TryAdd(string normalizedEmail) => Hashes.Add(Fnv1a64.Hash(normalizedEmail));

    public void SaveAtomic()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
        string tempPath = _path + ".tmp";
        using (FileStream fs = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4 * 1024 * 1024))
        using (GZipStream gz = new(fs, CompressionMode.Compress))
        using (BinaryWriter writer = new(gz))
        {
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(Hashes.Count);
            foreach (ulong hash in Hashes) writer.Write(hash);
        }

        File.Move(tempPath, _path, true);
    }
}
