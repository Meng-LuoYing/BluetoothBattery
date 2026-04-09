using Microsoft.Win32;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BluetoothBatteryUI
{
    public partial class IconPickerDialog : Window
    {
        private const string IconsFolder = "Images\\Icons";
        private const string BluetoothIconFile = "蓝牙.svg";
        private const string MouseIconFile = "鼠标.svg";
        private const string KeyboardIconFile = "键盘.svg";
        private const string HeadphonesIconFile = "耳机.svg";
        private const string GamepadIconFile = "游戏手柄.svg";
        private const string PhoneIconFile = "手机.svg";
        private const string SpeakerIconFile = "重低音扬声器.svg";

        public string SelectedIcon { get; private set; } = "Default";
        public bool IsCustomImage { get; private set; } = false;

        public IconPickerDialog(string currentIcon)
        {
            InitializeComponent();
            LoadDefaultIconImages();
            LoadPresetButtonImages();
            InitializeCurrentSelection(currentIcon);
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
                if (rb == DefaultIconRadio)
                {
                    SelectedIcon = "Default";
                    IsCustomImage = false;
                    UpdatePreviewDefaultImage();
                    return;
                }

                SelectedIcon = iconCode;
                IsCustomImage = false;
                UpdatePreviewPresetImage(iconCode);
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
                CustomTab.IsChecked = true;
                Tab_Changed(CustomTab, new RoutedEventArgs());
                UpdatePreviewImage(SelectedIcon);
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

        private void InitializeCurrentSelection(string currentIcon)
        {
            if (string.IsNullOrWhiteSpace(currentIcon) ||
                string.Equals(currentIcon, "Default", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentIcon, "默认", StringComparison.OrdinalIgnoreCase) ||
                currentIcon == "\uE972")
            {
                DefaultIconRadio.IsChecked = true;
                SelectedIcon = "Default";
                IsCustomImage = false;
                UpdatePreviewDefaultImage();
                return;
            }

            SelectedIcon = currentIcon;
            IsCustomImage = false;
            if (File.Exists(currentIcon))
            {
                IsCustomImage = true;
                CustomTab.IsChecked = true;
                Tab_Changed(CustomTab, new RoutedEventArgs());
                UpdatePreviewImage(currentIcon);
                return;
            }

            var presetKey = NormalizePresetKey(currentIcon);
            if (presetKey != null)
            {
                SelectedIcon = presetKey;
                IsCustomImage = false;
                UpdatePreviewPresetImage(presetKey);
                return;
            }

            var glyph = currentIcon.Length == 1 ? currentIcon : MapPresetNameToGlyph(currentIcon);
            UpdatePreviewGlyph(glyph);
        }

        private void UpdatePreviewGlyph(string glyph)
        {
            PreviewImage.Visibility = Visibility.Collapsed;
            PreviewImage.Source = null;
            PreviewDefaultImage.Visibility = Visibility.Collapsed;
            PreviewIcon.Visibility = Visibility.Visible;
            PreviewIcon.Text = glyph;
        }

        private void UpdatePreviewImage(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                PreviewIcon.Visibility = Visibility.Collapsed;
                PreviewDefaultImage.Visibility = Visibility.Collapsed;
                PreviewImage.Source = bitmap;
                PreviewImage.Visibility = Visibility.Visible;
            }
            catch
            {
                UpdatePreviewDefaultImage();
            }
        }

        private void UpdatePreviewDefaultImage()
        {
            PreviewImage.Visibility = Visibility.Collapsed;
            PreviewImage.Source = null;
            var defaultIconSource = TryLoadProjectSvgImageSource(BluetoothIconFile);
            if (defaultIconSource != null)
            {
                PreviewIcon.Visibility = Visibility.Collapsed;
                PreviewDefaultImage.Source = defaultIconSource;
                PreviewDefaultImage.Visibility = Visibility.Visible;
                return;
            }

            PreviewDefaultImage.Visibility = Visibility.Collapsed;
            PreviewIcon.Visibility = Visibility.Visible;
            PreviewIcon.Text = "\uE972";
        }

        private void UpdatePreviewPresetImage(string presetName)
        {
            var imageSource = TryCreatePresetImageSource(presetName);
            if (imageSource != null)
            {
                PreviewIcon.Visibility = Visibility.Collapsed;
                PreviewDefaultImage.Visibility = Visibility.Collapsed;
                PreviewImage.Source = imageSource;
                PreviewImage.Visibility = Visibility.Visible;
                return;
            }

            UpdatePreviewGlyph(MapPresetNameToGlyph(presetName));
        }

        private void LoadDefaultIconImages()
        {
            var source = TryLoadProjectSvgImageSource(BluetoothIconFile);
            if (source != null)
            {
                DefaultOptionImage.Source = source;
                PreviewDefaultImage.Source = source;
            }
        }

        private void LoadPresetButtonImages()
        {
            SetPresetImage(PresetMouseImage, "鼠标");
            SetPresetImage(PresetKeyboardImage, "键盘");
            SetPresetImage(PresetHeadphonesImage, "耳机");
            SetPresetImage(PresetGamepadImage, "手柄");
            SetPresetImage(PresetPhoneImage, "手机");
            SetPresetImage(PresetSpeakerImage, "音箱");
        }

        private void SetPresetImage(Image image, string presetName)
        {
            var imageSource = TryCreatePresetImageSource(presetName);
            if (imageSource != null)
            {
                image.Source = imageSource;
                return;
            }

            image.Source = null;
        }

        private ImageSource TryCreatePresetImageSource(string presetName)
        {
            var key = NormalizePresetKey(presetName);
            if (key == null)
            {
                return null;
            }

            return key switch
            {
                "鼠标" => TryLoadProjectSvgImageSource(MouseIconFile),
                "键盘" => TryLoadProjectSvgImageSource(KeyboardIconFile),
                "耳机" => TryLoadProjectSvgImageSource(HeadphonesIconFile),
                "手柄" => TryLoadProjectSvgImageSource(GamepadIconFile),
                "手机" => TryLoadProjectSvgImageSource(PhoneIconFile),
                "音箱" => TryLoadProjectSvgImageSource(SpeakerIconFile),
                _ => null
            };
        }

        private string NormalizePresetKey(string presetName)
        {
            return presetName switch
            {
                "Mouse" => "鼠标",
                "Keyboard" => "键盘",
                "Headphones" => "耳机",
                "Gamepad" => "手柄",
                "Phone" => "手机",
                "Speaker" => "音箱",
                "Default" => "默认",
                "默认" => "默认",
                "蓝牙" => "默认",
                "\uE962" => "鼠标",
                "\uE765" => "键盘",
                "\uE95B" => "耳机",
                "\uE7FC" => "手柄",
                "\uE8EA" => "手机",
                "\uE7F5" => "音箱",
                "\uE972" => "默认",
                "Bluetooth" => "默认",
                "鼠标" => "鼠标",
                "键盘" => "键盘",
                "耳机" => "耳机",
                "手柄" => "手柄",
                "手机" => "手机",
                "音箱" => "音箱",
                _ when presetName != null && presetName.IndexOf("蓝牙", StringComparison.OrdinalIgnoreCase) >= 0 => "默认",
                _ => null
            };
        }

        private ImageSource TryLoadProjectSvgImageSource(string fileName)
        {
            try
            {
                var fullPath = Path.Combine(AppContext.BaseDirectory, IconsFolder, fileName);
                if (!File.Exists(fullPath))
                {
                    return null;
                }

                var svgText = File.ReadAllText(fullPath);
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
                    var image = new DrawingImage(drawing);
                    image.Freeze();
                    return image;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private string MapPresetNameToGlyph(string presetName)
        {
            return presetName switch
            {
                "Keyboard" => "\uE765",
                "Mouse" => "\uE962",
                "Headphones" => "\uE95B",
                "Gamepad" => "\uE7FC",
                "Speaker" => "\uE7F5",
                "Phone" => "\uE8EA",
                "Battery" => "\uE850",
                "Generic" => "\uE9D9",
                "默认" => "\uE972",
                "鼠标" => "\uE962",
                "耳机" => "\uE95B",
                "手柄" => "\uE7FC",
                "音箱" => "\uE7F5",
                "手机" => "\uE8EA",
                "电池" => "\uE850",
                "通用" => "\uE9D9",
                _ => "\uE9D9"
            };
        }
    }
}
