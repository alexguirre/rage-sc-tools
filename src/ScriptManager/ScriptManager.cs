namespace ScTools
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;

    using ScTools.Five;

    public sealed class ScriptManager
    {
        private readonly ConcurrentQueue<FileInfo> scriptsToRegister = new();

        public ScriptManager()
        {
            Util.AfterGameUpdate += Update;
        }

        private void Update()
        {
            while (scriptsToRegister.TryDequeue(out var scriptFile))
            {
                if (scriptFile.Exists)
                {
                    var fullPath = scriptFile.FullName;
                    var fileName = scriptFile.Name;

                    var scriptIndex = strPackfileManager.RegisterIndividualFile(fullPath, true, fileName, errorIfFailed: false);
                    if (scriptIndex.Value != -1)
                    {
                        var localIndex = scriptIndex.ToLocal(CStreamedScripts.Instance.ObjectsBaseIndex);
                        Console.WriteLine($"Registered '{scriptFile}' (index: {scriptIndex.Value}, local: {localIndex.Value})");
                        var loaded = CStreamedScripts.Instance.StreamingBlockingLoad(localIndex, 17);
                        Console.WriteLine($"    Loaded: {loaded}");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to register '{scriptFile}'");
                    }
                }
                else
                {
                    Console.WriteLine($"File '{scriptFile}' no longer exists");
                }
            }
        }

        public void RegisterScript(FileInfo scriptFile)
        {
            if (!scriptFile.Exists)
            {
                throw new ArgumentException($"File '{scriptFile}' does not exist", nameof(scriptFile));
            }

            scriptsToRegister.Enqueue(scriptFile);
        }
    }
}
