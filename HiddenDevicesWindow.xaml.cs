using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BluetoothBatteryUI
{
    public partial class HiddenDevicesWindow : Window
    {
        private AppSettings settings;
        private Dictionary<string, string> deviceNames;
        public event EventHandler<string> DeviceRestored;

        public HiddenDevicesWindow(AppSettings settings, Dictionary<string, string> deviceNames)
        {
            InitializeComponent();
            this.settings = settings;
            this.deviceNames = deviceNames;
            LoadHiddenDevices();
        }

        private void LoadHiddenDevices()
        {
            HiddenDevicesPanel.Children.Clear();

            if (settings.HiddenDeviceIds.Count == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
                return;
            }

            EmptyState.Visibility = Visibility.Collapsed;

            foreach (var deviceId in settings.HiddenDeviceIds)
            {
                string deviceName = deviceNames.ContainsKey(deviceId) 
                    ? deviceNames[deviceId] 
                    : "未知设备";

                var deviceCard = CreateDeviceCard(deviceId, deviceName);
                HiddenDevicesPanel.Children.Add(deviceCard);
            }
        }

        private Border CreateDeviceCard(string deviceId, string deviceName)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 设备信息
            var infoPanel = new StackPanel();
            
            var nameText = new TextBlock
            {
                Text = deviceName,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            infoPanel.Children.Add(nameText);

            var idText = new TextBlock
            {
                Text = $"ID: {deviceId.Substring(Math.Max(0, deviceId.Length - 20))}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170))
            };
            infoPanel.Children.Add(idText);

            grid.Children.Add(infoPanel);

            // 按钮面板
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var restoreButton = new Button
            {
                Content = "恢复显示",
                Style = (Style)FindResource("DeviceButton"),
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80))
            };
            restoreButton.Click += (s, e) => RestoreDevice(deviceId);
            buttonPanel.Children.Add(restoreButton);

            Grid.SetColumn(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            card.Child = grid;
            return card;
        }

        private void RestoreDevice(string deviceId)
        {
            settings.HiddenDeviceIds.Remove(deviceId);
            SettingsManager.SaveSettings(settings);
            
            DeviceRestored?.Invoke(this, deviceId);
            
            LoadHiddenDevices();
            
            Logger.Log($"恢复显示设备: {deviceId}");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
