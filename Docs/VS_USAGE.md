# 🎯 使用 Visual Studio 运行项目

## 方法一：直接打开解决方案文件（推荐）

1. **双击打开解决方案**
   - 找到 `BluetoothBatteryUI.sln` 文件
   - 双击打开，会自动启动 Visual Studio

2. **运行项目**
   - 按 `F5` 键（调试运行）
   - 或点击顶部工具栏的 ▶️ **启动** 按钮
   - 或按 `Ctrl + F5`（不调试运行）

3. **设置启动项目**（如果需要）
   - 在解决方案资源管理器中
   - 右键点击 `BluetoothBatteryUI` 项目
   - 选择"设为启动项目"

## 方法二：从 Visual Studio 打开

1. **启动 Visual Studio**
   - 打开 Visual Studio 2022（或 2019）

2. **打开项目**
   - 点击"打开项目或解决方案"
   - 浏览到 `C:\Users\50234\Desktop\新建文件夹`
   - 选择 `BluetoothBatteryUI.sln` 或 `BluetoothBatteryUI.csproj`
   - 点击"打开"

3. **运行项目**
   - 按 `F5` 开始调试

## 方法三：使用命令行（当前方式）

如果您更喜欢命令行方式：

```powershell
# 进入项目目录
cd C:\Users\50234\Desktop\新建文件夹

# 运行项目
dotnet run --project BluetoothBatteryUI.csproj

# 或者先编译再运行
dotnet build
dotnet run
```

## 📝 Visual Studio 的优势

### 1. 可视化设计器
- **XAML 设计器**：可视化编辑 UI 界面
- **实时预览**：边编辑边看效果
- **属性窗口**：方便修改控件属性

### 2. 强大的调试功能
- **断点调试**：在代码中设置断点
- **变量监视**：实时查看变量值
- **调用堆栈**：追踪代码执行路径
- **即时窗口**：运行时执行代码

### 3. 智能提示
- **IntelliSense**：代码自动完成
- **错误提示**：实时显示编译错误
- **重构工具**：快速重命名、提取方法等

### 4. 项目管理
- **解决方案资源管理器**：清晰的项目结构
- **NuGet 包管理**：方便添加依赖
- **版本控制集成**：Git 支持

## 🔧 调试技巧

### 设置断点
1. 在代码行号左侧点击，出现红点
2. 按 `F5` 运行，程序会在断点处暂停
3. 按 `F10` 单步执行，`F11` 进入函数

### 查看变量
- 鼠标悬停在变量上查看值
- 使用"监视"窗口添加变量
- 使用"即时窗口"执行表达式

### 热重载（Hot Reload）
- Visual Studio 2022 支持 WPF 热重载
- 修改 XAML 后无需重启即可看到效果
- 修改 C# 代码也可以热重载（部分情况）

## 📂 项目文件结构

在 Visual Studio 中您会看到：

```
BluetoothBatteryUI
├── App.xaml                    # 应用程序入口
├── App.xaml.cs                 # 应用程序代码
├── MainWindow.xaml             # 主窗口 UI
├── MainWindow.xaml.cs          # 主窗口逻辑
├── BluetoothBatteryReader.cs   # 蓝牙功能类
├── Program.cs                  # 控制台程序
└── BluetoothBatteryUI.csproj   # 项目配置
```

## ⚙️ 配置选项

### 生成配置
- **Debug**：调试版本，包含调试信息
- **Release**：发布版本，优化性能

### 平台选择
- **x64**：64 位 Windows（推荐）
- **ARM64**：ARM 架构 Windows

### 修改配置
1. 点击顶部工具栏的配置下拉框
2. 选择 "Debug" 或 "Release"
3. 选择平台 "x64" 或 "ARM64"

## 🚀 快捷键

| 快捷键 | 功能 |
|--------|------|
| `F5` | 开始调试 |
| `Ctrl + F5` | 不调试运行 |
| `F10` | 单步跳过 |
| `F11` | 单步进入 |
| `Shift + F5` | 停止调试 |
| `Ctrl + Shift + B` | 生成解决方案 |
| `Ctrl + K, Ctrl + D` | 格式化文档 |
| `F12` | 转到定义 |

## 💡 推荐设置

### 启用 XAML 热重载
1. 工具 → 选项
2. 调试 → .NET/C++ 热重载
3. 勾选"启用 XAML 热重载"

### 显示行号
1. 工具 → 选项
2. 文本编辑器 → 所有语言
3. 勾选"行号"

### 自动保存
1. 工具 → 选项
2. 环境 → 自动恢复
3. 设置自动保存间隔

## ✅ 总结

**推荐使用 Visual Studio**，因为：
- ✅ 可视化 UI 设计器
- ✅ 强大的调试功能
- ✅ 智能代码提示
- ✅ 更好的开发体验

**命令行方式**适合：
- 快速测试
- 自动化构建
- CI/CD 流程

现在您可以双击 `BluetoothBatteryUI.sln` 用 Visual Studio 打开项目了！
