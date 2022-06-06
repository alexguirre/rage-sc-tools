namespace ScTools.ScriptLang.Workspace;

using System.Text.Json.Serialization;

public readonly record struct BuildTarget(Game Game, Platform Platform);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Game
{
    /// <summary>
    /// Grand Theft Auto IV
    /// </summary>
    GTAIV,
    /// <summary>
    /// Midnight Club: Los Angeles
    /// </summary>
    MC4,
    /// <summary>
    /// Red Dead Redemption
    /// </summary>
    RDR2,
    /// <summary>
    /// Max Payne 3
    /// </summary>
    MP3,
    /// <summary>
    /// Grand Theft Auto V
    /// </summary>
    GTAV,
    /// <summary>
    /// Red Dead Redemption 2
    /// </summary>
    RDR3,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Platform
{
    /// <summary>
    /// Xbox 360
    /// </summary>
    Xenon,
    /// <summary>
    /// Windows 32-bit
    /// </summary>
    x86,
    /// <summary>
    /// Windows 64-bit
    /// </summary>
    x64,
}

public static class PlatformExtensions
{
    public static bool Is32Bit(this Platform platform)
        => !Is64Bit(platform);
    public static bool Is64Bit(this Platform platform)
        => platform == Platform.x64;
}
