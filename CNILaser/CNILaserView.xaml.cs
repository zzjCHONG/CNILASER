using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CNILaser
{
    /// <summary>
    /// CNILaserView.xaml 的交互逻辑
    /// </summary>
    public partial class CNILaserView : UserControl
    {
        public CNILaserView()
        {
            InitializeComponent();

            this.DataContext = Global.ServiceProvider?.GetService<CNILaserViewModel>();

            this.Loaded += OnControlLoaded;
            this.Unloaded += OnControlUnloaded;
        }

        #region 输入

        private Window? _parentWindow;
        private bool _isEventRegistered = false;
        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            RegisterGlobalClickEvent();
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            UnregisterGlobalClickEvent();
        }

        private void RegisterGlobalClickEvent()
        {
            if (_isEventRegistered) return;

            // 在父窗口级别注册事件
            _parentWindow = Window.GetWindow(this);
            if (_parentWindow != null)
            {
                _parentWindow.PreviewMouseDown += ParentWindow_PreviewMouseDown;
                _isEventRegistered = true;
            }
        }

        private void UnregisterGlobalClickEvent()
        {
            if (!_isEventRegistered) return;

            if (_parentWindow != null)
            {
                _parentWindow.PreviewMouseDown -= ParentWindow_PreviewMouseDown;
                _parentWindow = null;
            }

            _isEventRegistered = false;
        }

        private void ParentWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not FrameworkElement clickedElement)
                return;

            if (IsClickOnMenu(clickedElement))
                return;

            bool isClickInsideThisControl = IsElementInsideControl(clickedElement, this);
            bool isTextBoxClick = IsDescendantOfTextBox(clickedElement);

            if (isClickInsideThisControl && isTextBoxClick)
                return;

            var focusedTextBox = GetFocusedTextBoxInControl();
            if (focusedTextBox == null)
                return;

            ProcessTextBoxInput(focusedTextBox);
        }

        private static bool IsClickOnMenu(FrameworkElement element)
        {
            if (element == null) return false;

            DependencyObject current = element;
            while (current != null)
            {
                if (current is MenuItem || current is Menu)
                    return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private static bool IsElementInsideControl(FrameworkElement element, FrameworkElement control)
        {
            if (element == null || control == null) return false;

            DependencyObject current = element;
            while (current != null)
            {
                if (current == control)
                    return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private TextBox GetFocusedTextBoxInControl()
        {
            var textBoxes = FindVisualChildren<TextBox>(this);
            return textBoxes.FirstOrDefault(tb => tb.IsFocused)!;
        }

        private static bool IsDescendantOfTextBox(FrameworkElement element)
        {
            while (element != null)
            {
                if (element is TextBox)
                    return true;
                element = element.Parent as FrameworkElement;
            }
            return false;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    var child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (var childOfChild in FindVisualChildren<T>(child!))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ProcessTextBoxInput(sender as TextBox);
                e.Handled = true;
            }
        }

        private static void ProcessTextBoxInput(TextBox textBox)
        {
            if (textBox == null) return;

            if (double.TryParse(textBox.Text, out double doubleValue))
            {
                int value = (int)Math.Ceiling(doubleValue);
                value = Math.Clamp(value, 0, 100);

                textBox.Text = value.ToString(); // 显示为整数

                // 更新绑定源
                var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                binding?.UpdateSource();

                Keyboard.ClearFocus();
            }
            else
            {
                // 非法输入，回退原绑定值
                textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
            }
        }

        #endregion

    }
}
