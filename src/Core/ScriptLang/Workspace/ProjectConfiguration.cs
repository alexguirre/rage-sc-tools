namespace ScTools.ScriptLang.Workspace;

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public record ProjectConfiguration(
    ImmutableArray<string> Sources,
    string OutputPath,
    [property: JsonConverter(typeof(Json.BuildConfigurationsConverter))] ImmutableArray<BuildConfiguration> Configurations)
{
    public BuildConfiguration? GetBuildConfiguration(string name) => Configurations.FirstOrDefault(c => c.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

    public Task WriteToFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var file = File.Open(filePath, FileMode.Create, FileAccess.Write);
        return JsonSerializer.SerializeAsync(file, this, GetSerializerOptions(), cancellationToken);
    }

    public static async Task<ProjectConfiguration> ReadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Project file not found", filePath);
        }

        using var file = File.OpenRead(filePath);
        var config = await JsonSerializer.DeserializeAsync<ProjectConfiguration>(file, GetSerializerOptions(), cancellationToken).ConfigureAwait(false);
        if (config is null)
        {
            throw new InvalidDataException("Invalid project file");
        }

        return config;
    }

    private static JsonSerializerOptions GetSerializerOptions() => new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
}
