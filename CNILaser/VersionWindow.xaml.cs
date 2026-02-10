using System.ComponentModel;
using System.Reflection;

namespace Simscop.Spindisk.Wpf.Views
{
    /// <summary>
    /// VersionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class VersionWindow : Lift.UI.Controls.Window
    {
        public VersionWindow()
        {
            InitializeComponent();

            var assembly = Assembly.GetExecutingAssembly();

            // 读取 FileVersion
            var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            VersionText.Text = $"Version: {fileVersion}";

            // 读取 Company
            var company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
            CompanyText.Text = company;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        private void OkButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
