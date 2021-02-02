namespace ScTools.UI.Pages
{
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
    }
}
