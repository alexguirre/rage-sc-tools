namespace ScTools.ScriptLang.Workspace;

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public record ProjectConfiguration(
    [property: JsonConverter(typeof(ProjectConfiguration.BuildConfigurationsJsonConverter))]
    ImmutableArray<BuildConfiguration> BuildConfigurations)
{
    public BuildConfiguration? GetBuildConfiguration(string name) => BuildConfigurations.FirstOrDefault(c => c.Name == name);

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

    /// <summary>
    /// Serializes the array of build configurations as a dictionary where the key is the name.
    /// </summary>
    public sealed class BuildConfigurationsJsonConverter : JsonConverter<ImmutableArray<BuildConfiguration>>
    {
        /// <summary>
        /// Same as <see cref="BuildConfiguration"/> but without <see cref="BuildConfiguration.Name"/>.
        /// </summary>
        private record BuildConfigurationWithoutName(BuildTarget Target, ImmutableArray<string> Defines);

        public override ImmutableArray<BuildConfiguration> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is not JsonTokenType.StartObject) { throw new JsonException(); }
            reader.Read();

            var buildConfigs = ImmutableArray.CreateBuilder<BuildConfiguration>();
            while (reader.TokenType is not JsonTokenType.EndObject)
            {
                var name = reader.GetString() ?? throw new JsonException("Build configuration name cannot be null");
                reader.Read();
                var tempConfig = JsonSerializer.Deserialize<BuildConfigurationWithoutName>(ref reader, options) ?? throw new JsonException("Build configuration cannot be null");
                reader.Read();
                buildConfigs.Add(new(name, tempConfig.Target, tempConfig.Defines));
            }

            return buildConfigs.ToImmutable();
        }

        public override void Write(Utf8JsonWriter writer, ImmutableArray<BuildConfiguration> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var buildConfig in value)
            {
                writer.WritePropertyName(buildConfig.Name);
                JsonSerializer.Serialize(writer, new BuildConfigurationWithoutName(buildConfig.Target, buildConfig.Defines), options);
            }
            writer.WriteEndObject();
        }
    }
}
