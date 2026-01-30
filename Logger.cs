using System;
using System.IO;

namespace BluetoothBatteryUI
{
    public static class Logger
    {
        private static readonly string LogFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BluetoothBatteryMonitor",
            "logs"
        );
        
        private static bool _detailedLogging = false;
        
        public static void SetDetailedLogging(bool enabled)
        {
            _detailedLogging = enabled;
        }
        
        public static void Log(string message, bool isDetailed = false)
        {
            if (isDetailed && !_detailedLogging)
            {
                return;
            }
            
            try
            {
                if (!Directory.Exists(LogFolder))
                {
                    Directory.CreateDirectory(LogFolder);
                }
                
                string logFile = Path.Combine(LogFolder, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
                string logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
                
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }
            catch
            {
                // 忽略日志错误
            }
        }
        
        public static string GetLogFolder()
        {
            return LogFolder;
        }
    }
}
