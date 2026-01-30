using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;

namespace BluetoothBatteryUI
{
    public static class StartupManager
    {
        private const string AppName = "BluetoothBatteryMonitor";
        private const string RegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        
        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKey, false))
                {
                    return key?.GetValue(AppName) != null;
                }
            }
            catch
            {
                return false;
            }
        }
        
        public static void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                            key.SetValue(AppName, $"\"{exePath}\"");
                            Logger.Log("已启用开机自启动");
                        }
                        else
                        {
                            key.DeleteValue(AppName, false);
                            Logger.Log("已禁用开机自启动");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置开机自启动失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Log($"设置开机自启动失败: {ex.Message}");
            }
        }
    }
}
