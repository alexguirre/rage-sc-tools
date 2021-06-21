namespace ScTools.ScriptLang.CodeGen
{
    using System.Collections.Generic;

    public sealed class StringsTable
    {
        public int Count => StringToLabel.Count;
        public IDictionary<string, string> StringToLabel { get; } = new Dictionary<string, string>();

        public void Add(string str)
        {
            if (!StringToLabel.ContainsKey(str))
            {
                StringToLabel.Add(str, $"str{StringToLabel.Count}");
            }
        }
    }
}
