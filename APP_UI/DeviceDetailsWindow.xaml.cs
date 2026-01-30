using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Linq;
using BluetoothBatteryUI.Models;

namespace BluetoothBatteryUI
{
    public partial class DeviceDetailsWindow : Window
    {
        private string deviceId;
        private int currentBatteryLevel;
        private int selectedTimeRange = 6; // 默认6小时
        private bool isSidebarExpanded = false;

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
            
            // 监听窗口大小变化
            this.SizeChanged += DeviceDetailsWindow_SizeChanged;
        }
        
        private void DeviceDetailsWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 窗口大小变化时重绘图表
            if (ChartCanvas.ActualWidth > 0 && ChartCanvas.ActualHeight > 0)
            {
                DrawChart();
            }
        }

        private void DrawBatteryRing(int percentage)
        {
            double radius = 31; // (74 - 6*2) / 2
            double centerX = 37; // 74 / 2
            double centerY = 37;
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
            
            // 根据电量设置颜色
            if (percentage <= 20)
            {
                BatteryArc.Stroke = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // 红色
                BatteryPercentText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }
            else
            {
                BatteryArc.Stroke = new SolidColorBrush(Color.FromRgb(34, 211, 238)); // 青色
                BatteryPercentText.Foreground = new SolidColorBrush(Color.FromRgb(34, 211, 238));
            }
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
            this.currentRecords = records; // 保存记录供Tooltip使用
            
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

            // 更新时间标签
            DrawTimeLabels(records, width, height);
        }

        // 图表内边距设置
        private const double PaddingLeft = 35;
        private const double PaddingRight = 25;
        private const double PaddingTop = 15;
        private const double PaddingBottom = 15;

        private void DrawGrid(double width, double height)
        {
            double availableWidth = width - PaddingLeft - PaddingRight;
            double availableHeight = height - PaddingTop - PaddingBottom;
            
            // 水平网格线 (0-100, 每10一个刻度)
            for (int i = 0; i <= 10; i++)
            {
                double val = i * 10;
                // 计算Y坐标
                double y = PaddingTop + availableHeight - (val / 100.0 * availableHeight);
                
                var line = new Line
                {
                    X1 = PaddingLeft,
                    Y1 = y,
                    X2 = width - PaddingRight,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                    StrokeThickness = 1
                };
                ChartCanvas.Children.Add(line);

                // Y轴标签
                var label = new TextBlock
                {
                    Text = val.ToString(),
                    Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                    FontSize = 10,
                    TextAlignment = TextAlignment.Right,
                    Width = 30 // 固定宽度以便右对齐
                };
                
                // 标签位置：放在左侧内边距区域
                Canvas.SetLeft(label, PaddingLeft - 35); 
                Canvas.SetTop(label, y - 7); 
                ChartCanvas.Children.Add(label);
            }
        }

        private void DrawTrendLine(List<BatteryRecord> records, double width, double height)
        {
            var points = new PointCollection();

            if (records.Count == 0) return;

            DateTime viewEndTime = records.Last().Timestamp;
            DateTime viewStartTime = viewEndTime.AddHours(-selectedTimeRange);
            TimeSpan viewDuration = viewEndTime - viewStartTime;

            double availableWidth = width - PaddingLeft - PaddingRight;
            double availableHeight = height - PaddingTop - PaddingBottom;

            foreach (var record in records)
            {
                // 计算X坐标
                double timeRatio = (record.Timestamp - viewStartTime).TotalSeconds / viewDuration.TotalSeconds;
                double x = PaddingLeft + (timeRatio * availableWidth);
                
                // 计算Y坐标
                double y = PaddingTop + availableHeight - (record.BatteryLevel / 100.0 * availableHeight);
                
                points.Add(new Point(x, y));
            }

            // 绘制折线
            var polyline = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(Color.FromRgb(34, 211, 238)),
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round
            };

            ChartCanvas.Children.Add(polyline);

            // 绘制数据点
            foreach (var point in points)
            {
                // 只在绘制区域内绘制点
                if (point.X >= PaddingLeft - 2 && point.X <= width - PaddingRight + 2)
                {
                    var ellipse = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = new SolidColorBrush(Color.FromRgb(34, 211, 238))
                    };
                    Canvas.SetLeft(ellipse, point.X - 3);
                    Canvas.SetTop(ellipse, point.Y - 3);
                    ChartCanvas.Children.Add(ellipse);
                }
            }
        }

        // 鼠标交互变量
        private List<BatteryRecord> currentRecords;

        private void ChartCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentRecords == null || currentRecords.Count == 0) return;

            Point mousePos = e.GetPosition(ChartCanvas);
            double width = ChartCanvas.ActualWidth;
            double height = ChartCanvas.ActualHeight;

            double availableWidth = width - PaddingLeft - PaddingRight;
            double availableHeight = height - PaddingTop - PaddingBottom;

            // 如果鼠标在图表区域外，隐藏提示
            if (mousePos.X < PaddingLeft || mousePos.X > width - PaddingRight || 
                mousePos.Y < PaddingTop || mousePos.Y > height - PaddingBottom)
            {
                TooltipCanvas.Visibility = Visibility.Collapsed;
                return;
            }

            // 计算鼠标位置对应的时间
            double xRatio = (mousePos.X - PaddingLeft) / availableWidth;
            
            DateTime viewEndTime = currentRecords.Last().Timestamp;
            DateTime viewStartTime = viewEndTime.AddHours(-selectedTimeRange);
            TimeSpan viewDuration = viewEndTime - viewStartTime;

            DateTime hoveredTime = viewStartTime.AddSeconds(xRatio * viewDuration.TotalSeconds);

            // 找到最近的记录
            var closestRecord = currentRecords.OrderBy(r => Math.Abs((r.Timestamp - hoveredTime).TotalSeconds)).First();

            // 如果最近记录距离太远（例如超过这里显示范围的5%），也可以不显示
            // 这里简单处理：直接显示最近的

            // 计算该记录在图表上的位置
            double recordTimeRatio = (closestRecord.Timestamp - viewStartTime).TotalSeconds / viewDuration.TotalSeconds;
            double recordX = PaddingLeft + (recordTimeRatio * availableWidth);
            double recordY = PaddingTop + availableHeight - (closestRecord.BatteryLevel / 100.0 * availableHeight);

            // 更新提示UI
            TooltipCanvas.Visibility = Visibility.Visible;
            
            // 更新指示线位置
            TooltipLine.Y1 = PaddingTop;
            TooltipLine.Y2 = height - PaddingBottom;
            TooltipLine.X1 = recordX;
            TooltipLine.X2 = recordX;

            // 更新圆点位置
            Canvas.SetLeft(TooltipDot, recordX - 5);
            Canvas.SetTop(TooltipDot, recordY - 5);

            // 更新文本
            TooltipTime.Text = closestRecord.Timestamp.ToString("HH:mm");
            TooltipValue.Text = $"电量 {closestRecord.BatteryLevel}";

            // 更新提示框位置 (避免超出右边界)
            double tipX = recordX + 10;
            double tipY = recordY - 50;

            if (tipX + TooltipBorder.ActualWidth > width)
            {
                tipX = recordX - TooltipBorder.ActualWidth - 10;
            }
            if (tipY < 0) tipY = 0;

            Canvas.SetLeft(TooltipBorder, tipX);
            Canvas.SetTop(TooltipBorder, tipY);
        }

        private void ChartCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            TooltipCanvas.Visibility = Visibility.Collapsed;
        }

        private void DrawTimeLabels(List<BatteryRecord> records, double width, double height)
        {
            TimeLabelsPanel.Children.Clear();
            
            if (records.Count == 0) return;

            DateTime viewEndTime = records.Last().Timestamp;
            DateTime viewStartTime = viewEndTime.AddHours(-selectedTimeRange);
            TimeSpan viewDuration = viewEndTime - viewStartTime;

            // 确定时间间隔
            TimeSpan interval;
            switch (selectedTimeRange)
            {
                case 1: interval = TimeSpan.FromMinutes(10); break;
                case 6: interval = TimeSpan.FromMinutes(30); break;
                case 24: 
                default: interval = TimeSpan.FromHours(2); break;
            }

            // 有效绘图区域宽度
            double availableWidth = width - PaddingLeft - PaddingRight;

            // 找到第一个刻度时间 (向上取整)
            long ticks = interval.Ticks;
            long startTicks = viewStartTime.Ticks;
            long remainder = startTicks % ticks;
            DateTime firstTickTime = new DateTime(startTicks + (remainder == 0 ? 0 : ticks - remainder));

            // 循环生成标签
            for (DateTime t = firstTickTime; t <= viewEndTime; t = t.Add(interval))
            {
                // 计算位置比例
                double ratio = (t - viewStartTime).TotalSeconds / viewDuration.TotalSeconds;
                
                // 如果超出范围则跳过
                if (ratio < 0 || ratio > 1) continue;

                // 计算实际X坐标 (加上左边距)
                double x = PaddingLeft + (ratio * availableWidth);

                // 创建标签
                var label = new TextBlock
                {
                    Text = t.ToString("HH:mm"),
                    Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                    FontSize = 10
                };

                // 测量文本宽度以居中
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double labelWidth = label.DesiredSize.Width;

                Canvas.SetLeft(label, x - labelWidth / 2);
                Canvas.SetTop(label, 5);
                TimeLabelsPanel.Children.Add(label);

                // 这个时间点对应的垂直网格线 (如果你想只在有效区域绘制)
                 var line = new Line
                {
                    X1 = x,
                    Y1 = PaddingTop,
                    X2 = x,
                    Y2 = height - PaddingBottom, // 这里的height是Canvas的高度，可能和 DrawGrid 的 height 一致
                    Stroke = new SolidColorBrush(Color.FromRgb(31, 41, 55)), 
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 2 } 
                };
                ChartCanvas.Children.Add(line);
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

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            isSidebarExpanded = !isSidebarExpanded;
            
            if (isSidebarExpanded)
            {
                // 展开侧边栏
                SidebarColumn.Width = new GridLength(220);
                SidebarContentColumn.Width = new GridLength(1, GridUnitType.Star);
                SidebarContent.Visibility = Visibility.Visible;
                ToggleIcon.Text = "\uE76B"; // 向左箭头
            }
            else
            {
                // 收起侧边栏
                SidebarColumn.Width = new GridLength(72);
                SidebarContentColumn.Width = new GridLength(0);
                SidebarContent.Visibility = Visibility.Collapsed;
                ToggleIcon.Text = "\uE76C"; // 向右箭头
            }
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            // 窗口渲染完成后绘制图表
            DrawChart();
        }
    }
}
