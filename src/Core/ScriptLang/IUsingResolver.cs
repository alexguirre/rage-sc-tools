#nullable enable
namespace ScTools.ScriptLang
{
    using System;
    using System.Collections.Generic;
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

    public class DefaultSourceResolver : IUsingSourceResolver
    {
        private readonly Dictionary<string, string> normalizedPathsCache = new();
        private readonly Dictionary<string, DateTime> filesLastWriteTimes = new();

        public string WorkingDirectory { get; }

        public DefaultSourceResolver(string workingDirectory)
        {
            if (!Directory.Exists(workingDirectory))
            {
                throw new ArgumentException($"Directory '{workingDirectory}' does not exist", nameof(workingDirectory));
            }

            WorkingDirectory = Path.GetFullPath(workingDirectory);
        }

        public string NormalizePath(string usingPath)
        {
            if (normalizedPathsCache.TryGetValue(usingPath, out var normalizedPath))
            {
                return normalizedPath;
            }

            normalizedPath = Path.GetFullPath(Path.Combine(WorkingDirectory, usingPath));
            normalizedPathsCache.Add(usingPath, normalizedPath);
            return normalizedPath;
        }

        public bool IsValid(string usingPath) => File.Exists(NormalizePath(usingPath));

        public bool HasChanged(string usingPath)
        {
            usingPath = NormalizePath(usingPath);
            if (filesLastWriteTimes.TryGetValue(usingPath, out var time))
            {
                var currTime = File.GetLastWriteTimeUtc(usingPath);
                return time == currTime;
            }

            return true;
        }

        public TextReader Resolve(string usingPath)
        {
            if (!IsValid(usingPath))
            {
                throw new ArgumentException(null, nameof(usingPath));
            }

            usingPath = NormalizePath(usingPath);
            filesLastWriteTimes[usingPath] = File.GetLastWriteTimeUtc(usingPath);
            return new StreamReader(File.OpenRead(usingPath));
        }
    }
}
