namespace ScTools.ScriptLang.CodeGen;

using ScTools.ScriptLang.Workspace;

public readonly record struct CodeEmitterCreateParams(VarAllocator Statics, GlobalsAllocator Globals);

public static class CodeEmitterFactory
{
    public static ICodeEmitter CreateForTarget(BuildTarget target, CodeEmitterCreateParams createParams)
        => target switch
        {
            (Game.GTA4, Platform.x86) => new Targets.GTA4.CodeEmitter(createParams.Statics, createParams.Globals),
            (Game.GTA5, Platform.x64) => new Targets.GTA5.CodeEmitter(createParams.Statics, createParams.Globals),
            _ => throw new NotSupportedException($"Target '{target}' is not supported"),
        };

    public static bool IsTargetSupported(BuildTarget target)
        => target switch
        {
            (Game.GTA4, Platform.x86) => true,
            (Game.GTA5, Platform.x64) => true,
            _ => false,
        };
}
