namespace ScTools.GameFiles;

public interface IScript
{
    /// <summary>
    /// Dumps the contents of the script to the specified <see cref="System.IO.TextWriter"/>.
    /// </summary>
    /// <param name="sink">Output writer.</param>
    /// <param name="options">Output formatting options.</param>
    void Dump(System.IO.TextWriter sink, DumpOptions options);

}

public static class ScriptExtensions
{
    /// <summary>
    /// Dumps the contents of the script to a string.
    /// </summary>
    /// <param name="script">The script to dump.</param>
    /// <param name="options">Output formatting options.</param>
    /// <returns>A string with a textual representation of the contents of this script.</returns>
    public static string DumpToString(this IScript script, DumpOptions options)
    {
        using var sw = new System.IO.StringWriter();
        script.Dump(sw, options);
        return sw.ToString();
    }

    /// <summary>
    /// Dumps the contents of the script to a string with default formatting.
    /// </summary>
    /// <param name="script">The script to dump.</param>
    /// <returns>A string with a textual representation of the contents of this script.</returns>
    public static string DumpToString(this IScript script) => DumpToString(script, DumpOptions.Default);
}
