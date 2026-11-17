# Mania双背景系统使用说明

## ✅ 集成完成！

Mania双背景系统已成功集成到osu!游戏中。现在您可以直接在游戏中测试这个功能了！

## 🎮 如何在游戏中测试

### 1. 启动游戏
```bash
dotnet run --project osu.Desktop -c Release
```

### 2. 选择Mania谱面
- 选择任意Mania谱面开始游戏
- 系统会自动检测Mania模式并记录日志

### 3. 调整设置
1. 在游戏中按 `Ctrl + O` 打开设置
2. 进入"游戏玩法" → "Mania背景设置"
3. 调整以下设置：
   - ✅ **启用Mania独立背景层** - 开关控制
   - ✅ **Mania背景暗化** - 0-100% 滑杆控制  
   - ✅ **Mania背景模糊** - 0-100% 滑杆控制

## 🎯 预期效果

- **主背景层**: 显示完整的背景内容
- **Mania背景层**: 只在舞台区域显示背景内容
- **独立控制**: 两个背景层可以有不同的模糊和暗化设置
- **实时生效**: 设置更改立即生效

## 📁 已创建的文件

**核心系统：**
- `BackgroundScreenBeatmap_Mania.cs` - Mania专用背景屏幕
- `DualBackgroundManager.cs` - 双背景管理器
- `Player.cs` - 修改了Player基类以支持Mania双背景

**设置界面：**
- `ManiaBackgroundSettings.cs` - Mania背景设置界面
- `GameplaySection.cs` - 添加了Mania背景设置到游戏玩法设置

**配置系统：**
- `OsuConfigManager.cs` - 添加了Mania背景相关配置项

**文档：**
- `README_DualBackgroundSystem.md` - 详细技术文档
- `MANIA_DUAL_BACKGROUND_USAGE.md` - 使用说明
- `SystemArchitecture.txt` - 系统架构图

## 🔧 技术特性

- ✅ **完全独立**: 两个背景层完全独立控制
- ✅ **智能遮罩**: 自动检测舞台区域并创建精确遮罩
- ✅ **性能优化**: 条件渲染、智能缓存
- ✅ **实时更新**: 配置更改立即生效
- ✅ **向后兼容**: 不影响现有背景系统

## 📝 注意事项

- 系统仅在Mania模式下激活
- 舞台区域遮罩基于ManiaPlayfield自动计算
- 支持所有背景类型：静态图片、视频、故事板、音频反应背景
- 设置会自动保存到配置文件

## 🚀 开始测试

现在您可以直接在Mania游戏中测试这个双背景系统了！系统会自动检测Mania模式并启用，您可以通过设置界面调整各种参数来观察效果。
