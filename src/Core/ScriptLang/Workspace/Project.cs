﻿namespace ScTools.ScriptLang.Workspace;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Semantics;

using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

public class Project : IDisposable, IUsingResolver
{
    private readonly ConcurrentDictionary<string, SourceFile> sources = new();
    private bool isDisposed;
    private BuildConfiguration buildConfiguration;

    public string RootDirectory { get; }
    public ProjectConfiguration Configuration { get; }
    public string BuildConfigurationName
    {
        get => buildConfiguration.Name;
        set => buildConfiguration = Configuration.GetBuildConfiguration(value) ?? throw new ArgumentException($"Build configuration '{value}' does not exist", nameof(value));
    }
    public BuildConfiguration BuildConfiguration => buildConfiguration;
    public IReadOnlyDictionary<string, SourceFile> Sources => sources;

    private Project(string rootDirectoryPath, ProjectConfiguration config)
    {
        RootDirectory = rootDirectoryPath;
        Configuration = config;
        BuildConfigurationName = config.Configurations.First().Name;
        Debug.Assert(buildConfiguration is not null, "BuildConfigurationName setter should have set buildConfiguration field");
    }

    public SourceFile? GetSourceFile(string filePath)
    {
        var absoluteFilePath = Path.GetFullPath(Path.Combine(RootDirectory, filePath));
        if (sources.TryGetValue(absoluteFilePath, out var sourceFile))
        {
            return sourceFile;
        }

        return null;
    }

    public async Task<SourceFile> AddSourceFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        filePath = Path.GetFullPath(filePath);

        if (sources.ContainsKey(filePath))
        {
            throw new ArgumentException($"File '{filePath}' is already added to project", nameof(filePath));
        }

        var sourceFile = await SourceFile.OpenAsync(this, filePath, cancellationToken).ConfigureAwait(false);
        if (!sources.TryAdd(sourceFile.Path, sourceFile))
        {
            throw new ArgumentException($"File '{filePath}' is already added to project", nameof(filePath));
        }

        return sourceFile;
    }

    private async Task OpenSourceFilesAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(sources.IsEmpty);
        var sourceFilesTasks = Configuration.Sources
            .Select(path => AddSourceFileAsync(Path.Combine(RootDirectory, path), cancellationToken));
        await Task.WhenAll(sourceFilesTasks).ConfigureAwait(false);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                sources.Values.ForEach(f => f.Dispose());
                sources.Clear();
            }
            isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }


    public static async Task<Project> OpenProjectAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var config = await ProjectConfiguration.ReadFromFileAsync(filePath, cancellationToken).ConfigureAwait(false);

        var absoluteFilePath = Path.GetFullPath(filePath);
        var rootDir = Path.GetDirectoryName(absoluteFilePath);
        Debug.Assert(rootDir is not null, "File exists so the directory name should not be null");

        var project = new Project(rootDir, config);
        await project.OpenSourceFilesAsync(cancellationToken).ConfigureAwait(false);
        return project;
    }

    async Task<UsingResolveResult> IUsingResolver.ResolveUsingAsync(UsingDirective usingDirective, CancellationToken cancellationToken)
    {
        var path = usingDirective.Path;
        var sourceFileDir = Path.GetDirectoryName(usingDirective.Location.FilePath);
        var pathRelativeToSourceFileDir = sourceFileDir is null ? null : Path.GetFullPath(Path.Combine(sourceFileDir, path));
        var pathRelativeToRoot = Path.GetFullPath(Path.Combine(RootDirectory, path));
        if (!(pathRelativeToSourceFileDir != null && sources.TryGetValue(pathRelativeToSourceFileDir, out var sourceFile)) &&
            !sources.TryGetValue(pathRelativeToRoot, out sourceFile))
        {
            return new(UsingResolveStatus.NotFound, Ast: null);
        }

        // TODO: check cyclic dependencies
        var ast = await sourceFile.GetAstAsync(cancellationToken).ConfigureAwait(false);
        return new(UsingResolveStatus.Valid, ast);
    }
}
