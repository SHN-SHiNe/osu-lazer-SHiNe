# Mania双背景系统

这个系统为osu!的Mania模式提供了独立的背景层功能，允许为舞台区域设置独立的背景效果。

## 系统架构

### 核心组件

1. **BackgroundScreenBeatmap_Mania** - Mania专用的背景屏幕
2. **DualBackgroundManager** - 双背景管理器
3. **ManiaBackgroundSettings** - Mania背景设置界面
4. **DualBackgroundIntegrationExample** - 集成示例

### 层次结构

```
DualBackgroundManager
├── BackgroundScreenBeatmap (主背景层)
│   └── DimmableBackground
│       └── dimContent
│           └── AudioReactiveBeatmapBackground
│               ├── Sprite (基础背景图片)
│               └── storyboardContainer
│                   └── DrawableStoryboard
└── BackgroundScreenBeatmap_Mania (Mania背景层)
    └── DimmableBackground
        └── stageMaskContainer (舞台区域遮罩)
            └── AudioReactiveBeatmapBackground
                ├── Sprite (基础背景图片)
                └── storyboardContainer
                    └── DrawableStoryboard
```

## 功能特性

### 1. 独立背景层
- 主背景层：显示完整的背景内容
- Mania背景层：只显示舞台区域的背景内容

### 2. 舞台区域遮罩
- 自动检测ManiaPlayfield的舞台区域
- 动态调整遮罩大小和位置
- 支持多列布局的舞台

### 3. 独立参数控制
- **Mania背景暗化** (`ManiaBackgroundDimLevel`) - 控制舞台区域背景的暗化程度
- **Mania背景模糊** (`ManiaBackgroundBlurLevel`) - 控制舞台区域背景的模糊程度
- **启用Mania独立背景层** (`EnableManiaBackgroundLayer`) - 开关功能

### 4. 音频反应特效
- 支持与主背景相同的音频反应特效
- 独立的震动和缩放参数
- 实时音频分析

## 使用方法

### 1. 基本集成

```csharp
public partial class ManiaGameplayScreen : Container
{
    private DualBackgroundManager backgroundManager;
    private ManiaPlayfield maniaPlayfield;

    [BackgroundDependencyLoader]
    private void load()
    {
        RelativeSizeAxes = Axes.Both;

        // 添加双背景管理器
        Add(backgroundManager = new DualBackgroundManager
        {
            RelativeSizeAxes = Axes.Both,
            Depth = float.MaxValue,
        });

        // 添加Mania舞台
        Add(maniaPlayfield = new ManiaPlayfield
        {
            RelativeSizeAxes = Axes.Both,
        });

        // 连接舞台到背景管理器
        backgroundManager.UpdateManiaPlayfield(maniaPlayfield);
    }

    public WorkingBeatmap Beatmap
    {
        set => backgroundManager.Beatmap = value;
    }
}
```

### 2. 配置设置

用户可以在设置界面中调整以下参数：

- **启用Mania独立背景层**: 开启/关闭Mania背景层
- **Mania背景暗化**: 0-100%，控制舞台区域背景的暗化程度
- **Mania背景模糊**: 0-100%，控制舞台区域背景的模糊程度

### 3. 高级用法

```csharp
// 获取主背景引用
var mainBackground = backgroundManager.MainBackground;

// 获取Mania背景引用
var maniaBackground = backgroundManager.ManiaBackground;

// 动态调整Mania背景的模糊
maniaBackground.BlurAmount.Value = 0.5f;

// 动态调整Mania背景的暗化
maniaBackground.DimWhenUserSettingsIgnored.Value = 0.3f;
```

## 技术实现

### 1. 遮罩系统
- 使用`Container.Masking = true`实现舞台区域遮罩
- 动态计算`ManiaPlayfield.SkinnableComponentScreenSpaceDrawQuad`获取舞台边界
- 自动调整遮罩容器的位置和大小

### 2. 配置系统
- 新增`OsuSetting`枚举值：
  - `ManiaBackgroundDimLevel`
  - `ManiaBackgroundBlurLevel`
  - `EnableManiaBackgroundLayer`
- 独立的配置绑定，不影响主背景设置

### 3. 渲染优化
- 使用`BufferedContainer`实现模糊效果
- 智能分辨率缩放减少性能开销
- 条件渲染避免不必要的计算

## 性能考虑

1. **内存使用**: 双背景层会增加内存使用，但通过智能缓存和条件渲染优化
2. **渲染性能**: 遮罩和模糊效果会增加GPU负载，建议在低端设备上适当降低设置
3. **音频分析**: 两个背景层共享同一个音频分析器，避免重复计算

## 扩展性

这个系统设计为可扩展的，可以轻松添加：
- 其他游戏模式的独立背景层
- 更多特效类型
- 自定义遮罩形状
- 动态遮罩动画

## 注意事项

1. 确保在设置ManiaPlayfield后再设置Beatmap，以保证正确的遮罩计算
2. 舞台区域变化时会自动更新遮罩，无需手动处理
3. 配置更改会实时生效，无需重启游戏
