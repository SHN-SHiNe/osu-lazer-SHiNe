# 🎆 Yan 动画特效功能

## ✨ 功能概述

当 `mania-stage-bottom-overload` 透明度成功突破 100% 时，自动播放 `mania-stage-bottom-yan@2x.png` 的帧动画序列，作为额外的视觉奖励效果。

## 🎯 核心特性

### 1. **触发条件**
- ✅ Overload 透明度达到 100%
- ✅ 每次达到 100% 都会触发一次动画
- ✅ 支持多次触发（每次都是独立的动画播放）

### 2. **层级顺序**
```
最上层: mania-stage-bottom-yan (Depth = -1) ⭐
第二层: mania-stage-bottom-overload (Depth = 0)
第三层: mania-stage-bottom-fx (Depth = 1)
最底层: mania-stage-bottom / broken (Depth = 2)
```

### 3. **动画特性**
- ✅ 60fps 帧率播放
- ✅ 1秒淡出效果
- ✅ 跟随舞台抖动
- ✅ 支持滚动方向切换

## 🏗️ 技术实现

### YanAnimation 类

```csharp
public partial class YanAnimation : Container
{
    private readonly ISkinSource skin;
    private readonly string bottomImageBaseName;
    private Drawable? yanSprite;

    public YanAnimation(ISkinSource skin, string bottomImageBaseName)
    {
        this.skin = skin;
        this.bottomImageBaseName = bottomImageBaseName;
        RelativeSizeAxes = Axes.Both;
        Depth = -1; // 最上层
    }
}
```

### 素材加载

```csharp
[BackgroundDependencyLoader]
private void load()
{
    // 尝试加载 yan 动画素材
    string yanImageName = $"{bottomImageBaseName}-yan";
    yanSprite = skin.GetAnimation(yanImageName, true, true)?.With(d =>
    {
        d.Scale = new Vector2(1.6f);
        d.Anchor = Anchor.BottomCentre;
        d.Origin = Anchor.BottomCentre;
    });
}
```

### 动画播放

```csharp
public void PlayAnimation()
{
    if (yanSprite == null)
        return;

    Logger.Log("YanAnimation: 播放 Yan 动画");

    // 立即显示并播放动画
    yanSprite.Alpha = 1.0f;
    yanSprite.FadeOut(1000, Easing.Out); // 1秒后淡出
}
```

## 🔗 事件绑定

### 在 LegacyStageForeground 中集成

```csharp
// 创建 Yan 动画特效
yanAnimation = new YanAnimation(skin, bottomImage);
performanceOverload.OnMaxOverloadReached += yanAnimation.PlayAnimation;
```

### 添加到容器

```csharp
bottomContainer.Add(bpmSyncedFX);        // BPM 闪烁层
bottomContainer.Add(performanceOverload); // Overload 层
bottomContainer.Add(yanAnimation);        // Yan 动画层（最上）
```

## 📊 工作流程

### 完整触发流程

```
1. 玩家击打音符 → 舞台抖动
   ↓
2. Overload 转速增加 → 透明度上升
   ↓
3. 透明度达到 100% → 触发 OnMaxOverloadReached 事件
   ↓
4. 同时触发两个效果：
   ├── activateBrokenMode() → 替换为 broken 素材
   └── yanAnimation.PlayAnimation() → 播放 Yan 动画
   ↓
5. Yan 动画显示 1 秒后淡出
```

### 多次触发支持

```
第一次达到 100%:
Overload: 100% → Yan 动画播放 → 1秒后淡出

第二次达到 100%:
Overload: 100% → Yan 动画再次播放 → 1秒后淡出

第三次达到 100%:
Overload: 100% → Yan 动画第三次播放 → 1秒后淡出
```

## 🎮 视觉效果

### 层级叠加效果

```
屏幕显示顺序（从前往后）：
┌─────────────────────────┐
│  🎆 Yan 动画 (最前)      │ ← 100% 透明度时播放
├─────────────────────────┤
│  ⚡ Overload 特效        │ ← 基于转速的透明度
├─────────────────────────┤
│  💫 BPM 同步闪烁         │ ← 节拍同步闪烁
├─────────────────────────┤
│  🏗️ Stage Bottom        │ ← 基础舞台素材
└─────────────────────────┘
```

### 动画时序

```
时间轴: 0ms → 1000ms → 2000ms
Yan:    显示 → 淡出中 → 完全隐藏
Alpha:  1.0  → 0.5    → 0.0
```

## 🔧 配置参数

### 动画参数

| 参数 | 值 | 说明 |
|------|-----|------|
| 播放时长 | 1000ms | 动画显示时间 |
| 淡出效果 | Easing.Out | 平滑淡出 |
| 缩放比例 | 1.6f | 与舞台素材一致 |
| 帧率 | 60fps | 由皮肤素材决定 |

### 层级深度

| 元素 | Depth | 说明 |
|------|-------|------|
| Yan 动画 | -1 | 最上层 |
| Overload | 0 | 第二层 |
| BPM FX | 1 | 第三层 |
| Stage Bottom | 2 | 最底层 |

## 🎯 使用场景

### 场景 1: 首次达到 100%

```
Combo 800 → 密集击打 → Overload 转速 8.0
   ↓
透明度达到 100% → 触发事件
   ↓
🎆 Yan 动画播放 + 🏗️ 舞台变为 Broken 模式
   ↓
1秒后 Yan 动画淡出，但 Broken 模式保持
```

### 场景 2: 多次达到 100%

```
第一次: Combo 600 → 100% → Yan 动画播放
   ↓
转速衰减 → 透明度降低
   ↓
第二次: 继续击打 → 100% → Yan 动画再次播放
   ↓
转速衰减 → 透明度降低
   ↓
第三次: 继续击打 → 100% → Yan 动画第三次播放
```

### 场景 3: 滚动方向切换

```
向下滚动: Yan 动画锚点在底部
   ↓
切换到向上滚动
   ↓
Yan 动画锚点自动切换到顶部
   ↓
继续正常播放
```

## 🛠️ 调试功能

### 日志输出

```csharp
Logger.Log("YanAnimation: 播放 Yan 动画");
Logger.Log($"YanAnimation: 未找到 {yanImageName} 素材");
```

### 素材检查

- ✅ 自动检测 `mania-stage-bottom-yan@2x.png` 是否存在
- ✅ 如果不存在，记录日志但不影响其他功能
- ✅ 支持帧动画序列（多帧图片）

## 📁 文件结构

### 皮肤素材要求

```
皮肤文件夹/
├── mania-stage-bottom@2x.png          ← 基础舞台素材
├── mania-stage-bottom-fx@2x.png       ← BPM 闪烁素材
├── mania-stage-bottom-overload@2x.png ← Overload 素材
├── mania-stage-bottom-broken@2x.png   ← Broken 模式素材
└── mania-stage-bottom-yan@2x.png      ← Yan 动画素材 ⭐
```

### 代码文件

```
osu.Game.Rulesets.Mania/Skinning/Legacy/
└── LegacyStageForeground.cs ← 包含所有特效实现
    ├── BPMSyncedBottomFX
    ├── PerformanceBasedOverload
    └── YanAnimation ⭐
```

## 🎉 优势特性

### 1. **视觉奖励**
- ✅ 达到 100% 透明度的额外奖励
- ✅ 炫酷的动画效果
- ✅ 增强成就感

### 2. **非侵入性**
- ✅ 不影响现有功能
- ✅ 自动管理生命周期
- ✅ 优雅的淡出效果

### 3. **性能优化**
- ✅ 按需播放，不持续占用资源
- ✅ 自动清理动画状态
- ✅ 支持多次触发

### 4. **兼容性**
- ✅ 与 Broken Mode 完美配合
- ✅ 支持所有滚动方向
- ✅ 跟随舞台抖动

## ⚠️ 注意事项

### 1. 素材要求
- 需要提供 `mania-stage-bottom-yan@2x.png` 素材
- 支持帧动画序列（多帧图片）
- 建议使用透明背景

### 2. 性能考虑
- 动画播放时会短暂增加渲染负担
- 1秒后自动淡出，不会持续占用资源
- 支持多次触发，每次都是独立播放

### 3. 层级管理
- Yan 动画始终在最上层
- 不会遮挡游戏界面
- 与 Overload 特效完美配合

## 🔄 重置机制

### Retry 时重置

```csharp
public void Reset()
{
    if (yanSprite != null)
    {
        yanSprite.Alpha = 0;
        yanSprite.ClearTransforms();
    }
}
```

### 新游戏自动重置
- 每次选择新谱面，所有状态自动重置
- 需要重新达到 Overload 100% 才能触发

## 📈 总结

### 核心功能
**"Overload 100% 透明度时的视觉奖励动画"**

### 技术特点
- ✅ **事件驱动**: 基于 OnMaxOverloadReached 事件
- ✅ **层级管理**: 最上层显示，不遮挡游戏
- ✅ **自动播放**: 60fps 帧率，1秒淡出
- ✅ **多次触发**: 每次达到 100% 都播放
- ✅ **方向适配**: 支持滚动方向切换

### 用户体验
- 🎆 **视觉冲击**: 达到 100% 时的炫酷动画
- 🏆 **成就感**: 额外的视觉奖励
- 🎮 **流畅性**: 不影响游戏操作
- 🔄 **重复性**: 可以多次触发

---

**版本**: v1.0  
**功能**: Yan 动画特效  
**状态**: ✅ 已实现并编译通过

**现在当 Overload 达到 100% 透明度时，会有炫酷的 Yan 动画作为奖励！** 🎆✨





