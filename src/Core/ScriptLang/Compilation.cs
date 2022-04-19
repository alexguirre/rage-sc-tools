namespace ScTools.ScriptLang;

using System.Collections.Generic;
using System.IO;

/// <summary>
/// Result of one or more compilation units.
/// </summary>
public sealed class Compilation
{
    /// <summary>
    /// Gets the scripts result of the compilation.
    /// </summary>
    public ScriptCompilation[] Scripts { get; }

    public Compilation(IEnumerable<string> sourceFiles)
    {

    }
}

/// <summary>
/// Result of a compiled SCRIPT declaration.
/// </summary>
public sealed class ScriptCompilation
{

}
