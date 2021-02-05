namespace ScTools
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
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

        public IEnumerable<(string Name, int Index)> EnumerateRegisteredScripts()
        {
            if (Util.IsInGame)
            {
                var size = CStreamedScripts.Instance.GetSize();
                for (int i = 0; i < size; i++)
                {
                    var name = CStreamedScripts.Instance.GetAssetName(i);
                    if (name != "-undefined-")
                    {
                        yield return (name, i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < 100; i++)
                {
                    yield return ($"script_{i}", i);
                }
            }
        }
    }
}
