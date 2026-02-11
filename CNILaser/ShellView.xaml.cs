using Microsoft.Extensions.DependencyInjection;
using Simscop.Spindisk.Wpf.Views;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace CNILaser
{
    public partial class ShellView : Lift.UI.Controls.Window
    {
        private bool _isClosing = false;

        public ShellView()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_isClosing)
            {
                e.Cancel = false;
                return;
            }

            // 始终取消默认关闭，由我们自己控制
            e.Cancel = true;

            if (_isClosing) return;

            // 同步弹出确认框（此时窗口还完全正常）
            var result = MessageBox.Show(this,
                "Are you sure you want to exit?", "Exit Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            // 异步执行关闭流程，不阻塞 OnClosing
            _ = CloseAsync();
        }

        private async Task CloseAsync()
        {
            var laserVM = Global.ServiceProvider?.GetService<CNILaserViewModel>();
            if (laserVM != null)
            {
                await laserVM.CloserAllLaserChannel();
            }

            _isClosing = true;

            // 回到 UI 线程执行关闭
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                this.Close();
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        private void Exit_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void UserManual_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string relativePath = @"Docs\多通道激光器软件说明书_2026-02-11_v1.0.0.0.pdf";
            string documentPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            try
            {
                if (System.IO.File.Exists(documentPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = documentPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("User manual not found!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open document: " + ex.Message);
            }
        }

        private void CompanyWebsite_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.simscop.com/WebShop/index.aspx",
                UseShellExecute = true
            });
        }

        private void About_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var aboutWindow = Global.ServiceProvider?.GetService<VersionWindow>();
            aboutWindow!.ShowDialog();
        }

        private void Info_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var paramWindow = Global.ServiceProvider?.GetService<ParamWindow>();
            paramWindow!.Show();
        }
    }
}