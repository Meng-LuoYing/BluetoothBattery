using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;

namespace BluetoothBatteryUI
{
    public partial class IconPickerDialog : Window
    {
        public string SelectedIcon { get; private set; } = "Default";
        public bool IsCustomImage { get; private set; } = false;

        public IconPickerDialog(string currentIcon)
        {
            InitializeComponent();
            
            // 预选当前图标 logic
            if (string.IsNullOrEmpty(currentIcon) || currentIcon == "Default")
            {
                DefaultIconRadio.IsChecked = true;
                PreviewIcon.Text = "\uE972";
            }
            else
            {
                // 尝试查找对应的 RadioButton 并选中 (简单遍历)
                // 注意：这里需要遍历所有 RadioButton，但在构造函数中 UI 可能还没完全加载，
                // 比较合适的方式是依靠 Binding 或者简单的检查
                // 此时 currentIcon 可能是 "\uE962"
                PreviewIcon.Text = currentIcon;
                
                // 由于面板控件较多，简单起见我们不自动选中具体的 Preset RadioButton，
                // 但至少初始化 Preview。如果需要选中，可以遍历容器。
            }
        }

        private void Tab_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                // 隐藏所有面板
                DefaultPanel.Visibility = Visibility.Collapsed;
                PresetPanel.Visibility = Visibility.Collapsed;
                CustomPanel.Visibility = Visibility.Collapsed;

                // 显示对应面板
                if (rb == DefaultTab)
                {
                    DefaultPanel.Visibility = Visibility.Visible;
                }
                else if (rb == PresetTab)
                {
                    PresetPanel.Visibility = Visibility.Visible;
                }
                else if (rb == CustomTab)
                {
                    CustomPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private void Icon_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string iconCode)
            {
                // 更新预览
                PreviewIcon.Text = iconCode;
                
                // 保存选择
                SelectedIcon = iconCode;
                IsCustomImage = false;
            }
        }

        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.ico)|*.png;*.jpg;*.jpeg;*.ico",
                Title = "选择图标图片"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedIcon = openFileDialog.FileName;
                IsCustomImage = true;
                DialogResult = true; // 选中文件直接确认
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
