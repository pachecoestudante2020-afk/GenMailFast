using System.Text;

namespace GenMailFast.Dedupe;

public static class Fnv1a64
{
    public static ulong Hash(string value)
    {
        const ulong offset = 14695981039346656037;
        const ulong prime = 1099511628211;
        ulong hash = offset;
        ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(value);
        foreach (byte b in bytes)
        {
            hash ^= b;
            hash *= prime;
        }

        return hash;
    }
}
