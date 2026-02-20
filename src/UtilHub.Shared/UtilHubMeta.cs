using System.Text;

public static class UtilHubMeta
{
    public sealed record UtilityInfo(
        string Id,
        string Title,
        string Description,
        string[] Tags,
        string FilePath
    );

    // ---------------- Metadata parsing ----------------

    public static UtilityInfo ReadInfo(string filePath, int maxLines)
    {
        var defaultId = Path.GetFileNameWithoutExtension(filePath);

        var id = defaultId;
        var title = defaultId;
        var desc = "";
        var tags = Array.Empty<string>();

        foreach (var raw in File.ReadLines(filePath).Take(maxLines))
        {
            var line = raw.Trim();
            if (!line.StartsWith("// @", StringComparison.Ordinal)) continue;

            if (TryGetValue(line, "@id", out var v1)) id = v1;
            else if (TryGetValue(line, "@title", out var v2)) title = v2;
            else if (TryGetValue(line, "@desc", out var v3)) desc = v3;
            else if (TryGetValue(line, "@tags", out var v4))
                tags = v4.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return new UtilityInfo(id, title, desc, tags, filePath);
    }

    public static bool TryGetValue(string line, string key, out string value)
    {
        // Expected format: "// @key value..."
        value = "";
        var prefix = $"// {key} ";
        if (!line.StartsWith(prefix, StringComparison.Ordinal)) return false;

        value = line[prefix.Length..].Trim();
        return true;
    }
}
