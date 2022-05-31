namespace ScTools.Cli;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

public class FileGlob
{
    private PatternMatchingResult result;
    private PatternMatchingResult Result => result ??= Execute();

    public ImmutableArray<string> Patterns { get; }
    public bool HasMatches => Result.HasMatches;
    public IEnumerable<FileInfo> Matches => Result.Files.Select(m => new FileInfo(m.Path));

    public FileGlob(string pattern) => Patterns = ImmutableArray.Create(pattern);
    public FileGlob(IEnumerable<string> patterns) => Patterns = ImmutableArray.CreateRange(patterns);

    private PatternMatchingResult Execute() => AddIncludes(new Matcher()).Execute(new DirectoryInfoWrapper(new DirectoryInfo(".")));

    private Matcher AddIncludes(Matcher m)
    {
        foreach(string s in Patterns)
        {
            m.AddInclude(s);
        }
        return m;
    }
}
