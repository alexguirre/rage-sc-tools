namespace ScTools
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;

    using ScTools.Five;

    internal sealed class ScriptManager
    {
        /// <summary>
        /// Queue of jobs to run on the main game thread.
        /// </summary>
        private readonly ConcurrentQueue<IJob> jobs = new();

        public event Action<string>? Output;

        public ScriptManager()
        {
            Util.AfterGameUpdate += Update;
        }

        private void OnOutput(string s) => Output?.Invoke(s);

        private void Update()
        {
            while (jobs.TryDequeue(out var job))
            {
                job.Execute(this);
            }
        }

        public void RegisterScript(FileInfo scriptFile)
        {
            if (!scriptFile.Exists)
            {
                throw new ArgumentException($"File '{scriptFile}' does not exist", nameof(scriptFile));
            }

            jobs.Enqueue(new RegisterScriptJob(scriptFile));
        }

        public void UnregisterScript(string script)
        {
            jobs.Enqueue(new UnregisterScriptJob(script));
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

        public IEnumerable<(scrThreadId ThreadId, scrProgramId ProgramId, string ProgramName, scrThread.State State)> EnumerateScriptThreads()
        {
            if (Util.IsInGame)
            {
                for (int i = 0; i < scrThread.Threads.Count; i++)
                {
                    if (scrThread.Threads.IsItemNull(i))
                    {
                        continue;
                    }

                    var thread = scrThread.Threads.ItemDeref(i);

                    if (thread.Info.ThreadId.Value != 0)
                    {
                        yield return (thread.Info.ThreadId, thread.Info.ProgramId, thread.GetName(), thread.Info.State);
                    }
                }
            }
            else
            {
                for (int i = 1; i <= 30; i++)
                {
                    yield return ((uint)i, (uint)(30 - i), $"script_{30 - i}", (scrThread.State)(i % 3));
                }
            }
        }

        public IEnumerable<(uint Size, bool Used)> EnumerateScriptStacks()
        {
            if (Util.IsInGame)
            {
                for (int i = 0; i < scrThread.Stacks.Count; i++)
                {
                    var stack = scrThread.Stacks[i];
                    yield return (stack.Size, stack.IsUsed);
                }
            }
            else
            {
                for (int i = 0; i < 20; i++)
                {
                    yield return ((uint)(i + 1) * 8, i % 2 == 0);
                }
                for (int i = 0; i < 20; i++)
                {
                    yield return ((uint)(i + 1) * 8, i % 2 != 0);
                }
            }
        }

        private interface IJob { public void Execute(ScriptManager mgr); }

        private sealed class RegisterScriptJob : IJob
        {
            public FileInfo ScriptFile { get; }

            public RegisterScriptJob(FileInfo scriptFile) => ScriptFile = scriptFile;

            public void Execute(ScriptManager mgr)
            {
                if (ScriptFile.Exists)
                {
                    var fullPath = ScriptFile.FullName;
                    var fileName = ScriptFile.Name;

                    var scriptIndex = strPackfileManager.RegisterIndividualFile(fullPath, true, fileName, errorIfFailed: false);
                    if (scriptIndex.Value != -1)
                    {
                        var localIndex = scriptIndex.ToLocal(CStreamedScripts.Instance.ObjectsBaseIndex);
                        mgr.OnOutput($"Registered '{ScriptFile}' (index: {scriptIndex.Value}, local: {localIndex.Value})");
                        var loaded = CStreamedScripts.Instance.StreamingBlockingLoad(localIndex, 17);
                        mgr.OnOutput($"    Loaded: {loaded}");
                    }
                    else
                    {
                        mgr.OnOutput($"Failed to register '{ScriptFile}'");
                    }
                }
                else
                {
                    mgr.OnOutput($"File '{ScriptFile}' no longer exists");
                }
            }
        }

        private sealed class UnregisterScriptJob : IJob
        {
            public string Name { get; }

            public UnregisterScriptJob(string name) => Name = name;

            public void Execute(ScriptManager mgr)
            {
                // TODO
                mgr.OnOutput($"{nameof(UnregisterScriptJob)} not implemented");
            }
        }
    }
}
