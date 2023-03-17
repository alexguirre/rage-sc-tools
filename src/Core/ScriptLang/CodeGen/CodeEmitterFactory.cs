namespace ScTools.ScriptLang.CodeGen;

using ScTools.ScriptLang.Workspace;

public readonly record struct CodeEmitterCreateParams(VarAllocator Statics);

public static class CodeEmitterFactory
{
    public static ICodeEmitter CreateForTarget(BuildTarget target, CodeEmitterCreateParams createParams)
        => target switch
        {
            (Game.GTAIV, Platform.x86) => new Targets.NY.CodeEmitter(createParams.Statics),
            (Game.GTAV, Platform.x64) => new Targets.Five.CodeEmitter(createParams.Statics),
            _ => throw new NotSupportedException($"Target '{target}' is not supported"),
        };

    public static bool IsTargetSupported(BuildTarget target)
        => target switch
        {
            (Game.GTAIV, Platform.x86) => true,
            (Game.GTAV, Platform.x64) => true,
            _ => false,
        };
}
