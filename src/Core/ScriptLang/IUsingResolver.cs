#nullable enable
namespace ScTools.ScriptLang
{
    using System.IO;

    /// <summary>
    /// Resolves USING paths to <see cref="Module"/>s.
    /// </summary>
    public interface IUsingModuleResolver
    {
        Module? Resolve(string usingPath);
    }

    /// <summary>
    /// Resolves USING paths to source code.
    /// </summary>
    public interface IUsingSourceResolver
    {
        /// <summary>
        /// Converts <paramref name="usingPath"/> to its unique representation, for example, a relative path to an absolute one.
        /// </summary>
        string NormalizePath(string usingPath);

        /// <summary>
        /// Does <paramref name="usingPath"/> exist? 
        /// </summary>
        bool IsValid(string usingPath);

        /// <summary>
        /// Have the contents of <paramref name="usingPath"/> changed since <see cref="Resolve(string)"/> was last called?
        /// </summary>
        bool HasChanged(string usingPath);

        /// <summary>
        /// Returns the source code of <paramref name="usingPath"/>.
        /// </summary>
        /// <exception cref="System.ArgumentException">If <see cref="IsValid(string)"/> on <paramref name="usingPath"/> returns <c>false</c>.</exception>
        TextReader Resolve(string usingPath);
    }
}
