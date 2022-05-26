namespace ScTools.ScriptLang.Semantics;

internal class LabelGenerator
{
    private ulong counter = 0;

    public string NextLabel() => $"__lbl{counter++}";
}
