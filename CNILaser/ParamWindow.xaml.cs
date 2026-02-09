using System.ComponentModel;

namespace CNILaser
{
    /// <summary>
    /// ParamWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ParamWindow : Lift.UI.Controls.Window
    {
        public ParamWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }
    }
}
