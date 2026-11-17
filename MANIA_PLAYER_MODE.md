# Mania 播放器模式

这是一个简化的 osu!mania 游戏版本，专注于提供类似音乐播放器的体验。

## 功能特点

1. **仅 Mania 模式**：游戏只支持 mania 模式，移除了其他游戏模式
2. **无选歌界面**：跳过了传统的主菜单和选歌界面
3. **拖拽播放**：直接拖拽 MP3 文件到游戏窗口即可开始播放
4. **自动搜索谱面**：自动在 MP3 文件所在文件夹搜索 .osu 谱面文件
5. **无死亡机制**：游戏过程中不会因为失误而死亡（自动启用 NoFail 模式）
6. **无结算界面**：播放完毕后不显示结算界面，直接返回等待拖拽状态
7. **循环等待**：始终保持在 mania 舞台界面或等待界面，随时准备接受新的拖拽

## 使用方法

### 启动游戏

1. 运行游戏后，会直接进入等待拖拽文件的界面
2. 界面会显示提示：「拖拽 MP3 文件到此窗口开始播放」

### 播放音乐

1. 准备一个包含以下文件的文件夹：
   - 音频文件（.mp3 格式）
   - 谱面文件（.osu 格式，至少一个）
   - 其他相关资源文件（背景图等，可选）

2. 将 MP3 文件拖拽到游戏窗口中

3. 游戏会自动：
   - 搜索 MP3 所在文件夹的所有 .osu 文件
   - 导入第一个找到的 .osu 文件
   - 如果有 mania 模式的谱面，优先使用
   - 如果没有，则将其他模式的谱面转换为 mania 模式
   - 自动开始播放

### 中途更换歌曲

- 在游戏进行中，随时可以拖拽新的 MP3 文件
- 当前播放会停止，新的歌曲会立即开始

### 退出游戏

- 按 ESC 键或 Alt+F4 退出游戏

## 文件结构示例

```
我的歌曲文件夹/
├── song.mp3          # 音频文件
├── easy.osu          # 简单难度谱面
├── normal.osu        # 普通难度谱面
├── hard.osu          # 困难难度谱面
└── background.jpg    # 背景图片（可选）
```

## 技术说明

### 修改的文件

1. **OsuGame.cs**
   - 修改了 `CreateLoader()` 方法，使用 `ManiaPlayerLoader` 替代默认 Loader
   - 修改了 `onWindowDragDrop()` 方法，添加 MP3 文件处理逻辑
   - 添加了 `handleManiaPlayback()` 方法来处理拖拽的 MP3 文件
   - 添加了 `importAndPlayBeatmap()` 方法来导入并播放谱面
   - 添加了 `startManiaPlayer()` 方法来启动无尽播放器

2. **新增文件**
   - `Screens/ManiaPlayerLoader.cs` - 简化的加载器
   - `Screens/Play/ManiaPlayerScreen.cs` - 等待拖拽的屏幕
   - `Screens/Play/ManiaEndlessPlayer.cs` - 无死亡、无结算的播放器

### 核心特性实现

#### 1. 跳过主菜单
通过自定义 `ManiaPlayerLoader` 直接加载 `ManiaPlayerScreen` 而不是介绍动画和主菜单。

#### 2. 拖拽文件处理
在 `OsuGame.cs` 的 `onWindowDragDrop` 方法中检测 MP3 文件，并自动搜索同文件夹的 .osu 文件。

#### 3. 禁用死亡机制
`ManiaEndlessPlayer` 重写了 `PerformFail()` 方法，使其不执行任何操作，并自动添加 NoFail mod。

#### 4. 禁用结算界面
通过 `PlayerConfiguration` 设置 `ShowResults = false`，并重写 `CreateResults()` 返回 null。

#### 5. 自动返回等待状态
监听 `GameplayClockContainer.Completed` 事件，在播放完成后自动退出并返回等待屏幕。

## 注意事项

1. **文件格式**：目前只支持 MP3 格式的音频文件
2. **谱面要求**：必须在 MP3 同文件夹下有至少一个 .osu 文件
3. **导入机制**：每次拖拽都会临时导入谱面，可能会占用磁盘空间
4. **性能考虑**：频繁拖拽可能导致临时文件累积，建议定期清理临时文件夹

## 开发者信息

如需恢复原版功能，可以：
1. 将 `OsuGame.cs` 中的 `CreateLoader()` 改回 `return new Loader();`
2. 移除 `onWindowDragDrop` 中的 MP3 文件处理逻辑
3. 删除新增的三个文件

## 许可证

遵循 osu! 的 MIT 许可证。

