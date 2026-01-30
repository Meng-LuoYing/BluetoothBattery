using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using BluetoothBatteryUI.Models;

namespace BluetoothBatteryUI
{
    public partial class DeviceDetailsWindow : Window
    {
        private string deviceId;
        private int currentBatteryLevel;
        private int selectedTimeRange = 24; // 默认24小时

        public DeviceDetailsWindow(string deviceId, string deviceName, int batteryLevel, string connectionType)
        {
            InitializeComponent();
            
            this.deviceId = deviceId;
            this.currentBatteryLevel = batteryLevel;
            
            // 设置设备信息
            DeviceNameText.Text = deviceName;
            DeviceStatusText.Text = "LINKED";
            BatteryPercentText.Text = $"{batteryLevel}%";
            ConnectionTypeText.Text = connectionType;
            
            // 绘制电量圆环
            DrawBatteryRing(batteryLevel);
            
            // 加载数据
            LoadDeviceData();
        }

        private void DrawBatteryRing(int percentage)
        {
            double radius = 42;
            double centerX = 50;
            double centerY = 50;
            double angle = (percentage / 100.0) * 360;
            
            // 转换为弧度
            double startAngle = -90; // 从顶部开始
            double endAngle = startAngle + angle;
            
            double startRad = startAngle * Math.PI / 180;
            double endRad = endAngle * Math.PI / 180;
            
            Point startPoint = new Point(
                centerX + radius * Math.Cos(startRad),
                centerY + radius * Math.Sin(startRad)
            );
            
            Point endPoint = new Point(
                centerX + radius * Math.Cos(endRad),
                centerY + radius * Math.Sin(endRad)
            );
            
            bool isLargeArc = angle > 180;
            
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure { StartPoint = startPoint };
            pathFigure.Segments.Add(new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = isLargeArc
            });
            pathGeometry.Figures.Add(pathFigure);
            
            BatteryArc.Data = pathGeometry;
        }

        private void LoadDeviceData()
        {
            var history = DeviceHistoryManager.GetDeviceHistory(deviceId);
            if (history == null)
            {
                ShowNoData();
                return;
            }

            // 计算使用时长
            var usageTime = DeviceHistoryManager.CalculateUsageTime(deviceId, TimeSpan.FromHours(selectedTimeRange));
            UsageTimeText.Text = $"{(int)usageTime.TotalHours}h {usageTime.Minutes}m";

            // 计算平均续航
            var avgDrain = DeviceHistoryManager.CalculateAverageBatteryDrain(deviceId, TimeSpan.FromHours(selectedTimeRange));
            if (avgDrain > 0)
            {
                AverageDrainText.Text = $"{avgDrain:F1}%/h";
            }
            else
            {
                AverageDrainText.Text = "--";
            }

            // 获取最后变化
            LastChangeText.Text = DeviceHistoryManager.GetLastBatteryChange(deviceId);

            // 绘制图表
            DrawChart();
        }

        private void DrawChart()
        {
            ChartCanvas.Children.Clear();
            NoDataText.Visibility = Visibility.Collapsed;

            var records = DeviceHistoryManager.GetRecordsInRange(deviceId, TimeSpan.FromHours(selectedTimeRange));
            
            if (records.Count < 2)
            {
                ShowNoData();
                return;
            }

            double width = ChartCanvas.ActualWidth > 0 ? ChartCanvas.ActualWidth : 600;
            double height = ChartCanvas.ActualHeight > 0 ? ChartCanvas.ActualHeight : 250;

            // 绘制网格线
            DrawGrid(width, height);

            // 绘制趋势线
            DrawTrendLine(records, width, height);

            // 绘制时间轴标签
            DrawTimeLabels(records, width, height);
        }

        private void DrawGrid(double width, double height)
        {
            // 水平网格线 (每20%)
            for (int i = 0; i <= 5; i++)
            {
                double y = height - (i * height / 5);
                
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    StrokeThickness = 1
                };
                ChartCanvas.Children.Add(line);

                // Y轴标签
                var label = new TextBlock
                {
                    Text = (i * 20).ToString(),
                    Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                    FontSize = 10
                };
                Canvas.SetLeft(label, 5);
                Canvas.SetTop(label, y - 15);
                ChartCanvas.Children.Add(label);
            }

            // 垂直网格线
            for (int i = 0; i <= 6; i++)
            {
                double x = i * width / 6;
                
                var line = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = height,
                    Stroke = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    StrokeThickness = 1
                };
                ChartCanvas.Children.Add(line);
            }
        }

        private void DrawTrendLine(List<BatteryRecord> records, double width, double height)
        {
            var points = new PointCollection();

            for (int i = 0; i < records.Count; i++)
            {
                double x = (i / (double)(records.Count - 1)) * width;
                double y = height - (records[i].BatteryLevel / 100.0) * height;
                points.Add(new Point(x, y));
            }

            var polyline = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 188, 212)),
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round
            };

            ChartCanvas.Children.Add(polyline);

            // 绘制数据点
            foreach (var point in points)
            {
                var ellipse = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(0, 188, 212))
                };
                Canvas.SetLeft(ellipse, point.X - 3);
                Canvas.SetTop(ellipse, point.Y - 3);
                ChartCanvas.Children.Add(ellipse);
            }
        }

        private void DrawTimeLabels(List<BatteryRecord> records, double width, double height)
        {
            // 显示6个时间标签
            for (int i = 0; i <= 6; i++)
            {
                int index = (int)((i / 6.0) * (records.Count - 1));
                if (index >= records.Count) index = records.Count - 1;

                var record = records[index];
                double x = (i / 6.0) * width;

                var label = new TextBlock
                {
                    Text = record.Timestamp.ToString("HH:mm"),
                    Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                    FontSize = 10
                };
                Canvas.SetLeft(label, x - 15);
                Canvas.SetTop(label, height + 5);
                ChartCanvas.Children.Add(label);
            }
        }

        private void ShowNoData()
        {
            NoDataText.Visibility = Visibility.Visible;
            UsageTimeText.Text = "0h 0m";
            AverageDrainText.Text = "--";
            LastChangeText.Text = "-";
        }

        private void TimeRange_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                selectedTimeRange = int.Parse(tag);

                // 更新按钮样式
                Range1HourButton.Style = (Style)FindResource("NavButton");
                Range6HourButton.Style = (Style)FindResource("NavButton");
                Range24HourButton.Style = (Style)FindResource("NavButton");

                button.Style = (Style)FindResource("ActiveNavButton");

                // 重新加载数据
                LoadDeviceData();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            // 窗口渲染完成后绘制图表
            DrawChart();
        }
    }
}
