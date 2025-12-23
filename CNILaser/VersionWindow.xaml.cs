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

            Assembly assembly = Assembly.GetExecutingAssembly();

            string assemblyVersion = assembly.GetName().Version?.ToString() ?? "未知";
            string fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "未知";
            string infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "未知";
            string name = assembly.GetName().Name ?? "未知程序集";// 名称
            string company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "未知公司";// 公司
            string product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "未知产品"; // 产品
            string title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "未知标题";// 标题

            SoftwareName.Text = title;
            CompanyText.Text = $"公司：{company}";
            VersionText.Text = $"版本号：v{fileVersion}";
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
