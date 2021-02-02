namespace ScTools.UI.Pages
{
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    using ScTools.UI.ViewModels;

    public class ThreadsPage : UserControl
    {
        public ThreadsPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            if (Design.IsDesignMode)
            {
                DataContext = new ThreadsViewModel();
            }
            AvaloniaXamlLoader.Load(this);
        }
    }
}
