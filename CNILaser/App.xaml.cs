using CNILaser.Core;
using Microsoft.Extensions.DependencyInjection;
using Simscop.Pl.Core.Hardwares.Interfaces;
using Simscop.Spindisk.Wpf.Views;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

namespace CNILaser
{
    public partial class App
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        private void CaptureAllError()
        {
            DispatcherUnhandledException += (s, e) =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"DispatcherUnhandledException -> {s.GetType().ToString()}\n{e.Exception.Message}");
                });
                e.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var error = e.ExceptionObject as Exception;
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"UnhandledException -> {s.GetType().ToString()}\n{error?.Message}");
                });
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"UnobservedTaskException -> {s.GetType().ToString()}\n{e.Exception.Message}");
                });
                e.SetObserved();
            };
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            CaptureAllError();

            Global.ServiceProvider = new ServiceCollection()

             // add interface
             .AddSingleton<ILaser, FakeLaser>()
             .AddSingleton<CNILaserView>()
             .AddSingleton<CNILaserViewModel>()
             .AddSingleton<VersionWindow>()
             .AddSingleton<ShellView>()

             .BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //AllocConsole();  // 创建控制台窗口
            //Console.WriteLine("Console window is now open.");

            var splash = Global.ServiceProvider!.GetService<ShellView>();
            splash?.Show();
        }
    }

}
