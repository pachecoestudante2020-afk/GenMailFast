namespace GenMailFast.Generation;

public static class QualityFilter
{
    public static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 2 || username.Length > 64)
        {
            return false;
        }

        if (username.Contains('@') || username.Contains("://"))
        {
            return false;
        }

        char prev = '\0';
        for (int i = 0; i < username.Length; i++)
        {
            char c = username[i];
            bool isSep = c == '.' || c == '_' || c == '-';
            bool valid = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || isSep;
            if (!valid)
            {
                return false;
            }

            if (i == 0 || i == username.Length - 1)
            {
                if (isSep)
                {
                    return false;
                }
            }

            if (isSep && prev == c)
            {
                return false;
            }

            prev = c;
        }

        return true;
    }
}
