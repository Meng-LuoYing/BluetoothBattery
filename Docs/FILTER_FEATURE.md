# 🔌 已连接设备过滤功能

## 功能说明

现在应用程序默认**只显示已连接的蓝牙设备**，不再显示所有已配对但未连接的设备。

![用户上传的截图](C:/Users/50234/.gemini/antigravity/brain/3594d576-0cf5-48c6-a8a8-69875713b528/uploaded_media_1769676875517.png)

如图所示，Windows 蓝牙设置中虽然有多个配对设备，但只有 **AULA-SC650** 和 **R65 5.0** 是已连接状态。现在应用程序会自动过滤，只显示这两个设备。

## ✨ 新增功能

### 1. 连接状态检测
自动检测每个设备的连接状态：
- ✅ 优先使用设备属性 `System.Devices.Aep.IsConnected`
- ✅ 备用方案：通过 `BluetoothLEDevice.ConnectionStatus` 检查
- ✅ 只显示真正连接的设备

### 2. 过滤开关
在界面顶部添加了 **"仅显示已连接设备"** 复选框：
- ✅ 默认启用（勾选状态）
- ✅ 取消勾选可显示所有设备
- ✅ 切换时自动重新扫描

## 🎯 使用方法

### 默认模式（仅已连接）
1. 启动应用程序
2. 点击"扫描设备"
3. **只会显示已连接的设备**（如 AULA-SC650、R65 5.0）

### 显示所有设备
1. 取消勾选 **"仅显示已连接设备"**
2. 点击"扫描设备"
3. 会显示所有配对过的设备（包括未连接的）

## 🔧 技术实现

### 连接状态检测方法

```csharp
private async Task<bool> IsDeviceConnectedAsync(DeviceInformation deviceInfo)
{
    try
    {
        // 方法1：检查设备属性中的连接状态
        if (deviceInfo.Properties.TryGetValue("System.Devices.Aep.IsConnected", out object? isConnectedObj))
        {
            if (isConnectedObj is bool isConnected)
            {
                return isConnected;
            }
        }
        
        // 方法2：通过 BluetoothLEDevice 检查连接状态
        var device = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
        if (device != null)
        {
            var connected = device.ConnectionStatus == BluetoothConnectionStatus.Connected;
            device.Dispose();
            return connected;
        }
    }
    catch
    {
        // 忽略错误，默认返回 false
    }
    
    return false;
}
```

### 设备添加时的过滤逻辑

```csharp
private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
{
    await Dispatcher.InvokeAsync(async () =>
    {
        if (!deviceCards.ContainsKey(deviceInfo.Id))
        {
            // 检查连接状态
            bool isConnected = await IsDeviceConnectedAsync(deviceInfo);
            
            // 如果启用了"仅显示已连接"过滤，则跳过未连接的设备
            if (showConnectedOnly && !isConnected)
            {
                return;  // 跳过未连接的设备
            }
            
            await CreateDeviceCardAsync(deviceInfo);
            DeviceCountText.Text = $"已找到 {deviceCards.Count} 个设备";
        }
    });
}
```

## 📊 效果对比

### 之前的问题
- ❌ 显示所有配对过的设备（10+ 个）
- ❌ 包括大量未连接的设备
- ❌ 难以找到真正在用的设备

### 现在的效果
- ✅ 只显示已连接的设备（2 个）
- ✅ 清晰明了，一目了然
- ✅ 可选择显示所有设备

## 🎨 UI 更新

在标题栏添加了过滤选项：

```xml
<CheckBox Grid.Column="1"
          x:Name="ConnectedOnlyCheckBox"
          Content="仅显示已连接设备"
          IsChecked="True"
          Foreground="#AAAAAA"
          VerticalAlignment="Center"
          Margin="0,0,20,0"
          Checked="ConnectedOnlyCheckBox_Changed"
          Unchecked="ConnectedOnlyCheckBox_Changed"
          FontSize="13"/>
```

## ✅ 总结

现在应用程序更加实用：
- ✅ 默认只显示已连接设备，避免列表混乱
- ✅ 提供开关选项，灵活切换显示模式
- ✅ 自动检测连接状态，准确过滤
- ✅ 符合用户实际使用需求

您现在可以重新运行应用程序，扫描时将只看到真正连接的蓝牙设备！
