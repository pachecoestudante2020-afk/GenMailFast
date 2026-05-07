namespace GenMailFast.Generation;

public static class EmailBuilder
{
    public static string ValidateAndNormalizeDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) throw new ArgumentException("Domain is required.");
        string d = domain.Trim().ToLowerInvariant();
        if (d.Contains('@') || d.Any(char.IsWhiteSpace) || !d.Contains('.')) throw new ArgumentException("Invalid domain.");
        return d;
    }

    public static string Build(string username, string domain)
    {
        return username + "@" + ValidateAndNormalizeDomain(domain);
    }
}
