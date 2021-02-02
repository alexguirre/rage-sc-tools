namespace ScTools.UI
{
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    using ScTools.UI.ViewModels;

    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
