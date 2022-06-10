namespace ScTools.ScriptLang.Workspace.Json;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Serializes the array of build configurations as a JSON object where the keys are the names.
/// </summary>
public sealed class BuildConfigurationsConverter : JsonConverter<ImmutableArray<BuildConfiguration>>
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
