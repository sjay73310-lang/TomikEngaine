using System.Windows;

namespace Tomk.Editor;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var window = new MainWindow
        {
            WindowState = WindowState.Normal,
            ShowActivated = true,
            Topmost = true
        };

        MainWindow = window;
        window.Show();
        window.Activate();
        window.Topmost = false;
    }
}
