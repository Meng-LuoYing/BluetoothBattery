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
            
            // 根据电量设置颜色：0-30红色，30-70黄色，70-100绿色
            if (percentage <= 30)
            {
                BatteryArc.Stroke = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // 红色
                BatteryPercentText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }
            else if (percentage <= 70)
            {
                BatteryArc.Stroke = new SolidColorBrush(Color.FromRgb(250, 204, 21)); // 黄色
                BatteryPercentText.Foreground = new SolidColorBrush(Color.FromRgb(250, 204, 21));
            }
            else
            {
                BatteryArc.Stroke = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // 绿色
                BatteryPercentText.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
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

            // 计算使用时长（固定24小时窗口）
            var usageTime = DeviceHistoryManager.CalculateUsageTime(deviceId, TimeSpan.FromHours(24));
            UsageTimeText.Text = $"{(int)usageTime.TotalHours}h {usageTime.Minutes}m";

            // 计算平均续航（估算剩余时间）
            // 1. 优先尝试使用24小时内的平均消耗率（更稳定）
            var drainRate = DeviceHistoryManager.CalculateAverageBatteryDrain(deviceId, TimeSpan.FromHours(24));
            
            // 2. 如果无效，尝试使用当前时间范围
            if (drainRate <= 0)
            {
                drainRate = DeviceHistoryManager.CalculateAverageBatteryDrain(deviceId, TimeSpan.FromHours(selectedTimeRange));
            }
            
            // 3. 如果还是无效，尝试简单的线性计算 (总掉电量 / 总时间)
            if (drainRate <= 0)
            {
                var rangeRecs = DeviceHistoryManager.GetRecordsInRange(deviceId, TimeSpan.FromHours(24));
                if (rangeRecs.Count >= 2)
                {
                    var first = rangeRecs.First();
                    var last = rangeRecs.Last();
                    var totalHours = (last.Timestamp - first.Timestamp).TotalHours;
                    var totalDrop = first.BatteryLevel - last.BatteryLevel;
                    
                    if (totalHours > 0.1 && totalDrop > 0)
                    {
                        drainRate = totalDrop / totalHours;
                    }
                }
            }

            if (drainRate > 0)
            {
                // 获取当前电量
                int batteryPercentage = 0;
                if (history.BatteryRecords.Count > 0)
                {
                    batteryPercentage = history.BatteryRecords.Last().BatteryLevel;
                }

                // 计算剩余时间 = 当前电量 / 消耗率
                if (batteryPercentage > 0)
                {
                    var hoursLeft = batteryPercentage / drainRate;
                    
                    if (hoursLeft > 99)
                    {
                        AverageDrainText.Text = ">99h";
                    }
                    else
                    {
                        int h = (int)hoursLeft;
                        int m = (int)((hoursLeft - h) * 60);
                        AverageDrainText.Text = h > 0 ? $"{h}h {m}m" : $"{m}m";
                    }
                }
                else
                {
                     AverageDrainText.Text = "--";
                }
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

            // 使用参考项目的 Thickness 边距系统
            var padding = chartPadding;
            var plotW = Math.Max(1, width - padding.Left - padding.Right);
            var plotH = Math.Max(1, height - padding.Top - padding.Bottom);

            // 计算时间轴范围 - 向上取整到下一个整点（用于显示标签）
            var currentTime = records.Last().Timestamp;
            var axisMaxT = RoundUpToInterval(currentTime, selectedTimeRange);
            var axisMinT = axisMaxT.AddHours(-selectedTimeRange);
            
            // 计算数据范围 - 向下取整到上一个整点（用于数据终点）
            var dataMaxT = RoundDownToInterval(currentTime, selectedTimeRange);
            
            // 过滤数据：只保留到dataMaxT的数据（不采样，保留所有原始数据点）
            var filteredRecords = records.Where(r => r.Timestamp >= axisMinT && r.Timestamp <= dataMaxT).ToList();
            
            // 保存过滤后的记录供Tooltip使用
            this.currentRecords = filteredRecords;
            
            // 保存时间轴范围供Tooltip使用
            this.currentAxisMinT = axisMinT;
            this.currentAxisMaxT = axisMaxT;
            
            var axisTotalSeconds = Math.Max(1, (axisMaxT - axisMinT).TotalSeconds);

            // 绘制网格和坐标轴
            DrawGrid(width, height, padding, plotW, plotH, axisMinT, axisMaxT);

            // 绘制趋势线（使用过滤后的所有数据点）
            DrawTrendLine(filteredRecords, padding, plotW, plotH, axisMinT, axisTotalSeconds);

            // 绘制时间轴标签
            DrawTimeLabels(padding, plotW, plotH, axisMinT, axisMaxT);
        }

        // 向上取整到下一个间隔点
        private static DateTime RoundUpToInterval(DateTime currentTime, int timeRangeHours)
        {
            if (timeRangeHours <= 1)
            {
                // 1小时：向上取整到下一个10分钟
                var minute = currentTime.Minute;
                var roundedMinute = ((minute / 10) + 1) * 10;
                if (roundedMinute >= 60)
                {
                    return new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 0, 0).AddHours(1);
                }
                return new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, roundedMinute, 0);
            }
            else if (timeRangeHours <= 6)
            {
                // 6小时：向上取整到下一个30分钟
                var minute = currentTime.Minute;
                if (minute == 0)
                {
                    return currentTime; // 已经是整点
                }
                else if (minute <= 30)
                {
                    return new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 30, 0);
                }
                else
                {
                    return new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 0, 0).AddHours(1);
                }
            }
            else
            {
                // 24小时：向上取整到下一个整点
                if (currentTime.Minute == 0 && currentTime.Second == 0)
                {
                    return currentTime; // 已经是整点
                }
                return new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 0, 0).AddHours(1);
            }
        }

        // 向下取整到最近的间隔点（用于数据采样终点）
        private static DateTime RoundDownToInterval(DateTime currentTime, int timeRangeHours)
        {
            if (timeRangeHours <= 1)
            {
                // 1小时：向下取整到10分钟
                var minute = (currentTime.Minute / 10) * 10;
                return new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, minute, 0);
            }
            else if (timeRangeHours <= 6)
            {
                // 6小时：向下取整到30分钟
                var minute = currentTime.Minute < 30 ? 0 : 30;
                return new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, minute, 0);
            }
            else
            {
                // 24小时：向下取整到整点
                return new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 0, 0);
            }
        }

        // 均匀采样数据点
        private List<BatteryRecord> SampleRecordsEvenly(List<BatteryRecord> records, DateTime minT, DateTime maxT, int sampleCount)
        {
            if (records.Count == 0) return new List<BatteryRecord>();
            if (records.Count <= sampleCount) return records;

            var sampledRecords = new List<BatteryRecord>();
            var totalSeconds = (maxT - minT).TotalSeconds;
            var interval = totalSeconds / (sampleCount - 1);

            for (int i = 0; i < sampleCount; i++)
            {
                var targetTime = minT.AddSeconds(interval * i);
                
                // 找到最接近目标时间的记录
                var closestRecord = records
                    .Where(r => r.Timestamp >= minT && r.Timestamp <= maxT)
                    .OrderBy(r => Math.Abs((r.Timestamp - targetTime).TotalSeconds))
                    .FirstOrDefault();

                if (closestRecord != null && !sampledRecords.Contains(closestRecord))
                {
                    sampledRecords.Add(closestRecord);
                }
            }

            return sampledRecords.OrderBy(r => r.Timestamp).ToList();
        }

        // 图表边距设置 (优化后的 Thickness 系统) - 底部间距缩小为5，标签绘制在Canvas外部
        private readonly Thickness chartPadding = new Thickness(56, 18, 18, 5);
        private List<BatteryRecord> currentRecords;
        private DateTime currentAxisMinT; // 当前时间轴最小值
        private DateTime currentAxisMaxT; // 当前时间轴最大值

        private void DrawGrid(double width, double height, Thickness padding, double plotW, double plotH, DateTime minT, DateTime maxT)
        {
            var gridBrush = new SolidColorBrush(Color.FromArgb(60, 148, 163, 184));

            // 绘制水平网格线 (Y轴: 0-100)
            for (int i = 0; i <= 10; i++)
            {
                double y = padding.Top + i / 10.0 * plotH;
                
                var line = new Line
                {
                    X1 = padding.Left,
                    X2 = padding.Left + plotW,
                    Y1 = y,
                    Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                ChartCanvas.Children.Add(line);

                // Y轴标签
                var label = new TextBlock
                {
                    Text = (100 - i * 10).ToString(),
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    FontSize = 11
                };
                Canvas.SetLeft(label, padding.Left - 45); // 靠近图表左边缘
                Canvas.SetTop(label, y - 8);
                ChartCanvas.Children.Add(label);
            }

            // 绘制边框
            var border = new Rectangle
            {
                Width = plotW,
                Height = plotH,
                Stroke = new SolidColorBrush(Color.FromArgb(80, 34, 211, 238)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(border, padding.Left);
            Canvas.SetTop(border, padding.Top);
            ChartCanvas.Children.Add(border);
        }

        private void DrawTrendLine(List<BatteryRecord> records, Thickness padding, double plotW, double plotH, DateTime axisMinT, double axisTotalSeconds)
        {
            var points = new PointCollection();

            foreach (var record in records)
            {
                // 计算X坐标
                var xRatio = (record.Timestamp - axisMinT).TotalSeconds / axisTotalSeconds;
                if (xRatio < 0) xRatio = 0;
                if (xRatio > 1) xRatio = 1;
                var x = padding.Left + xRatio * plotW;

                // 计算Y坐标 (0-100范围)
                var percent = Math.Clamp(record.BatteryLevel, 0, 100);
                var y = padding.Top + (1 - percent / 100.0) * plotH;

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

            // 绘制最后一个数据点
            if (points.Count > 0)
            {
                var last = points[points.Count - 1];
                var dot = new Ellipse 
                { 
                    Width = 8, 
                    Height = 8, 
                    Fill = polyline.Stroke 
                };
                Canvas.SetLeft(dot, last.X - 4);
                Canvas.SetTop(dot, last.Y - 4);
                ChartCanvas.Children.Add(dot);
            }
        }

        private void DrawTimeLabels(Thickness padding, double plotW, double plotH, DateTime minT, DateTime maxT)
        {
            var gridBrush = new SolidColorBrush(Color.FromArgb(60, 148, 163, 184));

            // 根据时间范围确定固定间隔和标签数量
            TimeSpan interval;
            int labelCount;
            
            if (selectedTimeRange <= 1)
            {
                interval = TimeSpan.FromMinutes(10); // 1小时：每10分钟
                labelCount = 6; // 6个标签
            }
            else if (selectedTimeRange <= 6)
            {
                interval = TimeSpan.FromMinutes(30); // 6小时：每30分钟
                labelCount = 12; // 12个标签
            }
            else
            {
                interval = TimeSpan.FromHours(2); // 24小时：每2小时
                labelCount = 12; // 12个标签
            }

            var totalSeconds = (maxT - minT).TotalSeconds;

            // 从最小时间开始，按固定间隔生成标签
            for (int i = 0; i < labelCount; i++)
            {
                var t = minT.Add(TimeSpan.FromTicks(interval.Ticks * i));
                
                // 确保时间在范围内
                if (t > maxT) break;
                
                var x = padding.Left + (t - minT).TotalSeconds / totalSeconds * plotW;

                // 垂直网格线
                var line = new Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = padding.Top,
                    Y2 = padding.Top + plotH,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                ChartCanvas.Children.Add(line);

                // 时间标签
                var label = new TextBlock
                {
                    Text = t.ToString("HH:mm"),
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    FontSize = 11
                };
                
                // 居中对齐标签
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var left = x - label.DesiredSize.Width / 2;
                
                // 防止标签超出边界
                if (left < padding.Left) left = padding.Left;
                if (left + label.DesiredSize.Width > padding.Left + plotW) 
                    left = padding.Left + plotW - label.DesiredSize.Width;
                
                Canvas.SetLeft(label, left);
                Canvas.SetTop(label, padding.Top + plotH + 8); // 标签向下移动，利用Border的Padding区域
                ChartCanvas.Children.Add(label);
            }
        }

        private static DateTime AlignToStep(DateTime dt, TimeSpan step)
        {
            if (step >= TimeSpan.FromHours(1))
            {
                var hourStep = (int)Math.Round(step.TotalHours);
                var alignedHour = (dt.Hour / hourStep) * hourStep;
                var baseTime = new DateTime(dt.Year, dt.Month, dt.Day, alignedHour, 0, 0);
                if (baseTime < dt) baseTime = baseTime.AddHours(hourStep);
                return baseTime;
            }

            var minuteStep = (int)Math.Round(step.TotalMinutes);
            var alignedMinute = (dt.Minute / minuteStep) * minuteStep;
            var baseMin = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, alignedMinute, 0);
            if (baseMin < dt) baseMin = baseMin.AddMinutes(minuteStep);
            return baseMin;
        }

        private void ChartCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentRecords == null || currentRecords.Count == 0) return;

            Point mousePos = e.GetPosition(ChartCanvas);
            double width = ChartCanvas.ActualWidth;
            double height = ChartCanvas.ActualHeight;

            var padding = chartPadding;
            double plotW = Math.Max(1, width - padding.Left - padding.Right);
            double plotH = Math.Max(1, height - padding.Top - padding.Bottom);

            // 如果鼠标在图表区域外，隐藏提示
            if (mousePos.X < padding.Left || mousePos.X > padding.Left + plotW || 
                mousePos.Y < padding.Top || mousePos.Y > padding.Top + plotH)
            {
                TooltipCanvas.Visibility = Visibility.Collapsed;
                return;
            }

            // 计算鼠标位置对应的时间比例
            double xRatio = (mousePos.X - padding.Left) / plotW;
            
            // 使用保存的时间轴范围（而不是从记录计算）
            DateTime viewStartTime = currentAxisMinT;
            DateTime viewEndTime = currentAxisMaxT;
            TimeSpan viewDuration = viewEndTime - viewStartTime;

            DateTime hoveredTime = viewStartTime.AddSeconds(xRatio * viewDuration.TotalSeconds);

            // 找到最近的记录
            var closestRecord = currentRecords.OrderBy(r => Math.Abs((r.Timestamp - hoveredTime).TotalSeconds)).First();

            // 计算该记录在图表上的位置
            double recordTimeRatio = (closestRecord.Timestamp - viewStartTime).TotalSeconds / viewDuration.TotalSeconds;
            double recordX = padding.Left + recordTimeRatio * plotW;
            double recordY = padding.Top + (1 - closestRecord.BatteryLevel / 100.0) * plotH;

            // 更新提示UI
            TooltipCanvas.Visibility = Visibility.Visible;
            
            // 更新指示线位置
            TooltipLine.Y1 = padding.Top;
            TooltipLine.Y2 = padding.Top + plotH;
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
