namespace ScTools.Cli;

using System.Text.Json;

public record Configuration(
    string? GTA5ExePath,
    string? GTA4ExePath,
    string? MC4XexPath,
    string? RDR2XexPath,
    string? MP3ExePath)
{
    public static Configuration Default => new(null, null, null, null, null);

    public void ToJson(System.IO.Stream stream) => JsonSerializer.Serialize(stream, this, GetSerializerOptions());
    public static Configuration? FromJson(System.IO.Stream stream) => JsonSerializer.Deserialize<Configuration>(stream, GetSerializerOptions());

    private static JsonSerializerOptions GetSerializerOptions() => new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
    };

    public static readonly string[] OptionNames = {
        nameof(GTA5ExePath),
        nameof(GTA4ExePath),
        nameof(MC4XexPath),
        nameof(RDR2XexPath),
        nameof(MP3ExePath)
    };
    
    public Configuration Set(string option, string? value)
    {
        var cmp = System.StringComparer.OrdinalIgnoreCase;
        if (cmp.Equals(option, nameof(GTA5ExePath))) { return this with { GTA5ExePath = value }; }
        if (cmp.Equals(option, nameof(GTA4ExePath))) { return this with { GTA4ExePath = value }; }
        if (cmp.Equals(option, nameof(MC4XexPath))) { return this with { MC4XexPath = value }; }
        if (cmp.Equals(option, nameof(RDR2XexPath))) { return this with { RDR2XexPath = value }; }
        if (cmp.Equals(option, nameof(MP3ExePath))) { return this with { MP3ExePath = value }; }

        throw new System.ArgumentException($"Unknown option '{option}'", nameof(option));
    }
    
    public string? Get(string option)
    {
        var cmp = System.StringComparer.OrdinalIgnoreCase;
        if (cmp.Equals(option, nameof(GTA5ExePath))) { return GTA5ExePath; }
        if (cmp.Equals(option, nameof(GTA4ExePath))) { return GTA4ExePath; }
        if (cmp.Equals(option, nameof(MC4XexPath))) { return MC4XexPath; }
        if (cmp.Equals(option, nameof(RDR2XexPath))) { return RDR2XexPath; }
        if (cmp.Equals(option, nameof(MP3ExePath))) { return MP3ExePath; }

        throw new System.ArgumentException($"Unknown option '{option}'", nameof(option));
    }
}
