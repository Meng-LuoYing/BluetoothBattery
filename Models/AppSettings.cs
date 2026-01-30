using System.Collections.Generic;

namespace BluetoothBatteryUI
{
    public class AppSettings
    {
        // 常规设置
        public int LowBatteryThreshold { get; set; } = 30;
        
        // 低电量提醒
        public bool EnableLowBatteryAlert { get; set; } = true;
        public bool UseToastNotification { get; set; } = true;
        
        // 系统设置
        public bool StartWithWindows { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public bool DetailedLogging { get; set; } = false;
        
        // 隐藏设备列表
        public List<string> HiddenDeviceIds { get; set; } = new List<string>();
        
        // 已提醒的设备（避免重复提醒）
        public HashSet<string> AlertedDevices { get; set; } = new HashSet<string>();
        
        // 自动刷新设置
        public bool EnableAutoRefresh { get; set; } = true;
        public int AutoRefreshIntervalMinutes { get; set; } = 5;

        // 自定义设置
        public Dictionary<string, string> DeviceAliases { get; set; } = new Dictionary<string, string>(); // 设备别名
        public Dictionary<string, string> DeviceIcons { get; set; } = new Dictionary<string, string>();   // 设备图标路径或预设名
        public HashSet<string> TrayIconDevices { get; set; } = new HashSet<string>();                     // 显示在托盘的设备
    }
}
