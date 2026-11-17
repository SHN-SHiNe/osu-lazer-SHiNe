# 🔧 Bottom 图层层级修复

## ❌ 问题描述

**之前的问题：** `broken` 素材跑到了 `bpm-fx` 之上，层级顺序错误

## ✅ 修复方案

通过设置明确的 `Depth` 值来确保正确的层级顺序。

## 🏗️ 修复后的层级结构

### 📊 层级顺序（从前往后）

```
最上层: mania-stage-bottom-yan (Depth = -1) ⭐
第二层: mania-stage-bottom-overload (Depth = 0)
第三层: mania-stage-bottom-fx (Depth = 1)  
最底层: mania-stage-bottom / broken (Depth = 2)
```

### 🔧 代码修复

#### 1. **原始 sprite（最底层）**
```csharp
sprite = skin.GetAnimation(bottomImage, true, true)?.With(d =>
{
    d.Scale = new Vector2(1.6f);
    d.Anchor = Anchor.BottomCentre;
    d.Origin = Anchor.BottomCentre;
    d.Depth = 2; // 最底层
});
```

#### 2. **Broken sprite（最底层）**
```csharp
brokenSprite = skin.GetAnimation(brokenImageName, true, true)?.With(d =>
{
    d.Scale = new Vector2(1.6f);
    d.Anchor = Anchor.BottomCentre;
    d.Origin = Anchor.BottomCentre;
    d.Depth = 2; // 最底层（与原始 sprite 同层级）
});
```

#### 3. **BPM 同步特效（第三层）**
```csharp
public BPMSyncedBottomFX(ISkinSource skin, string bottomImageBaseName)
{
    // ...
    Depth = 1; // 中间层（在 bottom 之上，overload 之下）
}
```

#### 4. **Overload 特效（第二层）**
```csharp
public PerformanceBasedOverload(ISkinSource skin, string bottomImageBaseName)
{
    // ...
    Depth = 0; // 第二层（在 BPM FX 之上，Yan 之下）
}
```

#### 5. **Yan 动画（最上层）**
```csharp
public YanAnimation(ISkinSource skin, string bottomImageBaseName)
{
    // ...
    Depth = -1; // 最上层（在所有元素之上）
}
```

## 🎯 层级说明

### Depth 值规则
- **Depth 值越小，层级越高**
- `Depth = -1` 是最上层
- `Depth = 2` 是最底层

### 层级关系
```
Depth = -1: Yan 动画（最前）
Depth = 0:  Overload 特效
Depth = 1:  BPM 闪烁
Depth = 2:  基础舞台 / Broken 舞台（最后）
```

## 🎮 实际显示效果

### 正常模式
```
屏幕显示顺序（从前往后）：
┌─────────────────────────┐
│  🎆 Yan 动画 (最前)      │ ← Depth = -1
├─────────────────────────┤
│  ⚡ Overload 特效        │ ← Depth = 0
├─────────────────────────┤
│  💫 BPM 同步闪烁         │ ← Depth = 1
├─────────────────────────┤
│  🏗️ Stage Bottom        │ ← Depth = 2
└─────────────────────────┘
```

### Broken 模式
```
屏幕显示顺序（从前往后）：
┌─────────────────────────┐
│  🎆 Yan 动画 (最前)      │ ← Depth = -1
├─────────────────────────┤
│  ⚡ Overload 特效        │ ← Depth = 0
├─────────────────────────┤
│  💫 BPM 同步闪烁         │ ← Depth = 1
├─────────────────────────┤
│  🔥 Broken Stage        │ ← Depth = 2
└─────────────────────────┘
```

## 🔄 Broken 模式激活流程

### 激活前
```
容器内容：
1. sprite (mania-stage-bottom) - Depth = 2
2. bpmSyncedFX - Depth = 1
3. performanceOverload - Depth = 0
4. yanAnimation - Depth = -1
```

### 激活后
```
容器内容：
1. brokenSprite (mania-stage-bottom-broken) - Depth = 2
2. bpmSyncedFX - Depth = 1
3. performanceOverload - Depth = 0
4. yanAnimation - Depth = -1
```

### 关键代码
```csharp
private void activateBrokenMode()
{
    // 从容器中移除原始 sprite
    if (sprite != null)
    {
        bottomContainer.Remove(sprite, true);
        sprite = null;
    }

    // 添加 brokenSprite（Depth 值确保它在最底层）
    bottomContainer.Add(brokenSprite);
    sprite = brokenSprite;
}
```

## 💡 修复要点

### 1. **明确的 Depth 值**
- ✅ 所有图层都有明确的 Depth 值
- ✅ 不再依赖添加顺序
- ✅ 确保层级关系稳定

### 2. **Broken 模式正确处理**
- ✅ `brokenSprite` 与原始 `sprite` 使用相同的 Depth = 2
- ✅ 替换后层级关系保持不变
- ✅ 不会影响其他特效的显示

### 3. **层级隔离**
- ✅ 每个特效都有独立的 Depth 值
- ✅ 不会相互干扰
- ✅ 可以独立控制显示/隐藏

## 🎯 测试场景

### 场景 1: 正常游玩
```
显示顺序：Yan > Overload > BPM FX > Stage Bottom
层级正确：✅
```

### 场景 2: Broken 模式激活
```
显示顺序：Yan > Overload > BPM FX > Broken Stage
层级正确：✅
```

### 场景 3: 特效叠加
```
同时显示：BPM 闪烁 + Overload 特效
层级正确：Overload 在 BPM FX 之上 ✅
```

### 场景 4: 完整特效
```
同时显示：Yan 动画 + Overload + BPM FX + Broken Stage
层级正确：Yan > Overload > BPM FX > Broken ✅
```

## 📈 修复前后对比

### 修复前
```
问题：broken 素材跑到 bpm-fx 之上
原因：没有设置明确的 Depth 值
结果：层级混乱，视觉效果错误
```

### 修复后
```
解决：所有图层都有明确的 Depth 值
结果：层级正确，视觉效果完美
```

## 🎉 总结

### 核心修复
**"通过设置明确的 Depth 值确保正确的图层层级"**

### 技术要点
- ✅ **Depth 值控制**: 每个图层都有明确的 Depth 值
- ✅ **层级稳定**: 不依赖添加顺序，层级关系稳定
- ✅ **Broken 模式**: 正确替换底层素材，不影响其他特效
- ✅ **特效叠加**: 多个特效可以正确叠加显示

### 视觉效果
- 🎆 **Yan 动画**: 始终在最上层，不会被遮挡
- ⚡ **Overload 特效**: 在 BPM FX 之上，优先级正确
- 💫 **BPM 闪烁**: 在舞台之上，节拍同步效果清晰
- 🏗️ **舞台素材**: 在最底层，作为背景基础

---

**版本**: v1.2  
**功能**: Bottom 图层层级修复  
**状态**: ✅ 已修复并编译通过

**现在所有图层的层级顺序都正确了！** 🎯✨




