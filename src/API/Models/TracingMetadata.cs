public record TracingMetadata
{
    public Dictionary<string, string> Metadata { get; } = [];

    public static void SetMetadata(Dictionary<string, string> metadata, string key, string value)
    {
        metadata.Add(key, value);
    }

    public static IEnumerable<string> GetMetadata(Dictionary<string, string> metadata, string key)
    {
        return metadata.TryGetValue(key, out string? value) ? [value] : Enumerable.Empty<string>();
    }

    public void SetActivityMetadata(string key, string value)
    {
        Metadata.Add(key, value);
    }
};
