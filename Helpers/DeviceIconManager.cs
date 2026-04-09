using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PathShape = System.Windows.Shapes.Path;

namespace BluetoothBatteryUI
{
    public static class DeviceIconManager
    {
        private const string LegacyDefaultIconSymbol = "\uE972";
        private const string IconsFolder = "Images\\Icons";
        private const string BluetoothIconFile = "蓝牙.svg";
        private const string MouseIconFile = "鼠标.svg";
        private const string KeyboardIconFile = "键盘.svg";
        private const string HeadphonesIconFile = "耳机.svg";
        private const string GamepadIconFile = "游戏手柄.svg";
        private const string PhoneIconFile = "手机.svg";
        private const string SpeakerIconFile = "重低音扬声器.svg";

        public static object GetIconForDevice(string deviceId, string deviceName, AppSettings settings, bool isDarkTheme = true)
        {
            // 1. 检查是否存在自定义图标设置
            if (settings.DeviceIcons.TryGetValue(deviceId, out string iconSetting))
            {
                if (string.IsNullOrWhiteSpace(iconSetting) ||
                    string.Equals(iconSetting, "Default", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(iconSetting, "默认", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(iconSetting, "蓝牙", StringComparison.OrdinalIgnoreCase))
                {
                    return CreateDefaultDeviceIcon(isDarkTheme);
                }

                // 如果是文件路径（包含冒号或斜杠）
                if (iconSetting.Contains(":") || iconSetting.Contains("\\") || iconSetting.Contains("/"))
                {
                    if (File.Exists(iconSetting))
                    {
                        try
                        {
                            var imageSource = TryLoadImageSourceFromFile(iconSetting);
                            if (imageSource == null)
                            {
                                return CreateDefaultDeviceIcon(isDarkTheme);
                            }
                            
                            var image = new Image
                            {
                                Source = imageSource,
                                Width = 36,
                                Height = 36,
                                Stretch = Stretch.UniformToFill,
                                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                            };
                            
                            // 圆形裁剪
                            var ellipseGeometry = new EllipseGeometry(new System.Windows.Point(18, 18), 18, 18);
                            image.Clip = ellipseGeometry;
                            
                            // 包装在 Border 中以保持一致性并支持状态颜色边框
                            var border = new Border
                            {
                                Width = 40,
                                Height = 40,
                                CornerRadius = new System.Windows.CornerRadius(20),
                                BorderThickness = new System.Windows.Thickness(1),
                                Margin = new System.Windows.Thickness(0, 0, 15, 0),
                                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                Child = image
                            };
                            ApplyTheme(border, isDarkTheme);

                            return border;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"加载自定义图标失败: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // 如果长度为1，说明直接存储了符号 (修复预设图标显示问题)
                    if (iconSetting.Length == 1)
                    {
                        if (iconSetting == LegacyDefaultIconSymbol)
                        {
                            return CreateDefaultDeviceIcon(isDarkTheme);
                        }

                        return CreatePresetIcon(iconSetting, isDarkTheme);
                    }

                    // 预设图标 (兼容旧数据: 名称对应 Unicode 字符)
                    string symbol = GetSymbolForPreset(iconSetting);
                    if (symbol == LegacyDefaultIconSymbol)
                    {
                        return CreateDefaultDeviceIcon(isDarkTheme);
                    }

                    return CreatePresetIcon(symbol, isDarkTheme);
                }
            }

            // 2. 如果没有自定义，根据名称自动判断
            string defaultSymbol = GetDefaultSymbolForDevice(deviceName);
            if (defaultSymbol == LegacyDefaultIconSymbol)
            {
                return CreateDefaultDeviceIcon(isDarkTheme);
            }

            return CreatePresetIcon(defaultSymbol, isDarkTheme);
        }

        private static string GetSymbolForPreset(string presetName)
        {
            switch (presetName)
            {
                case "Default":
                case "默认":
                case "蓝牙":
                    return "\uE972";
                case "鼠标": return "\uE962";
                case "键盘": return "\uE765";
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
            if (name.Contains("keyboard") || name.Contains("keypad") || name.Contains("键盘")) return "\uE765";
            if (name.Contains("headset") || name.Contains("headphone") || name.Contains("earbud") || name.Contains("airpods") || name.Contains("耳机")) return "\uE95B";
            if (name.Contains("gamepad") || name.Contains("controller") || name.Contains("xbox") || name.Contains("dualshock") || name.Contains("手柄")) return "\uE7FC";
            if (name.Contains("phone") || name.Contains("iphone") || name.Contains("android") || name.Contains("手机")) return "\uE8EA";
            if (name.Contains("pen") || name.Contains("pencil") || name.Contains("stylus") || name.Contains("笔")) return "\uEDC6";
            if (name.Contains("speaker") || name.Contains("sound") || name.Contains("音箱")) return "\uE7F5";

            return "\uE972"; // 默认蓝牙图标
        }

        public static void ApplyTheme(UIElement iconElement, bool isDarkTheme)
        {
            if (iconElement is not Border border)
            {
                return;
            }

            border.BorderBrush = isDarkTheme
                ? new SolidColorBrush(Color.FromArgb(120, 255, 255, 255))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
            border.Background = isDarkTheme
                ? new SolidColorBrush(Color.FromArgb(22, 255, 255, 255))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEF2F7"));

            if (border.Child is TextBlock textBlock)
            {
                textBlock.Foreground = isDarkTheme
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
            }
            else if (border.Child is Viewbox viewbox && viewbox.Child is PathShape path)
            {
                var iconBrush = isDarkTheme
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));

                if (path.Stroke != null)
                {
                    path.Stroke = iconBrush;
                }
                else
                {
                    path.Fill = iconBrush;
                }
            }
        }

        private static Border CreatePresetIcon(string symbol, bool isDarkTheme)
        {
            var iconFileName = GetPresetSvgFileBySymbol(symbol);
            if (!string.IsNullOrWhiteSpace(iconFileName))
            {
                var imageSource = TryLoadProjectSvgImageSource(iconFileName);
                if (imageSource == null)
                {
                    return CreateDefaultDeviceIcon(isDarkTheme);
                }

                var svgBorder = new Border
                {
                    Width = 40,
                    Height = 40,
                    CornerRadius = new CornerRadius(20),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 15, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new Image
                    {
                        Source = imageSource,
                        Width = 30,
                        Height = 30,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                ApplyTheme(svgBorder, isDarkTheme);
                return svgBorder;
            }

            // 创建带圆圈的图标容器
            var border = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new System.Windows.CornerRadius(20), // 圆形
                BorderThickness = new System.Windows.Thickness(1),
                Margin = new System.Windows.Thickness(0, 0, 15, 0),
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            var textBlock = new TextBlock
            {
                Text = symbol,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 20,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            border.Child = textBlock;
            ApplyTheme(border, isDarkTheme);
            return border;
        }

        private static string GetPresetSvgFileBySymbol(string symbol)
        {
            return symbol switch
            {
                "\uE962" => MouseIconFile,
                "\uE765" => KeyboardIconFile,
                "\uE95B" => HeadphonesIconFile,
                "\uE7FC" => GamepadIconFile,
                "\uE8EA" => PhoneIconFile,
                "\uE7F5" => SpeakerIconFile,
                _ => string.Empty
            };
        }

        private static ImageSource TryLoadProjectSvgImageSource(string fileName)
        {
            var fullPath = Path.Combine(AppContext.BaseDirectory, IconsFolder, fileName);
            return TryLoadImageSourceFromFile(fullPath);
        }

        private static ImageSource TryLoadImageSourceFromFile(string filePath)
        {
            try
            {
                if (string.Equals(Path.GetExtension(filePath), ".svg", StringComparison.OrdinalIgnoreCase))
                {
                    var svgText = File.ReadAllText(filePath);
                    var match = Regex.Match(svgText, "data:image/[^;]+;base64,([^\"']+)");
                    if (match.Success)
                    {
                        var bytes = Convert.FromBase64String(match.Groups[1].Value);
                        using var ms = new MemoryStream(bytes);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }

                    var pathMatch = Regex.Match(svgText, "<path[^>]*\\sd\\s*=\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (pathMatch.Success)
                    {
                        var drawing = new GeometryDrawing
                        {
                            Brush = Brushes.White,
                            Geometry = Geometry.Parse(pathMatch.Groups[1].Value)
                        };
                        var drawingImage = new DrawingImage(drawing);
                        drawingImage.Freeze();
                        return drawingImage;
                    }

                    return null;
                }

                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(filePath);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = 100;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        private static Border CreateDefaultDeviceIcon(bool isDarkTheme)
        {
            var imageSource = TryLoadProjectSvgImageSource(BluetoothIconFile);
            if (imageSource != null)
            {
                var imageBorder = new Border
                {
                    Width = 40,
                    Height = 40,
                    CornerRadius = new CornerRadius(20),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 15, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new Image
                    {
                        Source = imageSource,
                        Width = 30,
                        Height = 30,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                ApplyTheme(imageBorder, isDarkTheme);
                return imageBorder;
            }

            var border = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 15, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var iconPath = new PathShape
            {
                Data = Geometry.Parse("M33.4520187,31.2442265l15.1142006-13.6035004c0.2041016-0.1835995,0.3232994-0.4424,0.3311005-0.7168007 c0.006813-0.2743988-0.0986023-0.5391006-0.2929993-0.7334003L32.7069206,0.2930257 c-0.2862015-0.2861-0.714901-0.372-1.089901-0.2168c-0.3740005,0.1543-0.6172009,0.5196-0.6172009,0.9238999v27.7910004 L16.8094196,14.6016254c-0.3906002-0.3906002-1.0233994-0.3906002-1.4140997,0 c-0.3906002,0.3906002-0.3906002,1.0235004,0,1.4141006l15.1524,15.1513004L15.4334202,44.7706261 c-0.4101,0.3690987-0.4432878,1.0018997-0.0742006,1.4120979c0.3691006,0.4101028,1.000001,0.4433022,1.4121008,0.0742035 l14.2284985-12.8057022v29.5489006c0,0.4042015,0.2432003,0.7695007,0.6172009,0.9238014 c0.1240005,0.0516968,0.2539005,0.0761948,0.3827991,0.0761948c0.2598019,0,0.5156994-0.1015968,0.7071018-0.2929955 l15.8973999-15.8974991c0.1875-0.1875,0.2929993-0.4414024,0.2929993-0.7070007 c0-0.2656021-0.1054993-0.5195007-0.2929993-0.7070007L33.4520187,31.2442265z M32.9998207,3.4141257l13.4453125,13.4453011 L32.9998207,28.9600258V3.4141257z M32.9998207,60.5860252V33.620224l13.4833984,13.4824028L32.9998207,60.5860252z"),
                Stretch = Stretch.Uniform
            };

            border.Child = new Viewbox
            {
                Width = 26,
                Height = 26,
                Stretch = Stretch.Uniform,
                Child = iconPath
            };

            ApplyTheme(border, isDarkTheme);
            return border;
        }
    }
}
