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
            
            // 预选当前图标 logic (简化版)
            if (string.IsNullOrEmpty(currentIcon) || currentIcon == "Default")
            {
                DefaultIconRadio.IsChecked = true;
            }
        }

        private void Icon_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                SelectedIcon = rb.Content.ToString();
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
