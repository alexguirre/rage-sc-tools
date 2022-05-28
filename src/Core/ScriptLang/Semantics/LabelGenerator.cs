namespace ScTools.ScriptLang.Semantics;

internal class LabelGenerator
{
    private ulong counter = 0;
    public string Prefix { get; set; } = "lbl";

    public string NextLabel() => $"__{Prefix}{counter++}";
}
