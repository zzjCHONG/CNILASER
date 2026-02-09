using Microsoft.Extensions.DependencyInjection;
using Simscop.Spindisk.Wpf.Views;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace CNILaser
{
    /// <summary>
    /// ShellView.xaml 的交互逻辑
    /// </summary>
    public partial class ShellView : Lift.UI.Controls.Window
    {
        public ShellView()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            bool shouldContinue = false;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                var result = MessageBox.Show(Application.Current.MainWindow, "确定要退出吗？", "退出确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

                shouldContinue = result == MessageBoxResult.Yes;
            });

            if (shouldContinue)
            {
                //关闭设备，激光器各通道关闭
                var laserVM = Global.ServiceProvider?.GetService<CNILaserViewModel>();
                var res = laserVM!.CloserAllLaserChannel();

                base.OnClosing(e);
            }
            else
            {
                e.Cancel = true; // 阻止关闭
                return;
            }
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
            string relativePath = @"Docs\激光器软件说明文档_2024.06.20_v1.0.0.0.pdf";
            string documentPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            try
            {
                if (System.IO.File.Exists(documentPath))
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = documentPath,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                else
                {
                    MessageBox.Show("无法找到说明文档！");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("无法打开文档：" + ex.Message);
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
