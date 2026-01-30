using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace BluetoothBatteryUI
{
    public partial class MainWindow : Window
    {
        private static readonly Guid BatteryServiceUuid = new Guid("0000180F-0000-1000-8000-00805F9B34FB");
        private static readonly Guid BatteryLevelCharacteristicUuid = new Guid("00002A19-0000-1000-8000-00805F9B34FB");
        
        private DeviceWatcher? deviceWatcher;
        private Dictionary<string, Border> deviceCards = new Dictionary<string, Border>();
        private Dictionary<string, int> deviceBatteryLevels = new Dictionary<string, int>();  // 跟踪设备电量
        private Dictionary<string, string> deviceNames = new Dictionary<string, string>();  // 跟踪设备名称
        private bool isScanning = false;
        private bool showConnectedOnly = true;  // 默认只显示已连接设备
        private AppSettings settings;

        public MainWindow()
        {
            InitializeComponent();
            
            // 加载设置
            settings = SettingsManager.LoadSettings();
            Logger.SetDetailedLogging(settings.DetailedLogging);
            
            // 应用启动设置
            if (settings.StartMinimized)
            {
                WindowState = WindowState.Minimized;
            }
            
            Logger.Log("应用程序启动");
        }




        private void ConnectedOnlyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            showConnectedOnly = ConnectedOnlyCheckBox.IsChecked ?? true;
            
            // 如果正在扫描，重新开始扫描以应用新的过滤设置
            if (isScanning)
            {
                StopScanning();
                StartScanning();
            }
        }




        private async Task RefreshAllBatteryLevelsAsync()
        {
            if (deviceCards.Count == 0)
            {
                UpdateStatus("没有设备需要刷新", Colors.Orange);
                StopRefreshAnimation();
                return;
            }

            UpdateStatus($"正在刷新 {deviceCards.Count} 个设备的电量...", Colors.Orange);
            Logger.Log("开始刷新所有设备电量");

            try
            {
                var tasks = deviceCards.Keys.Select(deviceId => RefreshDeviceBattery(deviceId));
                await Task.WhenAll(tasks);

                int count = deviceCards.Count;
                UpdateStatus($"已刷新 {count} 个设备的电量", Colors.LightGreen);
                Logger.Log($"刷新完成，共 {count} 个设备");
            }
            catch (Exception ex)
            {
                Logger.Log($"批量刷新电量时出错: {ex.Message}");
                UpdateStatus("刷新电量时部分失败", Colors.Red);
            }
            finally
            {
                StopRefreshAnimation();
            }
        }

        private async Task RefreshDeviceBattery(string deviceId)
        {
            try
            {
                var batteryLevel = await ReadBatteryLevelAsync(deviceId);
                
                if (batteryLevel >= 0)
                {
                    deviceBatteryLevels[deviceId] = batteryLevel;
                    UpdateDeviceCardBattery(deviceId, batteryLevel);
                    UpdateLowestBatteryDisplay();
                    
                    // 检查低电量提醒
                    CheckLowBattery(deviceId, batteryLevel);
                    
                    Logger.Log($"设备 {deviceId} 电量已刷新: {batteryLevel}%", true);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"刷新设备 {deviceId} 电量失败: {ex.Message}");
            }
        }

        private void UpdateDeviceCardBattery(string deviceId, int batteryLevel)
        {
            if (!deviceCards.ContainsKey(deviceId)) return;

            var card = deviceCards[deviceId];
            var grid = (Grid)card.Child;
            
            // 更新右侧电量显示
            var rightPanel = (StackPanel)grid.Children[1];
            var batteryPercentText = (TextBlock)rightPanel.Children[0];
            var progressBar = (ProgressBar)rightPanel.Children[1];
            
            batteryPercentText.Text = $"{batteryLevel}%";
            progressBar.Value = batteryLevel;
            
            // 更新颜色
            var color = batteryLevel > 50 ? Color.FromRgb(76, 175, 80) :
                       batteryLevel > 20 ? Color.FromRgb(255, 152, 0) :
                       Color.FromRgb(244, 67, 54);
            progressBar.Foreground = new SolidColorBrush(color);
            batteryPercentText.Foreground = new SolidColorBrush(color);
        }

        private void HiddenDevices_Click(object sender, RoutedEventArgs e)
        {
            var window = new HiddenDevicesWindow(settings, deviceNames);
            window.DeviceRestored += HiddenDevicesWindow_DeviceRestored;
            window.Owner = this;
            window.ShowDialog();
        }

        private void HiddenDevicesWindow_DeviceRestored(object? sender, string deviceId)
        {
            // 如果正在扫描，设备会自动重新出现
            Logger.Log($"设备 {deviceId} 已从隐藏列表中恢复");
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(settings);
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                // 设置已保存，重新加载
                settings = SettingsManager.LoadSettings();
                Logger.SetDetailedLogging(settings.DetailedLogging);
                Logger.Log("设置已更新");
            }
        }

        private void CheckLowBattery(string deviceId, int batteryLevel)
        {
            if (!settings.EnableLowBatteryAlert) return;
            if (batteryLevel > settings.LowBatteryThreshold) return;
            if (settings.AlertedDevices.Contains(deviceId)) return;

            string deviceName = deviceNames.ContainsKey(deviceId) ? deviceNames[deviceId] : "未知设备";
            
            settings.AlertedDevices.Add(deviceId);
            SettingsManager.SaveSettings(settings);

            if (settings.UseToastNotification)
            {
                ShowToastNotification(deviceName, batteryLevel);
            }
            else
            {
                MessageBox.Show(
                    $"设备 \"{deviceName}\" 电量低于 {settings.LowBatteryThreshold}%\n当前电量: {batteryLevel}%",
                    "低电量提醒",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
            
            Logger.Log($"低电量提醒: {deviceName} ({batteryLevel}%)");
        }

        private void ShowToastNotification(string deviceName, int batteryLevel)
        {
            try
            {
                // 简单的系统托盘通知（Windows 10/11）
                var notificationTitle = "蓝牙设备低电量";
                var notificationMessage = $"{deviceName} 电量仅剩 {batteryLevel}%";
                
                // 使用 MessageBox 作为备选方案
                MessageBox.Show(notificationMessage, notificationTitle, 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                Logger.Log($"显示通知失败: {ex.Message}");
            }
        }

        private void HideDevice(string deviceId)
        {
            if (!deviceCards.ContainsKey(deviceId)) return;

            // 添加到隐藏列表
            if (!settings.HiddenDeviceIds.Contains(deviceId))
            {
                settings.HiddenDeviceIds.Add(deviceId);
                SettingsManager.SaveSettings(settings);
            }

            // 从UI中移除
            var card = deviceCards[deviceId];
            DeviceListPanel.Children.Remove(card);
            deviceCards.Remove(deviceId);
            deviceBatteryLevels.Remove(deviceId);

            // 更新设备计数
            DeviceCountText.Text = $"已找到 {deviceCards.Count} 个设备";
            
            // 更新最低电量显示
            UpdateLowestBatteryDisplay();

            string deviceName = deviceNames.ContainsKey(deviceId) ? deviceNames[deviceId] : "未知设备";
            Logger.Log($"已隐藏设备: {deviceName}");

            // 如果没有设备了，显示空状态
            if (deviceCards.Count == 0)
            {
                ShowEmptyState("没有可显示的设备", "所有设备都已被隐藏");
            }
        }

        private void StartScanning()
        {
            try
            {
                isScanning = true;
                StartScanAnimation();
                UpdateStatus("正在扫描蓝牙设备...", Colors.Orange);
                
                // 清空设备列表和缓存
                DeviceListPanel.Children.Clear();
                deviceCards.Clear();
                deviceBatteryLevels.Clear();  // 清空电量记录
                EmptyState.Visibility = Visibility.Collapsed;
                DeviceCountText.Text = "已找到 0 个设备";

                // 创建 DeviceWatcher（非阻塞式扫描）
                string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };
                string selector = BluetoothLEDevice.GetDeviceSelector();
                
                deviceWatcher = DeviceInformation.CreateWatcher(
                    selector,
                    requestedProperties,
                    DeviceInformationKind.AssociationEndpoint);

                // 注册事件处理器
                deviceWatcher.Added += DeviceWatcher_Added;
                deviceWatcher.Updated += DeviceWatcher_Updated;
                deviceWatcher.Removed += DeviceWatcher_Removed;
                deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
                deviceWatcher.Stopped += DeviceWatcher_Stopped;

                // 开始扫描（立即返回，不阻塞）
                deviceWatcher.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动扫描时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("扫描失败", Colors.Red);
                isScanning = false;
                StopScanAnimation();
            }
        }

        private void StopScanning()
        {
            if (deviceWatcher != null)
            {
                // 注销事件处理器
                deviceWatcher.Added -= DeviceWatcher_Added;
                deviceWatcher.Updated -= DeviceWatcher_Updated;
                deviceWatcher.Removed -= DeviceWatcher_Removed;
                deviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
                deviceWatcher.Stopped -= DeviceWatcher_Stopped;
                
                deviceWatcher.Stop();
                deviceWatcher = null;
            }
            
            isScanning = false;
            UpdateStatus("扫描结束", Colors.Gray);
            StopScanAnimation();
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                if (!deviceCards.ContainsKey(deviceInfo.Id))
                {
                    // 检查是否在隐藏列表中
                    if (settings.HiddenDeviceIds.Contains(deviceInfo.Id))
                    {
                        Logger.Log($"跳过隐藏的设备: {deviceInfo.Id}", true);
                        return;
                    }
                    
                    // 检查连接状态
                    bool isConnected = await IsDeviceConnectedAsync(deviceInfo);
                    
                    // 如果启用了"仅显示已连接"过滤，则跳过未连接的设备
                    if (showConnectedOnly && !isConnected)
                    {
                        return;
                    }
                    
                    await CreateDeviceCardAsync(deviceInfo);
                    DeviceCountText.Text = $"已找到 {deviceCards.Count} 个设备";
                }
            });
        }

        private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            // 可以在这里处理设备信息更新
            await Dispatcher.InvokeAsync(() =>
            {
                // 暂时不处理更新事件
            });
        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (deviceCards.ContainsKey(deviceInfoUpdate.Id))
                {
                    var card = deviceCards[deviceInfoUpdate.Id];
                    DeviceListPanel.Children.Remove(card);
                    deviceCards.Remove(deviceInfoUpdate.Id);
                    DeviceCountText.Text = $"已找到 {deviceCards.Count} 个设备";
                }
            });
        }

        private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                UpdateStatus($"扫描完成 - 找到 {deviceCards.Count} 个设备", Colors.LightGreen);
                
                if (deviceCards.Count == 0)
                {
                    ShowEmptyState("未找到蓝牙设备", "请确保蓝牙已开启且设备在范围内");
                }
                
                // 扫描完成后自动停止，按钮变回"扫描设备"
                StopScanning();
            });
        }

        private async void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                isScanning = false;
            });
        }

        private async Task<bool> IsDeviceConnectedAsync(DeviceInformation deviceInfo)
        {
            try
            {
                // 检查设备属性中的连接状态
                if (deviceInfo.Properties.TryGetValue("System.Devices.Aep.IsConnected", out object? isConnectedObj))
                {
                    if (isConnectedObj is bool isConnected)
                    {
                        return isConnected;
                    }
                }
                
                // 尝试通过 BluetoothLEDevice 检查连接状态
                var device = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
                if (device != null)
                {
                    var connected = device.ConnectionStatus == BluetoothConnectionStatus.Connected;
                    device.Dispose();
                    return connected;
                }
            }
            catch
            {
                // 忽略错误，默认返回 false
            }
            
            return false;
        }

        private async Task CreateDeviceCardAsync(DeviceInformation deviceInfo)
        {
            // 检查连接状态
            bool isConnected = await IsDeviceConnectedAsync(deviceInfo);
            
            // 记录设备名称
            string devName = string.IsNullOrWhiteSpace(deviceInfo.Name) ? "未命名设备" : deviceInfo.Name;
            deviceNames[deviceInfo.Id] = devName;
            
            // 创建设备卡片
            var card = new Border
            {
                Style = (Style)FindResource("DeviceCard"),
                Opacity = 0
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 左侧：设备信息
            var leftPanel = new StackPanel();

            // 设备名称和连接状态
            var namePanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var deviceName = new TextBlock
            {
                Text = devName,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            namePanel.Children.Add(deviceName);
            
            // 连接状态标签
            var statusBadge = new Border
            {
                Background = new SolidColorBrush(isConnected ? Color.FromRgb(76, 175, 80) : Color.FromRgb(128, 128, 128)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var statusText = new TextBlock
            {
                Text = isConnected ? "已连接" : "未连接",
                FontSize = 11,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold
            };
            statusBadge.Child = statusText;
            namePanel.Children.Add(statusBadge);
            
            // 隐藏按钮
            var hideButton = new Button
            {
                Content = "隐藏",
                FontSize = 11,
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            hideButton.Click += (s, e) => HideDevice(deviceInfo.Id);
            namePanel.Children.Add(hideButton);
            
            leftPanel.Children.Add(namePanel);
            leftPanel.Children.Add(new TextBlock { Height = 8 }); // 间距

            var deviceId = new TextBlock
            {
                Text = $"ID: {deviceInfo.Id.Substring(Math.Max(0, deviceInfo.Id.Length - 20))}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                Margin = new Thickness(0, 0, 0, 12)
            };
            leftPanel.Children.Add(deviceId);

            // 电池信息容器
            var batteryPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var batteryIcon = new TextBlock
            {
                Text = "🔋",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            batteryPanel.Children.Add(batteryIcon);

            var batteryText = new TextBlock
            {
                Text = "正在读取...",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                VerticalAlignment = VerticalAlignment.Center
            };
            batteryPanel.Children.Add(batteryText);

            leftPanel.Children.Add(batteryPanel);

            Grid.SetColumn(leftPanel, 0);
            grid.Children.Add(leftPanel);

            // 右侧：电池电量可视化
            var rightPanel = new StackPanel
            {
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center
            };

            var batteryPercentText = new TextBlock
            {
                Text = "--",
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            rightPanel.Children.Add(batteryPercentText);

            var progressBar = new ProgressBar
            {
                Style = (Style)FindResource("BatteryProgressBar"),
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            rightPanel.Children.Add(progressBar);

            Grid.SetColumn(rightPanel, 1);
            grid.Children.Add(rightPanel);

            card.Child = grid;
            DeviceListPanel.Children.Add(card);
            
            // 将卡片添加到缓存
            deviceCards[deviceInfo.Id] = card;

            // 淡入动画
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            card.BeginAnimation(OpacityProperty, fadeIn);

            // 异步读取电池电量
            _ = Task.Run(async () =>
            {
                var batteryLevel = await ReadBatteryLevelAsync(deviceInfo.Id);

                await Dispatcher.InvokeAsync(() =>
                {
                    if (batteryLevel >= 0)
                    {
                        // 记录设备电量
                        deviceBatteryLevels[deviceInfo.Id] = batteryLevel;
                        
                        // 根据连接状态显示不同的文本
                        if (isConnected)
                        {
                            batteryText.Text = $"电池电量: {batteryLevel}%";
                        }
                        else
                        {
                            batteryText.Text = $"上次已知电量: {batteryLevel}%";
                            batteryText.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));
                        }
                        
                        batteryPercentText.Text = $"{batteryLevel}%";
                        progressBar.Value = batteryLevel;

                        // 根据电量设置颜色
                        var color = batteryLevel > 50 ? Color.FromRgb(76, 175, 80) :
                                   batteryLevel > 20 ? Color.FromRgb(255, 152, 0) :
                                   Color.FromRgb(244, 67, 54);
                        
                        // 未连接的设备使用灰色调
                        if (!isConnected)
                        {
                            color = Color.FromRgb(
                                (byte)(color.R * 0.6),
                                (byte)(color.G * 0.6),
                                (byte)(color.B * 0.6)
                            );
                        }
                        
                        progressBar.Foreground = new SolidColorBrush(color);
                        batteryPercentText.Foreground = new SolidColorBrush(color);
                        
                        // 更新最低电量显示
                        UpdateLowestBatteryDisplay();
                    }
                    else
                    {
                        batteryText.Text = "不支持电池服务";
                        batteryPercentText.Text = "N/A";
                        batteryPercentText.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
                    }
                });
            });
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (isScanning)
            {
                StopScanning();
            }
            else
            {
                StartScanning();
            }
        }

        private async void RefreshBattery_Click(object sender, RoutedEventArgs e)
        {
            StartRefreshAnimation();
            await RefreshAllBatteryLevelsAsync();
        }

        private void StartScanAnimation()
        {
            if (ScanRotate != null)
            {
                var animation = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = new Duration(TimeSpan.FromSeconds(1)),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                ScanRotate.BeginAnimation(RotateTransform.AngleProperty, animation);
                
                // Dim opacity for feedback
                if (ScanIconPath != null)
                    ScanIconPath.Opacity = 0.6;
            }
        }

        private void StopScanAnimation()
        {
            ScanRotate?.BeginAnimation(RotateTransform.AngleProperty, null);
            if (ScanIconPath != null)
                ScanIconPath.Opacity = 1.0;
        }

        private void StartRefreshAnimation()
        {
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                RepeatBehavior = RepeatBehavior.Forever
            };

            BatteryRingRotate?.BeginAnimation(RotateTransform.AngleProperty, animation);
            
            // Dim opacity of the rotating group for feedback
            if (BatteryOuterGroup != null) BatteryOuterGroup.Opacity = 0.6;
        }

        private void StopRefreshAnimation()
        {
            BatteryRingRotate?.BeginAnimation(RotateTransform.AngleProperty, null);
            
            // Restore opacity
            if (BatteryOuterGroup != null) BatteryOuterGroup.Opacity = 1.0;
        }

        private void UpdateLowestBatteryDisplay()
        {
            if (deviceBatteryLevels.Count == 0)
            {
                LowestBatteryPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // 找到电量最低的设备
            var lowestEntry = deviceBatteryLevels.OrderBy(x => x.Value).First();
            var lowestDeviceId = lowestEntry.Key;
            var lowestBattery = lowestEntry.Value;

            // 获取设备名称
            var deviceCard = deviceCards[lowestDeviceId];
            var grid = (Grid)deviceCard.Child;
            var leftPanel = (StackPanel)grid.Children[0];
            var namePanel = (StackPanel)leftPanel.Children[0];
            var deviceNameBlock = (TextBlock)namePanel.Children[0];
            var deviceName = deviceNameBlock.Text;

            // 更新显示
            LowestBatteryDeviceName.Text = deviceName;
            LowestBatteryLevel.Text = $"{lowestBattery}%";
            
            // 根据电量设置颜色
            var color = lowestBattery > 20 ? "#FF9800" : "#F44336";  // 橙色或红色
            LowestBatteryLevel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            
            LowestBatteryPanel.Visibility = Visibility.Visible;
        }

        private async Task<int> ReadBatteryLevelAsync(string deviceId)
        {
            // 添加超时机制 (5秒)
            var task = ReadBatteryLevelCoreAsync(deviceId);
            if (await Task.WhenAny(task, Task.Delay(5000)) == task)
            {
                return await task;
            }
            else
            {
                Logger.Log($"读取设备 {deviceId} 电量超时");
                return -1;
            }
        }

        private async Task<int> ReadBatteryLevelCoreAsync(string deviceId)
        {
            BluetoothLEDevice device = null;

            try
            {
                device = await BluetoothLEDevice.FromIdAsync(deviceId);
                if (device == null) return -1;

                var servicesResult = await device.GetGattServicesForUuidAsync(BatteryServiceUuid);
                if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
                    return -1;

                var batteryService = servicesResult.Services[0];
                var characteristicsResult = await batteryService.GetCharacteristicsForUuidAsync(BatteryLevelCharacteristicUuid);

                if (characteristicsResult.Status != GattCommunicationStatus.Success || characteristicsResult.Characteristics.Count == 0)
                    return -1;

                var batteryLevelCharacteristic = characteristicsResult.Characteristics[0];
                var readResult = await batteryLevelCharacteristic.ReadValueAsync();

                if (readResult.Status != GattCommunicationStatus.Success)
                    return -1;

                var reader = Windows.Storage.Streams.DataReader.FromBuffer(readResult.Value);
                return reader.ReadByte();
            }
            catch
            {
                return -1;
            }
            finally
            {
                device?.Dispose();
            }
        }

        private void ShowLoading(bool show)
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateStatus(string message, Color color)
        {
            StatusText.Text = message;
            StatusIndicator.Fill = new SolidColorBrush(color);
        }

        private void ShowEmptyState(string title, string subtitle)
        {
            DeviceListPanel.Children.Clear();
            EmptyState.Visibility = Visibility.Visible;

            var stack = (StackPanel)EmptyState.Child;
            ((TextBlock)stack.Children[1]).Text = title;
            ((TextBlock)stack.Children[2]).Text = subtitle;
        }
    }
}

