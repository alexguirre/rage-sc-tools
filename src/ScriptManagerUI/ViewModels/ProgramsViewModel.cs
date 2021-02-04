namespace ScTools.UI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;

    using Avalonia.Controls;
    using Avalonia.Threading;
    using Avalonia.VisualTree;

    public record ScriptDef(string Name, int Index);

    public class ProgramsViewModel : ViewModelBase
    {
        private int storeSize, storeUsed;

        public int StoreSize
        {
            get => storeSize;
            set => RaiseAndSetIfChanged(ref storeSize, value);
        }

        public int StoreUsed
        {
            get => storeUsed;
            set => RaiseAndSetIfChanged(ref storeUsed, value);
        }

        public ObservableCollection<ScriptDef> Scripts { get; } = new();

        public virtual void RegisterScript()
        {
        }
    }

    public sealed class DummyProgramsViewModel : ProgramsViewModel
    {
        private readonly IDisposable updateTimer;

        public DummyProgramsViewModel()
        {
            StoreSize = 50;
            StoreUsed = 30;

            for (int i = 0; i < 1000; i++)
            {
                Scripts.Add(new ScriptDef($"script_{i}", i));
            }

            updateTimer = DispatcherTimer.Run(Update, TimeSpan.FromSeconds(5));
        }

        ~DummyProgramsViewModel()
        {
            updateTimer.Dispose();
        }

        private bool Update()
        {
            StoreSize++;
            StoreUsed++;

            return true;
        }

        public override async void RegisterScript()
        {
            if (View?.GetVisualRoot() is Window w)
            {
                var dialog = new OpenFileDialog { AllowMultiple = true };
                dialog.Filters.Add(new FileDialogFilter { Name = "Script", Extensions = new() { "ysc" } });
                var file = await dialog.ShowAsync(w);
                if (file != null && file.Length > 0)
                {

                }
            }
        }
    }
}
