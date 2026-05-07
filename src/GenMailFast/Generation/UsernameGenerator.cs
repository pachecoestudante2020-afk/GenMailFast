namespace GenMailFast.Generation;

public static class UsernameGenerator
{
    public static IEnumerable<string> Generate(string[] tokens, IReadOnlyList<string> rules)
    {
        if (tokens.Length == 0)
        {
            yield break;
        }

        string first = tokens[0];
        string last = tokens[^1];
        string all = string.Concat(tokens);
        string allDot = string.Join('.', tokens);

        HashSet<string> seen = [];
        foreach (string rule in rules)
        {
            string? username = rule switch
            {
                "firstlast" => first + last,
                "first.dot.last" => first + "." + last,
                "first_last" => first + "_" + last,
                "first-last" => first + "-" + last,
                "flast" => first[0] + last,
                "firstl" => first + last[0],
                "all" => all,
                "all.dot" => allDot,
                _ => null
            };

            if (!string.IsNullOrEmpty(username) && seen.Add(username))
            {
                yield return username;
            }
        }
    }
}
