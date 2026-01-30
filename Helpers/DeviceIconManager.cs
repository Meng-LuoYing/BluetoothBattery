using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BluetoothBatteryUI
{
    public static class DeviceIconManager
    {
        public static object GetIconForDevice(string deviceId, string deviceName, AppSettings settings)
        {
            // 1. 检查是否存在自定义图标设置
            if (settings.DeviceIcons.TryGetValue(deviceId, out string iconSetting))
            {
                // 如果是文件路径（包含冒号或斜杠）
                if (iconSetting.Contains(":") || iconSetting.Contains("\\") || iconSetting.Contains("/"))
                {
                    if (File.Exists(iconSetting))
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(iconSetting);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad; // 加载到内存，防止文件占用
                            bitmap.DecodePixelWidth = 100; // 限制大小
                            bitmap.EndInit();
                            
                            var image = new Image
                            {
                                Source = bitmap,
                                Width = 32,
                                Height = 32,
                                Stretch = Stretch.Uniform,
                                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                Margin = new System.Windows.Thickness(0, 0, 15, 0)
                            };
                            
                            // 圆形裁剪
                            var ellipseGeometry = new EllipseGeometry(new System.Windows.Point(16, 16), 16, 16);
                            image.Clip = ellipseGeometry;
                            
                            return image;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"加载自定义图标失败: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // 预设图标 (名称对应 Unicode 字符)
                    string symbol = GetSymbolForPreset(iconSetting);
                    return CreatePresetIcon(symbol);
                }
            }

            // 2. 如果没有自定义，根据名称自动判断
            string defaultSymbol = GetDefaultSymbolForDevice(deviceName);
            return CreatePresetIcon(defaultSymbol);
        }

        private static string GetSymbolForPreset(string presetName)
        {
            switch (presetName)
            {
                case "鼠标": return "\uE962";
                case "键盘": return "\uE967";
                case "耳机": return "\uE95B";
                case "手柄": return "\uE7FC";
                case "手机": return "\uE8EA";
                case "笔": return "\uEDC6";
                case "音箱": return "\uE7F5";
                default: return "\uE9D9"; // 默认Generic
            }
        }

        private static string GetDefaultSymbolForDevice(string deviceName)
        {
            string name = deviceName.ToLower();

            if (name.Contains("mouse") || name.Contains("鼠标")) return "\uE962";
            if (name.Contains("keyboard") || name.Contains("keypad") || name.Contains("键盘")) return "\uE967";
            if (name.Contains("headset") || name.Contains("headphone") || name.Contains("earbud") || name.Contains("airpods") || name.Contains("耳机")) return "\uE95B";
            if (name.Contains("gamepad") || name.Contains("controller") || name.Contains("xbox") || name.Contains("dualshock") || name.Contains("手柄")) return "\uE7FC";
            if (name.Contains("phone") || name.Contains("iphone") || name.Contains("android") || name.Contains("手机")) return "\uE8EA";
            if (name.Contains("pen") || name.Contains("pencil") || name.Contains("stylus") || name.Contains("笔")) return "\uEDC6";
            if (name.Contains("speaker") || name.Contains("sound") || name.Contains("音箱")) return "\uE7F5";

            return "\uE972"; // 默认蓝牙图标
        }

        private static Border CreatePresetIcon(string symbol)
        {
            // 创建带圆圈的图标容器
            var border = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new System.Windows.CornerRadius(20), // 圆形
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), // 半透明白色边框
                BorderThickness = new System.Windows.Thickness(1),
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)), // 轻微背景色
                Margin = new System.Windows.Thickness(0, 0, 15, 0),
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            var textBlock = new TextBlock
            {
                Text = symbol,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 20,
                Foreground = Brushes.White,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            border.Child = textBlock;
            return border;
        }
    }
}
