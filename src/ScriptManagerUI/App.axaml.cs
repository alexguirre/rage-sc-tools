namespace ScTools.UI
{
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Logging.Serilog;
    using Avalonia.Markup.Xaml;

    using ScTools.UI.Pages;
    using ScTools.UI.ViewModels;

    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public static AppBuilder BuildApp<TProgramsViewModel, TThreadsViewModel>()
            where TProgramsViewModel : ProgramsViewModel, new()
            where TThreadsViewModel : ThreadsViewModel, new()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .AfterSetup(b =>
                {
                    if (b.Instance.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        desktop.Startup += (s, e) =>
                        {
                            var w = (MainWindow)desktop.MainWindow;
                            w.Get<ProgramsPage>("Programs").DataContext = new TProgramsViewModel();
                            w.Get<ThreadsPage>("Threads").DataContext = new TThreadsViewModel();
                        };
                    }
                });

    }
}
