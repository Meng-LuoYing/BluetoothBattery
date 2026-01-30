# 蓝牙设备电池电量读取工具

这是一个用于在 Windows 11 上读取蓝牙设备电池电量的 C# 应用程序。

## 功能特性

- ✅ 扫描并列出所有蓝牙 LE 设备
- ✅ 通过设备 ID 或名称读取电池电量
- ✅ 实时监听电池电量变化
- ✅ 使用标准蓝牙电池服务 (UUID: 0x180F)

## 系统要求

- Windows 10 (版本 1809 或更高) 或 Windows 11
- .NET 6.0 或更高版本
- 支持蓝牙 LE 的硬件

## 使用方法

### 1. 编译项目

```bash
dotnet build
```

### 2. 运行程序

```bash
dotnet run
```

### 3. 使用示例

程序提供了交互式菜单，您可以：

1. **扫描蓝牙设备** - 列出所有可用的蓝牙 LE 设备
2. **读取电池电量 (通过 ID)** - 使用设备 ID 读取电池电量
3. **读取电池电量 (通过名称)** - 使用设备名称读取电池电量
4. **监听电池电量变化** - 实时监听设备的电池电量更新

## 代码示例

### 扫描设备

```csharp
var devices = await BatteryReader.ScanBluetoothDevicesAsync();
```

### 读取电池电量

```csharp
// 通过设备 ID
int batteryLevel = await BatteryReader.ReadBatteryLevelAsync(deviceId);

// 通过设备名称
int batteryLevel = await BatteryReader.ReadBatteryLevelByNameAsync("设备名称");
```

### 监听电池电量变化

```csharp
await BatteryReader.MonitorBatteryLevelAsync(deviceId);
```

## 重要说明

### 权限配置

在 Windows 11 上，您需要确保应用程序具有蓝牙权限。如果是 UWP 应用，需要在 `Package.appxmanifest` 中添加：

```xml
<Capabilities>
  <DeviceCapability Name="bluetooth" />
</Capabilities>
```

对于桌面应用程序，确保在 Windows 设置中启用了蓝牙权限。

### 支持的设备

此工具适用于支持标准蓝牙电池服务 (Battery Service - 0x180F) 的设备，包括：

- 蓝牙耳机
- 蓝牙鼠标
- 蓝牙键盘
- 智能手表
- 其他支持 BLE 的设备

### 故障排除

**问题：找不到设备**
- 确保设备已配对并连接
- 检查蓝牙是否已启用
- 尝试重新配对设备

**问题：无法读取电池电量**
- 确认设备支持电池服务
- 某些设备可能需要先连接才能读取电池信息
- 检查设备是否在范围内

**问题：编译错误**
- 确保使用的是 .NET 6.0 或更高版本
- 确保目标框架设置为 `net6.0-windows10.0.19041.0`

## 技术细节

### 使用的 UUID

- **电池服务 UUID**: `0000180F-0000-1000-8000-00805F9B34FB`
- **电池电量特征 UUID**: `00002A19-0000-1000-8000-00805F9B34FB`

### API 参考

- `Windows.Devices.Bluetooth.BluetoothLEDevice`
- `Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceService`
- `Windows.Devices.Enumeration.DeviceInformation`

## 许可证

此项目仅供学习和参考使用。
