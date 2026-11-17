# Mania Stage Bottom BPM 同步特效实现总结

## 📋 需求回顾

用户需要在 mania 模式下实现一个新的皮肤元素 `mania-stage-bottom-fx@2x.png`，它需要：

1. ✅ 与 `mania-stage-bottom` 的位置和属性完全相同
2. ✅ 覆盖在 `mania-stage-bottom` 上层
3. ✅ 透明度随 BPM 闪动
4. ✅ 使用 `OnNewBeat` 回调，每当音乐到达新节拍时触发
5. ✅ 从 `timingPoint.BeatLength` 获取 BPM 对应的节拍长度
6. ✅ 如果节拍长度 < 250ms 则加倍
7. ✅ 快速淡入到 1 透明度（40ms）
8. ✅ 然后在节拍长度内缓慢淡出到 0 透明度
9. ✅ **每拍都闪**（不过滤节拍）

## 🎯 实现方案

### 修改的文件

**`osu.Game.Rulesets.Mania/Skinning/Legacy/LegacyStageForeground.cs`**

### 核心改动

#### 1. 添加必要的命名空间

```csharp
using osu.Framework.Audio.Track;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Graphics.Containers;
```

这些命名空间支持：
- 音频追踪和节拍同步
- BPM 控制点访问
- BeatSyncedContainer 基类

#### 2. 添加成员变量

```csharp
// BPM 同步的底部特效
private BPMSyncedBottomFX? bpmSyncedFX;
```

#### 3. 在 load() 方法中初始化特效

```csharp
// 创建 BPM 同步的底部特效
bpmSyncedFX = new BPMSyncedBottomFX(skin, bottomImage);

// ... 添加到 children 列表中 ...

// 添加 BPM 同步特效（覆盖在 bottom 上层）
children.Add(bpmSyncedFX);
```

**层级顺序：**
1. `sprite` (mania-stage-bottom) - 底层
2. `bpmSyncedFX` (mania-stage-bottom-fx) - 中层 ⭐ 新增
3. `fxBackground` (left-fx / right-fx) - 顶层

#### 4. 支持滚动方向切换

```csharp
private void onDirectionChanged(ValueChangedEvent<ScrollingDirection> direction)
{
    // ... 现有代码 ...
    
    // 更新 BPM 同步特效的方向
    bpmSyncedFX?.UpdateDirection(direction.NewValue);
}
```

#### 5. 创建 BPMSyncedBottomFX 内部类

```csharp
/// <summary>
/// BPM 同步的底部特效 - 每拍闪动
/// </summary>
public partial class BPMSyncedBottomFX : BeatSyncedContainer
{
    private Drawable? fxSprite;
    private readonly ISkinSource skin;
    private readonly string bottomImageBaseName;

    public BPMSyncedBottomFX(ISkinSource skin, string bottomImageBaseName)
    {
        this.skin = skin;
        this.bottomImageBaseName = bottomImageBaseName;
        
        RelativeSizeAxes = Axes.Both;
        EarlyActivationMilliseconds = 40;  // 提前 40ms 触发
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        // 尝试加载 mania-stage-bottom-fx@2x.png
        string fxImageName = $"{bottomImageBaseName}-fx";
        
        fxSprite = skin.GetAnimation(fxImageName, true, true)?.With(d =>
        {
            d.Scale = new Vector2(1.6f);  // 与 stage-bottom 相同的缩放
            d.Origin = Anchor.BottomCentre;
            d.Anchor = Anchor.BottomCentre;
            d.Alpha = 0; // 初始完全透明
        });

        if (fxSprite != null)
        {
            AddInternal(fxSprite);
        }
    }

    public void UpdateDirection(ScrollingDirection direction)
    {
        if (fxSprite == null)
            return;

        if (direction == ScrollingDirection.Up)
            fxSprite.Anchor = fxSprite.Origin = Anchor.TopCentre;
        else
            fxSprite.Anchor = fxSprite.Origin = Anchor.BottomCentre;
    }

    protected override void OnNewBeat(int beatIndex, TimingControlPoint timingPoint, 
                                       EffectControlPoint effectPoint, ChannelAmplitudes amplitudes)
    {
        base.OnNewBeat(beatIndex, timingPoint, effectPoint, amplitudes);

        if (fxSprite == null)
            return;

        // 每拍都闪动，不进行节拍过滤

        // 获取当前节拍长度
        double length = timingPoint.BeatLength;

        // 如果节拍长度太短（<250ms），则加倍
        while (length < 250)
            length *= 2;

        // 执行淡入淡出动画
        fxSprite
            .FadeTo(1f, 40, Easing.Out)      // 快速淡入到完全不透明（40ms）
            .Then()
            .FadeTo(0f, length, Easing.Out);  // 在节拍长度内淡出到完全透明
    }
}
```

## 🔑 关键技术点

### 1. BeatSyncedContainer 的使用

`BeatSyncedContainer` 是 osu! 框架提供的一个特殊容器：
- 自动追踪音频的节拍
- 提供 `OnNewBeat` 回调
- 支持提前触发（`EarlyActivationMilliseconds`）
- 自动处理 BPM 变化

### 2. 节拍检测机制

```csharp
// 在 BeatSyncedContainer.Update() 中：
int beatIndex = (int)((currentTrackTime - TimingPoint.Time) / beatLength);

if (beatIndex != lastBeat)
{
    OnNewBeat(beatIndex, TimingPoint, EffectPoint, amplitudes);
}
```

这确保了：
- 每个节拍只触发一次
- 与音频完美同步
- 支持变速段落

### 3. 动画链

```csharp
fxSprite
    .FadeTo(1f, 40, Easing.Out)   // 第一个动画
    .Then()                         // 连接符
    .FadeTo(0f, length, Easing.Out); // 第二个动画
```

`.Then()` 确保第二个动画在第一个完成后开始。

### 4. 自适应节拍长度

```csharp
while (length < 250)
    length *= 2;
```

这防止高 BPM 歌曲（如 300 BPM）闪烁过快：
- 300 BPM → 200ms → 加倍到 400ms
- 240 BPM → 250ms → 保持 250ms
- 120 BPM → 500ms → 保持 500ms

### 5. 皮肤资源加载

```csharp
string fxImageName = $"{bottomImageBaseName}-fx";
fxSprite = skin.GetAnimation(fxImageName, true, true);
```

`GetAnimation` 会自动：
- 查找 `mania-stage-bottom-fx.png`
- 或者 `mania-stage-bottom-fx@2x.png`（高清）
- 或者动画序列 `mania-stage-bottom-fx-0.png`, `-1.png` 等
- 如果都不存在，返回 `null`（不会崩溃）

## 📊 性能考虑

### 优点
- ✅ 使用现有的 BeatSyncedContainer 机制，不需要额外的计时器
- ✅ 动画由 osu! 框架的转换系统处理，高效且平滑
- ✅ 只在有图片资源时才创建和运行
- ✅ 不阻塞主线程

### 优化措施
- 仅在节拍改变时触发动画
- 使用 GPU 加速的透明度变换
- 复用现有的渲染管线

## 🎨 皮肤制作建议

### 推荐的图片内容

1. **发光效果**
   - 白色或彩色外发光
   - 边缘羽化
   - 中心亮度高

2. **粒子效果**
   - 向上飘散的光粒子
   - 星星或火花
   - 能量波纹

3. **高光反射**
   - 金属光泽
   - 玻璃反射
   - 霓虹灯效果

### 技术参数

- **尺寸**：与 stage-bottom 相同或稍大
- **格式**：PNG-24/32（支持透明通道）
- **透明度**：建议 60-80%
- **文件大小**：推荐 < 500KB（优化性能）

### 测试 BPM

建议在不同 BPM 下测试效果：
- 80 BPM（慢歌）
- 120 BPM（标准）
- 180 BPM（快歌）
- 240+ BPM（超高速）

## 🔄 与现有功能的集成

### 兼容性

✅ **完全兼容现有功能：**
- Stage bottom 抖动动画（判定时/长按时）
- Left/Right FX 动画
- 自定义 bottom 图片名称
- 上下滚动方向
- 多列布局（4K-9K）

### 不冲突设计

- BPM 特效独立于抖动系统
- 使用独立的 Drawable 实例
- 不影响现有的 FX 背景
- 可以选择性启用（只需添加/删除图片文件）

## 📝 使用方法

### 对于皮肤制作者

1. 在皮肤文件夹中放置 `mania-stage-bottom-fx@2x.png`
2. 游戏自动检测并启用效果
3. 无需任何配置

### 对于玩家

1. 下载支持此特效的皮肤
2. 选择该皮肤
3. 进入 mania 模式游玩
4. 享受随 BPM 闪动的视觉效果

### 禁用方法

- 删除或重命名 `mania-stage-bottom-fx` 图片文件
- 游戏会自动跳过该特效

## 🐛 故障排查

### 问题清单

| 问题 | 可能原因 | 解决方法 |
|------|---------|---------|
| 特效不显示 | 文件名错误 | 确认文件名为 `mania-stage-bottom-fx@2x.png` |
| 特效不闪烁 | BeatSyncedContainer 未工作 | 检查音乐是否播放 |
| 闪烁频率异常 | Timing 设置错误 | 检查谱面的 BPM 设置 |
| 位置不对 | 锚点设置错误 | 检查代码中的 Anchor/Origin |
| 性能下降 | 图片文件太大 | 优化图片文件大小 |

## 🚀 未来扩展可能性

### 可选改进

1. **配置选项**
   - 允许调整闪烁强度
   - 允许调整触发频率
   - 允许选择是否每拍都闪

2. **更多特效类型**
   - 根据音量自适应强度
   - Kiai 时间特殊效果
   - 连击数触发的特效

3. **颜色调制**
   - 根据难度星级调整颜色
   - 根据准确度调整亮度
   - 彩虹循环效果

## 📚 相关文档

- `BPM_SYNCED_BOTTOM_FX_README.md` - 功能说明和使用指南
- `TESTING_BPM_FX.md` - 测试步骤和调试方法
- `osu.Game/Graphics/Containers/BeatSyncedContainer.cs` - 基类实现
- `osu.Game.Rulesets.Mania/Skinning/Legacy/LegacyStageForeground.cs` - 主要实现

## ✅ 实现完成确认

- [x] 添加必要的命名空间引用
- [x] 创建 BPMSyncedBottomFX 类
- [x] 继承自 BeatSyncedContainer
- [x] 实现 OnNewBeat 回调
- [x] 实现淡入淡出动画（1 → 0）
- [x] 实现节拍长度检测和加倍逻辑
- [x] 支持滚动方向切换
- [x] 与现有特效层正确集成
- [x] 每拍都触发（不过滤节拍）
- [x] 提前 40ms 触发确保同步
- [x] 无编译错误
- [x] 编写文档和测试指南

---

**实现者**: AI Assistant  
**实现日期**: 2025-10-03  
**版本**: osu-masterV1.7.1dev  
**测试状态**: 代码完成，等待实际测试

