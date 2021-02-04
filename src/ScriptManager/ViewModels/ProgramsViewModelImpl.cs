namespace ScTools.ViewModels
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;

    using Avalonia.Controls;
    using Avalonia.Threading;
    using Avalonia.VisualTree;

    using ScTools.Five;
    using ScTools.UI.ViewModels;

    internal sealed class ProgramsViewModelImpl : ProgramsViewModel
    {
        private readonly IDisposable updateTimer;
        private readonly ConcurrentQueue<string> scriptsToRegister = new();

        public ProgramsViewModelImpl()
        {
            updateTimer = DispatcherTimer.Run(Update, TimeSpan.FromSeconds(5));

            PropertyChanged += OnPropertyChanged;
            Util.AfterGameUpdate += GameUpdate;
        }

        ~ProgramsViewModelImpl()
        {
            Util.AfterGameUpdate -= GameUpdate;
            updateTimer.Dispose();
        }

        private bool Update()
        {
            StoreSize = CStreamedScripts.Instance.GetSize();
            StoreUsed = CStreamedScripts.Instance.GetNumUsedSlots();

            return true;
        }

        private void GameUpdate()
        {
            while (scriptsToRegister.TryDequeue(out var scriptPath))
            {
                var fullPath = Path.GetFullPath(scriptPath);
                var fileName = Path.GetFileName(scriptPath);

                var scriptIndex = strPackfileManager.RegisterIndividualFile(fullPath, true, fileName, errorIfFailed: false);
                if (scriptIndex.Value != -1)
                {
                    CStreamedScripts.Instance.StreamingBlockingLoad(scriptIndex.ToLocal(CStreamedScripts.Instance.ObjectsBaseIndex), 17);
                }
            }
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(StoreUsed): OnStoreUsedChanged(); break;
                default: break;
            }
        }


        private void OnStoreUsedChanged()
        {
            if (StoreUsed == 0)
            {
                return;
            }

            for (int i = 0; i < StoreSize; i++)
            {
                var name = CStreamedScripts.Instance.GetAssetName(i);
                if (i < Scripts.Count && Scripts[i].Name != name)
                {
                    Scripts[i] = new ScriptDef(name, i);
                }
                else
                {
                    Scripts.Add(new ScriptDef(name, i));
                }
            }
        }

        public override async void RegisterScript()
        {
            if (View?.GetVisualRoot() is Window w)
            {
                // FIXME: cannot use OpenFileDialog, it uses COM underneath which is not supported by Native AOT
                var dialog = new OpenFileDialog();
                dialog.Filters.Add(new FileDialogFilter { Name = "Script", Extensions = new() { "ysc" } });
                var files = await dialog.ShowAsync(w);
                if (files != null && files.Length > 0)
                {
                    foreach (var f in files)
                    {
                        scriptsToRegister.Enqueue(f);
                    }
                }
            }
        }
    }
}
