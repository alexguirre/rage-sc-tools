namespace ScTools.ScriptLang.Workspace;

public record BuildConfiguration(string Name, BuildTarget Target, ImmutableArray<string> Defines);
