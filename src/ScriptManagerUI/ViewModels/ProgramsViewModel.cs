namespace ScTools.UI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;

    using Avalonia.Threading;

    public record ScriptDef(string Name, uint Index);

    public class ProgramsViewModel : ViewModelBase
    {
        private uint storeSize, storeUsed;

        public uint StoreSize
        {
            get => storeSize;
            set => RaiseAndSetIfChanged(ref storeSize, value);
        }

        public uint StoreUsed
        {
            get => storeUsed;
            set => RaiseAndSetIfChanged(ref storeUsed, value);
        }

        public ObservableCollection<ScriptDef> Scripts { get; } = new();
    }

    public sealed class DummyProgramsViewModel : ProgramsViewModel
    {
        private readonly IDisposable updateTimer;

        public DummyProgramsViewModel()
        {
            StoreSize = 50;
            StoreUsed = 30;

            for (uint i = 1; i <= 1000; i++)
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
    }
}
