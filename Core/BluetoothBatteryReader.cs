using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace BluetoothBatteryReader
{
    public class BatteryReader
    {
        // Battery Service UUID (标准蓝牙电池服务)
        private static readonly Guid BatteryServiceUuid = new Guid("0000180F-0000-1000-8000-00805F9B34FB");
        
        // Battery Level Characteristic UUID (电池电量特征)
        private static readonly Guid BatteryLevelCharacteristicUuid = new Guid("00002A19-0000-1000-8000-00805F9B34FB");

        /// <summary>
        /// 扫描并列出所有蓝牙LE设备
        /// </summary>
        public static async Task<List<DeviceInformation>> ScanBluetoothDevicesAsync()
        {
            try
            {
                // 设置需要的属性
                string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };
                
                // 获取蓝牙LE设备选择器
                string selector = BluetoothLEDevice.GetDeviceSelector();
                
                // 查找所有设备
                var devices = await DeviceInformation.FindAllAsync(selector, requestedProperties);
                
                Console.WriteLine($"搜索到 {devices.Count} 个蓝牙设备:");
                foreach (var device in devices)
                {
                    Console.WriteLine($"  - {device.Name} (ID: {device.Id})");
                }
                
                return devices.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"扫描设备时出错: {ex.Message}");
                return new List<DeviceInformation>();
            }
        }

        /// <summary>
        /// 读取指定设备的电池电量
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>电池电量百分比 (0-100)，如果失败返回-1</returns>
        public static async Task<int> ReadBatteryLevelAsync(string deviceId)
        {
            BluetoothLEDevice device = null;
            
            try
            {
                // 连接到设备
                Console.WriteLine($"正在连接到设备: {deviceId}");
                device = await BluetoothLEDevice.FromIdAsync(deviceId);
                
                if (device == null)
                {
                    Console.WriteLine("无法连接到设备");
                    return -1;
                }
                
                Console.WriteLine($"已连接到: {device.Name}");
                
                // 获取电池服务
                var servicesResult = await device.GetGattServicesForUuidAsync(BatteryServiceUuid);
                
                if (servicesResult.Status != GattCommunicationStatus.Success)
                {
                    Console.WriteLine($"无法获取GATT服务: {servicesResult.Status}");
                    return -1;
                }
                
                if (servicesResult.Services.Count == 0)
                {
                    Console.WriteLine("设备不支持电池服务");
                    return -1;
                }
                
                var batteryService = servicesResult.Services[0];
                Console.WriteLine("找到电池服务");
                
                // 获取电池电量特征
                var characteristicsResult = await batteryService.GetCharacteristicsForUuidAsync(BatteryLevelCharacteristicUuid);
                
                if (characteristicsResult.Status != GattCommunicationStatus.Success)
                {
                    Console.WriteLine($"无法获取特征: {characteristicsResult.Status}");
                    return -1;
                }
                
                if (characteristicsResult.Characteristics.Count == 0)
                {
                    Console.WriteLine("未找到设备电量特征");
                    return -1;
                }
                
                var batteryLevelCharacteristic = characteristicsResult.Characteristics[0];
                Console.WriteLine("找到设备电量特征");
                
                // 读取电池电量值
                var readResult = await batteryLevelCharacteristic.ReadValueAsync();
                
                if (readResult.Status != GattCommunicationStatus.Success)
                {
                    Console.WriteLine($"读取电池电量失败: {readResult.Status}");
                    return -1;
                }
                
                // 解析电池电量 (电池电量是一个字节，范围0-100)
                var reader = DataReader.FromBuffer(readResult.Value);
                byte batteryLevel = reader.ReadByte();
                
                Console.WriteLine($"设备电量: {batteryLevel}%");
                return batteryLevel;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取设备电量时出错: {ex.Message}");
                return -1;
            }
            finally
            {
                // 释放设备资源
                device?.Dispose();
            }
        }

        /// <summary>
        /// 读取指定设备的电池电量（通过设备名称）
        /// </summary>
        public static async Task<int> ReadBatteryLevelByNameAsync(string deviceName)
        {
            var devices = await ScanBluetoothDevicesAsync();
            var targetDevice = devices.FirstOrDefault(d => 
                d.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase));
            
            if (targetDevice == null)
            {
                Console.WriteLine($"未找到名为 '{deviceName}' 的设备");
                return -1;
            }
            
            return await ReadBatteryLevelAsync(targetDevice.Id);
        }

        /// <summary>
        /// 监听电池电量变化（订阅通知）
        /// </summary>
        public static async Task MonitorBatteryLevelAsync(string deviceId)
        {
            BluetoothLEDevice device = null;
            GattCharacteristic batteryLevelCharacteristic = null;
            
            try
            {
                device = await BluetoothLEDevice.FromIdAsync(deviceId);
                
                if (device == null)
                {
                    Console.WriteLine("无法连接到设备");
                    return;
                }
                
                var servicesResult = await device.GetGattServicesForUuidAsync(BatteryServiceUuid);
                if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
                {
                    Console.WriteLine("无法获取电池服务");
                    return;
                }
                
                var batteryService = servicesResult.Services[0];
                var characteristicsResult = await batteryService.GetCharacteristicsForUuidAsync(BatteryLevelCharacteristicUuid);
                
                if (characteristicsResult.Status != GattCommunicationStatus.Success || characteristicsResult.Characteristics.Count == 0)
                {
                    Console.WriteLine("无法获取电池电量特征");
                    return;
                }
                
                batteryLevelCharacteristic = characteristicsResult.Characteristics[0];
                
                // 检查是否支持通知
                if (!batteryLevelCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                {
                    Console.WriteLine("该设备不支持电池电量通知");
                    return;
                }
                
                // 订阅电池电量变化通知
                batteryLevelCharacteristic.ValueChanged += BatteryLevel_ValueChanged;
                
                var status = await batteryLevelCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
                
                if (status == GattCommunicationStatus.Success)
                {
                    Console.WriteLine("已订阅电池电量通知，按任意键停止监听...");
                    Console.ReadKey();
                }
                else
                {
                    Console.WriteLine($"订阅通知失败: {status}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"监听电池电量时出错: {ex.Message}");
            }
            finally
            {
                if (batteryLevelCharacteristic != null)
                {
                    batteryLevelCharacteristic.ValueChanged -= BatteryLevel_ValueChanged;
                    await batteryLevelCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.None);
                }
                device?.Dispose();
            }
        }

        private static void BatteryLevel_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte batteryLevel = reader.ReadByte();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 电池电量更新: {batteryLevel}%");
        }
    }
}
