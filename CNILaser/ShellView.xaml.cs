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

        protected override async void OnClosing(CancelEventArgs e)
        {
            if (_isClosing)
            {
                e.Cancel = false;
                return;
            }

            e.Cancel = true;

            var result = MessageBox.Show(Application.Current.MainWindow,
                "Are you sure you want to exit?", "Exit Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var laserVM = Global.ServiceProvider?.GetService<CNILaserViewModel>();
            if (laserVM != null)
            {
                await laserVM.CloserAllLaserChannel();
            }

            _isClosing = true;
            this.Close();
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