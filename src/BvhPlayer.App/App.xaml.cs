using System.IO;
using System.Windows;

namespace BvhPlayer.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show("程序遇到错误：\n" + args.Exception.Message, "BVH Player", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        var window = new MainWindow(e.Args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)));
        MainWindow = window;
        window.Show();
    }
}
