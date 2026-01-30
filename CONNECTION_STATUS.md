# 🔍 连接状态显示优化

## 问题说明

![用户上传的截图](C:/Users/50234/.gemini/antigravity/brain/3594d576-0cf5-48c6-a8a8-69875713b528/uploaded_media_1769677048108.png)

从截图中可以看到，即使设备未连接（如 PCV2、VERO 75、ROG STRIX IMPACT III WIRELESS、Mi MK71Pro），仍然显示了电池电量。

### 为什么会这样？

这是 **Windows 蓝牙缓存机制**的正常行为：
- Windows 会缓存蓝牙设备的电池信息
- 即使设备断开连接，仍可读取上次连接时的电池电量
- 这可能让用户误以为设备仍然连接

## ✨ 优化方案

### 1. 添加连接状态标签
在每个设备卡片的名称旁边显示状态徽章：
- 🟢 **已连接** - 绿色徽章
- ⚫ **未连接** - 灰色徽章

### 2. 区分电池信息显示
根据连接状态显示不同的文本：
- **已连接设备**：显示 "电池电量: XX%"
- **未连接设备**：显示 "上次已知电量: XX%"（灰色文字）

### 3. 视觉差异化
未连接设备的电池进度条和百分比使用**暗淡的颜色**（60% 亮度），与已连接设备区分开来。

## 🎨 UI 效果

### 已连接设备
```
┌─────────────────────────────────────────┐
│ R65 5.0 [已连接]               73%      │
│ ID: ec-ce-0e:e4:88:11:be               │
│ 🔋 电池电量: 73%         ████████░░     │
└─────────────────────────────────────────┘
```
- 绿色"已连接"徽章
- 正常亮度的进度条
- "电池电量"文本

### 未连接设备
```
┌─────────────────────────────────────────┐
│ PCV2 [未连接]                  75%      │
│ ID: ec-c2:be:c2:0e:fa:fe               │
│ 🔋 上次已知电量: 75%     ████████░░     │
└─────────────────────────────────────────┘
```
- 灰色"未连接"徽章
- 暗淡的进度条（60% 亮度）
- "上次已知电量"文本（灰色）

## 🔧 技术实现

### 1. 连接状态检测
```csharp
bool isConnected = await IsDeviceConnectedAsync(deviceInfo);
```

### 2. 状态徽章创建
```csharp
var statusBadge = new Border
{
    Background = new SolidColorBrush(
        isConnected ? Color.FromRgb(76, 175, 80) : Color.FromRgb(128, 128, 128)
    ),
    CornerRadius = new CornerRadius(10),
    Padding = new Thickness(8, 2, 8, 2),
    Margin = new Thickness(10, 0, 0, 0)
};

var statusText = new TextBlock
{
    Text = isConnected ? "已连接" : "未连接",
    FontSize = 11,
    Foreground = Brushes.White,
    FontWeight = FontWeights.SemiBold
};
```

### 3. 电池信息差异化显示
```csharp
if (isConnected)
{
    batteryText.Text = $"电池电量: {batteryLevel}%";
}
else
{
    batteryText.Text = $"上次已知电量: {batteryLevel}%";
    batteryText.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));
}
```

### 4. 颜色暗淡处理
```csharp
// 未连接的设备使用灰色调（60% 亮度）
if (!isConnected)
{
    color = Color.FromRgb(
        (byte)(color.R * 0.6),
        (byte)(color.G * 0.6),
        (byte)(color.B * 0.6)
    );
}
```

## 📊 改进效果

### 之前
- ❌ 无法区分设备是否连接
- ❌ 缓存的电量可能误导用户
- ❌ 所有设备显示效果相同

### 现在
- ✅ 清晰的连接状态标签
- ✅ 明确标注"上次已知电量"
- ✅ 视觉上区分已连接和未连接设备
- ✅ 用户一眼就能看出哪些设备真正连接

## 💡 使用建议

1. **查看已连接设备**
   - 勾选"仅显示已连接设备"
   - 只显示绿色"已连接"徽章的设备

2. **查看所有设备**
   - 取消勾选"仅显示已连接设备"
   - 可以看到所有设备及其上次已知电量
   - 灰色"未连接"徽章的设备显示缓存的电量

3. **电量准确性**
   - "已连接"设备的电量是实时读取的
   - "未连接"设备的电量是上次连接时的缓存值
   - 未连接设备的电量可能不准确

## ✅ 总结

现在应用程序能够：
- ✅ 清晰区分已连接和未连接的设备
- ✅ 准确标注电池信息的来源（实时 vs 缓存）
- ✅ 通过视觉效果帮助用户快速识别设备状态
- ✅ 避免用户对未连接设备的电量产生误解

重新运行应用程序后，您将看到每个设备卡片上都有连接状态标签！
