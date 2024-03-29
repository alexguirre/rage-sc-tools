﻿namespace ScTools
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using ScTools.GTA5;

    internal sealed class ScriptManager : IDisposable
    {
        /// <summary>
        /// Queue of jobs to run on the main game thread.
        /// </summary>
        private readonly ConcurrentQueue<IJob> jobs = new();
        private readonly ManualResetEvent jobsDoneEvent = new(initialState: true);
        private readonly HashSet<strLocalIndex> externalRegisteredScripts = new();

        public event Action<string>? Output;

        public ScriptManager()
        {
            Util.AfterGameUpdate += Update;
        }

        public void Dispose()
        {
            jobsDoneEvent.Dispose();
        }

        public void WaitForJobs() => jobsDoneEvent.WaitOne();

        private void QueueJob(IJob job)
        {
            jobs.Enqueue(job);
            if (Util.IsInGame)
            {
                // jobs are only processed if we are in-game
                jobsDoneEvent.Reset();
            }
        }

        private void OnOutput(string s) => Output?.Invoke(s);

        private void Update()
        {
            while (jobs.TryDequeue(out var job))
            {
                job.Execute(this);
            }
            jobsDoneEvent.Set();
        }

        public void RegisterScript(FileInfo scriptFile)
        {
            if (!scriptFile.Exists)
            {
                throw new ArgumentException($"File '{scriptFile}' does not exist", nameof(scriptFile));
            }

            QueueJob(new RegisterScriptJob(scriptFile));
        }

        public void UnregisterScript(string script)
        {
            QueueJob(new UnregisterScriptJob(script));
        }

        public void StartThread(string script, uint stackSize)
        {
            QueueJob(new StartThreadJob(script, stackSize));
        }

        public void KillThread(scrThreadId id)
        {
            QueueJob(new KillThreadJob(id));
        }

        public IEnumerable<(string Name, uint NumRefs, bool Loaded)> EnumerateRegisteredScripts()
        {
            if (Util.IsInGame)
            {
                var size = CStreamedScripts.Instance.GetSize();
                for (int i = 0; i < size; i++)
                {
                    var name = CStreamedScripts.Instance.GetAssetName(i);
                    if (name != "-undefined-")
                    {
                        yield return (name, CStreamedScripts.Instance.GetNumRefs(i), !Unsafe.IsNullRef(ref CStreamedScripts.Instance.GetPtr(i)));
                    }
                }
            }
            else
            {
                for (int i = 0; i < 100; i++)
                {
                    yield return ($"script_{i}", 1, i % 2 == 0);
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
                        mgr.OnOutput($"\tLoaded: {loaded}");
                        mgr.externalRegisteredScripts.Add(localIndex);
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
                ref var scripts = ref CStreamedScripts.Instance;
                var index = scripts.FindSlot(Name);
                if (index.Value != -1)
                {
                    var refs = scripts.GetNumRefs(index);
                    if (refs == 0)
                    {
                        var globalIndex = index.ToGlobal(CStreamedScripts.Instance.ObjectsBaseIndex);

                        if (!Unsafe.IsNullRef(ref scripts.GetPtr(index)))
                        {
                            mgr.OnOutput("Script program still loaded, removing it...");
                            strStreaming.Instance.ClearRequiredFlag(globalIndex, 17);
                            strStreaming.Instance.RemoveObject(globalIndex);
                        }

                        if (mgr.externalRegisteredScripts.Contains(index))
                        {
                            strPackfileManager.InvalidateIndividualFile(Name + ".ysc");
                            mgr.externalRegisteredScripts.Remove(index);
                        }

                        strStreaming.Instance.UnregisterObject(globalIndex);
                        CStreamedScripts.Instance.RemoveSlot(index);

                        mgr.OnOutput($"Unregistered script program '{Name}'");
                    }
                    else
                    {
                        mgr.OnOutput($"Script program '{Name}' still has {refs} reference(s)");
                    }
                }
                else
                {
                    mgr.OnOutput($"Script program '{Name}' does not exist");
                }
            }
        }

        private sealed class StartThreadJob : IJob
        {
            public string ScriptName { get; }
            public uint StackSize { get; }

            public StartThreadJob(string scriptName, uint stackSize) => (ScriptName, StackSize) = (scriptName, stackSize);

            public void Execute(ScriptManager mgr)
            {
                // TODO: load script if not loaded

                var threadId = scrThread.StartNewThreadWithName(ScriptName, IntPtr.Zero, 0, StackSize);
                if (threadId.Value != 0)
                {
                    mgr.OnOutput($"Started script thread with ID {threadId.Value}");
                }
                else
                {
                    mgr.OnOutput($"Failed to start a new script thread");
                }
            }
        }

        private sealed class KillThreadJob : IJob
        {
            public scrThreadId Id { get; }

            public KillThreadJob(scrThreadId id) => Id = id;

            public void Execute(ScriptManager mgr)
            {
                ref var threads = ref scrThread.Threads;
                var exists = false;
                if (Id.Value != 0)
                {
                    for (int i = 0; i < threads.Count; i++)
                    {
                        if (!threads.IsItemNull(i) && threads.ItemDeref(i).Info.ThreadId.Value == Id.Value)
                        {
                            exists = true;
                            break;
                        }
                    }
                }

                if (exists)
                {
                    scrThread.KillThread(Id);
                    mgr.OnOutput($"Script thread with ID {Id.Value} killed");
                }
                else
                {
                    mgr.OnOutput($"Script thread with ID {Id.Value} does not exist");
                }
            }
        }
    }
}
