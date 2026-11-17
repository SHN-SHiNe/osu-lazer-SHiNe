# 调试指南

## 🚀 快速开始

### Windows 用户
```batch
# 双击运行或在命令行执行
build-and-run.bat
```

### Linux/Mac 用户
```bash
# 添加执行权限
chmod +x build-and-run.sh

# 运行
./build-and-run.sh
```

### 手动构建和运行
```bash
# 构建
dotnet build osu.Desktop -c Release

# 运行
dotnet run --project osu.Desktop -c Release
```

## 📝 调试日志说明

所有与 Mania 播放器模式相关的日志都以 `[ManiaMode]` 开头，方便过滤和查找。

### 主要日志点

#### 1. 启动流程
```
[ManiaMode] Creating ManiaPlayerLoader instead of default Loader
[ManiaMode] ManiaPlayerLoader: Creating ManiaPlayerScreen (skipping intro and main menu)
[ManiaMode] ManiaPlayerScreen: Loading waiting screen UI
[ManiaMode] ManiaPlayerScreen: Screen entered, waiting for MP3 drag & drop
```

#### 2. 拖拽 MP3 文件
```
[ManiaMode] MP3 file dropped: "song.mp3"
[ManiaMode] Full path: "C:\path\to\song.mp3"
[ManiaMode] handleManiaPlayback started
[ManiaMode] Scheduled task executing
[ManiaMode] Directory: C:\path\to
[ManiaMode] Searching for .osu files in: C:\path\to
[ManiaMode] Found 3 .osu files
[ManiaMode] Using .osu file: C:\path\to\beatmap.osu
```

#### 3. 导入谱面
```
[ManiaMode] importAndPlayBeatmap started
[ManiaMode] Creating temporary .osz file: C:\Temp\temp_xxxxx.osz
[ManiaMode] .osz file created successfully
[ManiaMode] Starting beatmap import...
[ManiaMode] Import task completed. Status: RanToCompletion
[ManiaMode] Import successful! Imported 1 beatmap set(s)
[ManiaMode] Processing imported beatmap...
[ManiaMode] Beatmap set contains 5 difficulties
[ManiaMode] Found mania beatmap!
[ManiaMode] Setting ruleset to mania
[ManiaMode] Setting beatmap: Song Title
[ManiaMode] Clearing mods
[ManiaMode] Starting mania player...
[ManiaMode] Temporary file deleted
```

#### 4. 播放器启动
```
[ManiaMode] Creating ManiaEndlessPlayer instance
[ManiaMode] Pushing ManiaEndlessPlayer to screen stack
[ManiaMode] ManiaEndlessPlayer: Loading player
[ManiaMode] ManiaEndlessPlayer: Adding NoFail mod
[ManiaMode] ManiaEndlessPlayer: NoFail mod added successfully
[ManiaMode] ManiaEndlessPlayer: LoadComplete called
[ManiaMode] ManiaEndlessPlayer: Setting up completion callback
```

#### 5. 播放完成
```
[ManiaMode] ManiaEndlessPlayer: Beatmap playback completed!
[ManiaMode] Beatmap completed callback triggered
[ManiaMode] Exiting player screen, returning to waiting screen
```

#### 6. 错误日志
```
[ManiaMode] ERROR: Invalid file path
[ManiaMode] ERROR: No .osu files found in the same directory
[ManiaMode] ERROR: Failed to import beatmap - task result is null or empty
```

## 🔍 如何查看日志

### 方法 1: 控制台输出
运行游戏时，所有日志会直接输出到控制台。使用 Ctrl+F 搜索 `[ManiaMode]` 来过滤相关日志。

### 方法 2: 日志文件
日志文件位于：
- Windows: `%AppData%\osu\logs`
- Linux: `~/.local/share/osu/logs`
- macOS: `~/Library/Application Support/osu/logs`

最新的日志文件名为 `runtime.log`

### 方法 3: Visual Studio 调试
```bash
# 使用 Visual Studio 打开解决方案
osu.sln
```
设置断点并按 F5 启动调试。

## 🐛 常见问题排查

### 问题 1: 拖拽 MP3 后没有反应
**检查日志中是否出现：**
```
[ManiaMode] MP3 file dropped: "xxx.mp3"
[ManiaMode] Full path: "..."
```

**如果没有，可能原因：**
- 文件不是 .mp3 格式
- 拖拽事件未被正确捕获

**如果有，继续检查：**
```
[ManiaMode] Found X .osu files
```

**如果是 0 个：**
- 确保 MP3 同文件夹下有 .osu 文件
- 检查文件权限

### 问题 2: 导入失败
**检查日志：**
```
[ManiaMode] Import task completed. Status: XXX
```

**如果状态不是 RanToCompletion：**
- 查看异常信息
- 检查 .osu 文件格式是否正确
- 检查磁盘空间

### 问题 3: 播放器启动失败
**检查日志：**
```
[ManiaMode] ManiaEndlessPlayer: Loading player
```

**可能原因：**
- Mania ruleset 未正确加载
- Beatmap 数据损坏

### 问题 4: NoFail mod 未生效
**检查日志：**
```
[ManiaMode] ManiaEndlessPlayer: NoFail mod added successfully
```

**如果看到警告：**
```
[ManiaMode] ManiaEndlessPlayer: WARNING - Could not create NoFail mod
```
说明 NoFail mod 创建失败，需要检查 Mania ruleset 是否正确加载。

## 🔧 手动调试

### 启用更详细的日志
修改 `OsuGame.cs` 中的日志级别：
```csharp
Logger.Log("[ManiaMode] xxx", LoggingTarget.Runtime, LogLevel.Debug);
```

### 添加断点调试
在以下关键位置添加断点：
1. `OsuGame.cs` - `onWindowDragDrop()` 方法
2. `OsuGame.cs` - `handleManiaPlayback()` 方法
3. `OsuGame.cs` - `importAndPlayBeatmap()` 方法
4. `ManiaEndlessPlayer.cs` - `load()` 方法
5. `ManiaEndlessPlayer.cs` - `LoadComplete()` 方法

### 测试建议
1. 准备一个简单的测试用谱面文件夹：
   ```
   test/
   ├── test.mp3
   └── test.osu
   ```

2. 逐步测试：
   - 启动游戏
   - 拖拽 MP3
   - 观察日志输出
   - 检查每个步骤

## 📊 性能监控

### 检查导入性能
```
[ManiaMode] .osz file created successfully
[ManiaMode] Starting beatmap import...
[时间间隔应该很短]
[ManiaMode] Import task completed
```

### 检查播放器加载时间
```
[ManiaMode] Starting mania player...
[ManiaMode] ManiaEndlessPlayer: Loading player
[时间间隔应该很短]
[ManiaMode] ManiaEndlessPlayer: LoadComplete called
```

## 🆘 获取帮助

如果遇到问题：
1. 收集完整的日志输出
2. 记录重现步骤
3. 检查是否有错误堆栈信息
4. 查看 `MANIA_PLAYER_MODE.md` 了解功能说明

## 📝 日志过滤技巧

### PowerShell (Windows)
```powershell
Get-Content runtime.log | Select-String "\[ManiaMode\]"
```

### Bash (Linux/Mac)
```bash
grep "\[ManiaMode\]" runtime.log
```

### 实时监控
```bash
# Linux/Mac
tail -f runtime.log | grep "\[ManiaMode\]"

# Windows PowerShell
Get-Content runtime.log -Wait | Select-String "\[ManiaMode\]"
```

