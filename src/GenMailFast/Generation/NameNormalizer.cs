using System.Globalization;
using System.Text;

namespace GenMailFast.Generation;

public static class NameNormalizer
{
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string lowered = input.Trim().ToLowerInvariant().Replace('đ', 'd');
        string decomposed = lowered.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new(decomposed.Length);
        bool prevSpace = false;

        foreach (char c in decomposed)
        {
            UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (!prevSpace)
                {
                    sb.Append(' ');
                }

                prevSpace = true;
            }
            else
            {
                sb.Append(c);
                prevSpace = false;
            }
        }

        return sb.ToString().Trim().Normalize(NormalizationForm.FormC);
    }

    public static string[] Tokenize(string normalized)
    {
        return string.IsNullOrWhiteSpace(normalized)
            ? []
            : normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}
