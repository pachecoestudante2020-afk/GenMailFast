namespace GenMailFast.Generation;

public static class DirectUsernameDetector
{
    public static bool IsDirectUsername(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string value = text.Trim().ToLowerInvariant();
        if (value.Contains(' ') || value.Contains('@') || value.Contains("://"))
        {
            return false;
        }

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool valid = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-';
            if (!valid)
            {
                return false;
            }
        }

        return value.Length >= 2;
    }
}
