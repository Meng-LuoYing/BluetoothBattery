using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BluetoothBatteryUI.Models
{
    /// <summary>
    /// 设备历史数据管理器
    /// </summary>
    public static class DeviceHistoryManager
    {
        private static readonly string HistoryFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BluetoothBatteryMonitor",
            "device_history.json"
        );

        private static Dictionary<string, DeviceHistory> deviceHistories = new Dictionary<string, DeviceHistory>();
        private static readonly int MaxRecordsPerDevice = 500; // 限制每个设备的最大记录数

        static DeviceHistoryManager()
        {
            LoadHistory();
        }

        /// <summary>
        /// 加载历史数据
        /// </summary>
        public static void LoadHistory()
        {
            try
            {
                if (File.Exists(HistoryFilePath))
                {
                    string json = File.ReadAllText(HistoryFilePath);
                    deviceHistories = JsonSerializer.Deserialize<Dictionary<string, DeviceHistory>>(json) 
                        ?? new Dictionary<string, DeviceHistory>();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"加载历史数据失败: {ex.Message}");
                deviceHistories = new Dictionary<string, DeviceHistory>();
            }
        }

        /// <summary>
        /// 保存历史数据
        /// </summary>
        public static void SaveHistory()
        {
            try
            {
                string directory = Path.GetDirectoryName(HistoryFilePath)!;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(deviceHistories, options);
                File.WriteAllText(HistoryFilePath, json);
            }
            catch (Exception ex)
            {
                Logger.Log($"保存历史数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录电池数据
        /// </summary>
        public static void RecordBatteryLevel(string deviceId, string deviceName, int batteryLevel, bool isConnected, string connectionType)
        {
            if (!deviceHistories.ContainsKey(deviceId))
            {
                deviceHistories[deviceId] = new DeviceHistory
                {
                    DeviceId = deviceId,
                    DeviceName = deviceName,
                    FirstSeen = DateTime.Now,
                    ConnectionType = connectionType
                };
            }

            var history = deviceHistories[deviceId];
            history.DeviceName = deviceName; // 更新名称
            history.LastSeen = DateTime.Now;
            history.ConnectionType = connectionType;

            // 添加新记录
            history.BatteryRecords.Add(new BatteryRecord
            {
                Timestamp = DateTime.Now,
                BatteryLevel = batteryLevel,
                IsConnected = isConnected
            });

            // 限制记录数量,删除最旧的记录
            if (history.BatteryRecords.Count > MaxRecordsPerDevice)
            {
                history.BatteryRecords.RemoveRange(0, history.BatteryRecords.Count - MaxRecordsPerDevice);
            }
        }

        /// <summary>
        /// 获取设备历史
        /// </summary>
        public static DeviceHistory? GetDeviceHistory(string deviceId)
        {
            return deviceHistories.ContainsKey(deviceId) ? deviceHistories[deviceId] : null;
        }

        /// <summary>
        /// 获取指定时间范围内的记录
        /// </summary>
        public static List<BatteryRecord> GetRecordsInRange(string deviceId, TimeSpan range)
        {
            if (!deviceHistories.ContainsKey(deviceId))
                return new List<BatteryRecord>();

            var cutoff = DateTime.Now - range;
            return deviceHistories[deviceId].BatteryRecords
                .Where(r => r.Timestamp >= cutoff)
                .OrderBy(r => r.Timestamp)
                .ToList();
        }

        /// <summary>
        /// 计算使用时长(连接状态的总时长)
        /// </summary>
        public static TimeSpan CalculateUsageTime(string deviceId, TimeSpan range)
        {
            var records = GetRecordsInRange(deviceId, range);
            if (records.Count < 2)
                return TimeSpan.Zero;

            TimeSpan totalUsage = TimeSpan.Zero;
            for (int i = 1; i < records.Count; i++)
            {
                if (records[i].IsConnected && records[i - 1].IsConnected)
                {
                    var duration = records[i].Timestamp - records[i - 1].Timestamp;
                    // 只统计合理的时间间隔(小于1小时)
                    if (duration.TotalHours < 1)
                    {
                        totalUsage += duration;
                    }
                }
            }

            return totalUsage;
        }

        /// <summary>
        /// 计算平均续航(每小时平均电量消耗)
        /// </summary>
        public static double CalculateAverageBatteryDrain(string deviceId, TimeSpan range)
        {
            var records = GetRecordsInRange(deviceId, range);
            if (records.Count < 2)
                return 0;

            // 只考虑连接状态的记录
            var connectedRecords = records.Where(r => r.IsConnected).ToList();
            if (connectedRecords.Count < 2)
                return 0;

            int totalDrain = 0;
            int drainCount = 0;

            for (int i = 1; i < connectedRecords.Count; i++)
            {
                var timeDiff = connectedRecords[i].Timestamp - connectedRecords[i - 1].Timestamp;
                var batteryDiff = connectedRecords[i - 1].BatteryLevel - connectedRecords[i].BatteryLevel;

                // 只统计合理的数据(时间间隔小于1小时,电量下降)
                if (timeDiff.TotalHours < 1 && timeDiff.TotalMinutes > 1 && batteryDiff > 0)
                {
                    // 计算每小时的电量消耗
                    double drainPerHour = batteryDiff / timeDiff.TotalHours;
                    totalDrain += (int)drainPerHour;
                    drainCount++;
                }
            }

            return drainCount > 0 ? (double)totalDrain / drainCount : 0;
        }

        /// <summary>
        /// 获取最后一次电量变化
        /// </summary>
        public static string GetLastBatteryChange(string deviceId)
        {
            if (!deviceHistories.ContainsKey(deviceId))
                return "-";

            var records = deviceHistories[deviceId].BatteryRecords;
            if (records.Count < 2)
                return "-";

            var latest = records[records.Count - 1];
            var previous = records[records.Count - 2];

            int change = latest.BatteryLevel - previous.BatteryLevel;
            if (change == 0)
                return "无变化";

            return change > 0 ? $"+{change}%" : $"{change}%";
        }

        /// <summary>
        /// 清理旧数据(保留最近30天)
        /// </summary>
        public static void CleanupOldData()
        {
            var cutoff = DateTime.Now.AddDays(-30);

            foreach (var history in deviceHistories.Values)
            {
                history.BatteryRecords.RemoveAll(r => r.Timestamp < cutoff);
            }

            // 移除没有记录的设备
            var emptyDevices = deviceHistories.Where(kv => kv.Value.BatteryRecords.Count == 0)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var deviceId in emptyDevices)
            {
                deviceHistories.Remove(deviceId);
            }

            SaveHistory();
        }
    }
}
