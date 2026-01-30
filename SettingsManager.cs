using System;
using System.IO;
using System.Text.Json;

namespace BluetoothBatteryUI
{
    public class SettingsManager
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BluetoothBatteryMonitor"
        );
        
        private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.json");
        
        public static AppSettings LoadSettings()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }
                
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"加载设置失败: {ex.Message}");
            }
            
            return new AppSettings();
        }
        
        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }
                
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFilePath, json);
                
                Logger.Log("设置已保存");
            }
            catch (Exception ex)
            {
                Logger.Log($"保存设置失败: {ex.Message}");
            }
        }
        
        public static string GetAppDataFolder()
        {
            return AppDataFolder;
        }
    }
}
