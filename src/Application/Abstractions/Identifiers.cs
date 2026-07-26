namespace ssmsmcp.Application.Abstractions;

internal static class Identifiers
{
    public static string BuildFqn(string database, string schema, string name) =>
        $"{Quote(database)}.{BuildQualifiedName(schema, name)}";

    public static string BuildQualifiedName(string schema, string name) =>
        $"{Quote(schema)}.{Quote(name)}";

    public static string Quote(string identifier) =>
        IsSimpleIdentifier(identifier) ? identifier : $"[{identifier.Replace("]", "]]")}]";

    private static bool IsSimpleIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (!(char.IsLetter(value[0]) || value[0] == '_'))
        {
            return false;
        }

        foreach (char c in value)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_'))
            {
                return false;
            }
        }

        return true;
    }
}
