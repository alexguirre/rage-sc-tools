namespace ScTools.ViewModels
{
    using System;

    using Avalonia.Threading;

    using ScTools.Five;
    using ScTools.UI.ViewModels;

    internal sealed class ProgramsViewModelImpl : ProgramsViewModel
    {
        private readonly IDisposable updateTimer;

        public ProgramsViewModelImpl()
        {
            updateTimer = DispatcherTimer.Run(Update, TimeSpan.FromSeconds(5));

            PropertyChanged += OnStoreUsedChanged;
        }

        ~ProgramsViewModelImpl()
        {
            updateTimer.Dispose();
        }

        private bool Update()
        {
            StoreSize = CStreamedScripts.Instance.GetSize();
            StoreUsed = CStreamedScripts.Instance.GetNumUsedSlots();

            return true;
        }

        private void OnStoreUsedChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (StoreUsed == 0)
            {
                return;
            }

            for (uint i = 0; i < StoreSize; i++)
            {
                var name = CStreamedScripts.Instance.GetAssetName(i);
                if (i < Scripts.Count && Scripts[(int)i].Name != name)
                {
                    Scripts[(int)i] = new ScriptDef(name, i);
                }
                else
                {
                    Scripts.Add(new ScriptDef(name, i));
                }
            }
        }
    }
}
