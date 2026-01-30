using System;
using System.Collections.Generic;

namespace BluetoothBatteryUI.Models
{
    /// <summary>
    /// 电池记录
    /// </summary>
    public class BatteryRecord
    {
        public DateTime Timestamp { get; set; }
        public int BatteryLevel { get; set; }
        public bool IsConnected { get; set; }
    }

    /// <summary>
    /// 设备历史数据
    /// </summary>
    public class DeviceHistory
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public List<BatteryRecord> BatteryRecords { get; set; } = new List<BatteryRecord>();
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public string ConnectionType { get; set; } = "未知";
    }
}
