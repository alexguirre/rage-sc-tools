namespace ScTools.UI.Pages
{
    using System;

    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    using ScTools.UI.ViewModels;

    public class ProgramsPage : UserControl
    {
        public ProgramsPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            if (Design.IsDesignMode)
            {
                DataContext = new ProgramsViewModel();
            }
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            if (DataContext is ViewModelBase model)
            {
                model.View = this;
            }

            base.OnDataContextChanged(e);
        }
    }
}
