using System;
using System.Diagnostics;
using System.Windows;

namespace BluetoothBatteryUI
{
    public partial class SettingsWindow : Window
    {
        private AppSettings settings;

        public SettingsWindow(AppSettings currentSettings)
        {
            InitializeComponent();
            settings = currentSettings;
            LoadSettings();
        }

        private void LoadSettings()
        {
            ThresholdTextBox.Text = settings.LowBatteryThreshold.ToString();
            RefreshIntervalTextBox.Text = settings.AutoRefreshIntervalMinutes.ToString();
            EnableAutoRefreshCheckBox.IsChecked = settings.EnableAutoRefresh;
            EnableAlertCheckBox.IsChecked = settings.EnableLowBatteryAlert;
            UseToastCheckBox.IsChecked = settings.UseToastNotification;
            StartupCheckBox.IsChecked = settings.StartWithWindows;
            MinimizeToTrayCheckBox.IsChecked = settings.MinimizeToTray;
            DetailedLoggingCheckBox.IsChecked = settings.DetailedLogging;
        }

        private void IncreaseThreshold_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ThresholdTextBox.Text, out int value))
            {
                if (value < 100)
                {
                    ThresholdTextBox.Text = (value + 1).ToString();
                }
            }
        }

        private void DecreaseThreshold_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ThresholdTextBox.Text, out int value))
            {
                if (value > 1)
                {
                    ThresholdTextBox.Text = (value - 1).ToString();
                }
            }
        }

        private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logFolder = Logger.GetLogFolder();
                if (!System.IO.Directory.Exists(logFolder))
                {
                    System.IO.Directory.CreateDirectory(logFolder);
                }
                Process.Start("explorer.exe", logFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开日志文件夹: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证阈值
                if (!int.TryParse(ThresholdTextBox.Text, out int threshold) || 
                    threshold < 1 || threshold > 100)
                {
                    MessageBox.Show("低电量阈值必须在 1-100 之间", "输入错误", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 验证刷新间隔
                if (!int.TryParse(RefreshIntervalTextBox.Text, out int refreshInterval) || 
                    refreshInterval < 1 || refreshInterval > 1440)
                {
                    MessageBox.Show("刷新间隔必须在 1-1440 分钟之间", "输入错误", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 保存设置
                settings.LowBatteryThreshold = threshold;
                settings.AutoRefreshIntervalMinutes = refreshInterval;
                settings.EnableAutoRefresh = EnableAutoRefreshCheckBox.IsChecked ?? false;
                settings.EnableLowBatteryAlert = EnableAlertCheckBox.IsChecked ?? false;
                settings.UseToastNotification = UseToastCheckBox.IsChecked ?? false;
                settings.StartWithWindows = StartupCheckBox.IsChecked ?? false;
                settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? false;
                settings.DetailedLogging = DetailedLoggingCheckBox.IsChecked ?? false;

                // 应用开机自启动设置
                StartupManager.SetStartup(settings.StartWithWindows);

                // 应用日志设置
                Logger.SetDetailedLogging(settings.DetailedLogging);

                // 保存到文件
                SettingsManager.SaveSettings(settings);

                MessageBox.Show("设置已保存", "成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
