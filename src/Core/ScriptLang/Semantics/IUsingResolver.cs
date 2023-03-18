namespace ScTools.ScriptLang.Semantics;

using ScTools.ScriptLang.Ast;

public enum UsingResolveStatus
{
    Valid,
    NotFound,
    CyclicDependency,
}

public readonly record struct UsingResolveResult(UsingResolveStatus Status, CompilationUnit? Ast);

public interface IUsingResolver
{
    Task<UsingResolveResult> ResolveUsingAsync(UsingDirective usingDirective, CancellationToken cancellationToken = default);
}
