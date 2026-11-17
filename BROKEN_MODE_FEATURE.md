# 🔥 Broken Mode - 极限突破机制

## ✨ 功能概述

当玩家在游玩过程中 **Overload 透明度达到 100%** 时，`mania-stage-bottom` 会**永久变成破碎版本** (`mania-stage-bottom-broken`)，作为玩家达到极限表现的视觉奖励！

## 🎮 触发条件

### 前提条件
1. **Combo > 500** （Overload 特效的基本激活条件）
2. **连续击打音符** 使转速持续增加
3. **Overload 透明度达到 100%**

### 触发流程

```
游戏开始
  ↓
Combo 达到 500+
  ↓
Overload 特效激活 (开始响应抖动)
  ↓
持续击打 → 转速增加 → 透明度上升
  ↓
转速达到 8.0 (MAX_SPEED)
  ↓
透明度 = 100% ✨
  ↓
🔥 触发 Broken Mode！
  ↓
mania-stage-bottom 永久替换为 mania-stage-bottom-broken
```

## 📊 转速与透明度对照表

| 转速 | 透明度 | 状态 |
|------|--------|------|
| 0.0 - 1.0 | 0% | 未显示 |
| 1.0 | 0% | 阈值 |
| 2.0 | 14% | 开始可见 |
| 3.0 | 29% | |
| 4.0 | 43% | |
| 5.0 | 57% | |
| 6.0 | 71% | |
| 7.0 | 86% | |
| **8.0** | **100%** | **🔥 触发 Broken Mode** |

## 🎨 所需素材

### 1. mania-stage-bottom（原始素材）
- 默认的舞台底部图片
- 命名格式：`mania-stage-bottom@2x.png` 或按键数版本（如 `mania-stage-bottom\4K\bottom4@2x.png`）

### 2. mania-stage-bottom-broken（破碎素材）⭐
- 触发 Broken Mode 后的替换图片
- **必须添加到皮肤文件夹**
- 命名格式：
  - `mania-stage-bottom-broken@2x.png`
  - 或 `mania-stage-bottom\4K\bottom4-broken@2x.png`
- 建议设计：
  - 可以是破碎效果
  - 可以是强化/升级效果
  - 可以添加火焰、闪电等特效
  - 与原图保持相同尺寸和构图

### 3. mania-stage-bottom-fx（BPM 闪烁特效）
- BPM 同步的闪烁层

### 4. mania-stage-bottom-overload（Overload 特效）
- 性能表现的动态透明度层

## 💡 实现原理

### 代码结构

#### 1. LegacyStageForeground 类

**新增字段：**
```csharp
private Drawable? brokenSprite;              // Broken 素材
private bool isBrokenModeActivated = false;  // 是否已激活
```

**加载逻辑：**
```csharp
[BackgroundDependencyLoader]
private void load(...)
{
    // 加载原始素材
    sprite = skin.GetAnimation(bottomImage, true, true);
    
    // 预先加载 broken 素材（不立即添加）
    string brokenImageName = $"{bottomImage}-broken";
    brokenSprite = skin.GetAnimation(brokenImageName, true, true);
    
    // 创建 Overload 特效并绑定事件
    performanceOverload = new PerformanceBasedOverload(skin, bottomImage);
    performanceOverload.OnMaxOverloadReached += activateBrokenMode;
}
```

**激活方法：**
```csharp
private void activateBrokenMode()
{
    if (isBrokenModeActivated || brokenSprite == null)
        return;
    
    isBrokenModeActivated = true;
    
    // 移除原始素材
    bottomContainer.Remove(sprite, true);
    
    // 添加 broken 素材
    bottomContainer.Add(brokenSprite);
    sprite = brokenSprite;
    
    // 更新锚点
    UpdateAnchorBasedOnDirection();
}
```

#### 2. PerformanceBasedOverload 类

**新增事件：**
```csharp
public event Action? OnMaxOverloadReached;
private bool hasReachedMaxOverload = false;
```

**检测逻辑：**
```csharp
private void updateAlpha()
{
    // ... 计算透明度 ...
    
    double alpha = (currentRotationSpeed - MIN_SPEED) / (MAX_SPEED - MIN_SPEED);
    alpha = Math.Clamp(alpha, 0, 1);
    
    overloadSprite.Alpha = (float)alpha;
    
    // 检测首次达到 100%
    if (!hasReachedMaxOverload && alpha >= 1.0)
    {
        hasReachedMaxOverload = true;
        OnMaxOverloadReached?.Invoke();  // 🔥 触发事件
    }
}
```

## 🧪 测试场景

### 场景 1: 正常触发
```
1. 选择一首高密度谱面
2. 开始游玩
3. 维持 Combo 达到 500+
4. 持续快速击打（或按住长条音符）
5. 观察 Overload 特效亮度逐渐增加
6. 当达到最大亮度时 → 🔥 底部素材变成 broken 版本
```

**预期结果：**
- ✅ Overload 达到 100% 透明度
- ✅ `mania-stage-bottom` 瞬间切换为 `mania-stage-bottom-broken`
- ✅ 本局游戏剩余时间内保持 broken 状态
- ✅ 继续随抖动一起移动和旋转

### 场景 2: 未达到条件
```
1. Combo < 500
   → Overload 不激活
   → 无法触发 Broken Mode
   
2. Combo > 500 但击打密度低
   → Overload 透明度未达到 100%
   → 不触发 Broken Mode
   
3. Miss 导致 Combo 断掉
   → Overload 转速下降
   → 可能无法达到 100%
```

### 场景 3: Retry
```
1. 触发 Broken Mode
2. 按 Retry 重新开始
   → 重置为原始 mania-stage-bottom
   → 需要重新触发
```

## 📈 达到 100% 所需抖动次数

根据参数配置：
- **HIT_SPEED_INCREASE**: 1.0
- **MAX_SPEED**: 8.0
- **初始转速**: 0.0

**理论计算：**
```
需要转速: 8.0
每次增加: 1.0
所需抖动次数: 8.0 / 1.0 = 8 次
```

**实际情况：**
- 考虑到阻力衰减 (0.97 每帧)
- 如果抖动间隔 > 几帧，需要更多次数
- **连续快速击打 (如长条音符)**: 约 8-10 次
- **普通击打 (有间隔)**: 约 12-15 次

### 最快触发方式

**长按音符：**
```
持续按住 Hold Note
→ 每 50ms 一次抖动
→ 约 0.4-0.5 秒内达到 100%
→ 🔥 触发 Broken Mode
```

**密集击打：**
```
快速连续击打
→ 每次击打触发抖动
→ 约 8-15 次击打
→ 🔥 触发 Broken Mode
```

## 🎯 设计建议

### Broken 素材设计理念

**方案 A: 破碎效果**
- 裂纹、碎片效果
- 暗示极限压力下的"破损"
- 色调可以偏暗或偏冷

**方案 B: 强化效果**
- 发光、升级效果
- 暗示"突破极限"的成就
- 色调可以偏亮或加入特效颜色（金色、红色）

**方案 C: 特殊主题**
- 火焰燃烧效果
- 闪电环绕效果
- 能量爆发效果

**通用建议：**
- 与原图保持相同尺寸和中心点
- 可以更加华丽和夸张
- 建议添加动画帧（如果支持）
- 确保在游玩时不影响可读性

## 🔧 参数调整

如果觉得触发难度不合适，可以调整以下参数：

### 降低难度（更容易触发）

```csharp
// 1. 降低 Combo 阈值
private const int MIN_COMBO_THRESHOLD = 300;  // 从 500 改为 300

// 2. 降低最大转速要求
private const double MAX_SPEED = 6.0;  // 从 8.0 改为 6.0

// 3. 增加每次抖动的转速增量
private const double HIT_SPEED_INCREASE = 1.5;  // 从 1.0 改为 1.5
```

### 提高难度（更难触发）

```csharp
// 1. 提高 Combo 阈值
private const int MIN_COMBO_THRESHOLD = 800;  // 从 500 改为 800

// 2. 提高最大转速要求
private const double MAX_SPEED = 10.0;  // 从 8.0 改为 10.0

// 3. 减少每次抖动的转速增量
private const double HIT_SPEED_INCREASE = 0.5;  // 从 1.0 改为 0.5
```

## 📝 特性总结

### ✅ 已实现功能

1. ✅ **预加载 Broken 素材**
   - 在游戏开始时加载
   - 不影响启动性能

2. ✅ **100% 透明度检测**
   - 精确检测首次达到
   - 只触发一次

3. ✅ **素材永久替换**
   - 移除原始素材
   - 添加 broken 素材
   - 保持所有属性（位置、缩放、锚点）

4. ✅ **方向适配**
   - 自动适配滚动方向
   - 锚点自动更新

5. ✅ **本局游戏有效**
   - Retry 后重置
   - 不影响其他谱面

### 🎮 玩家体验

- **视觉反馈明确**：看到 Overload 达到最亮就知道要触发了
- **成就感强**：需要维持高 Combo + 高密度击打
- **不可逆转**：本局触发后永久保持，强化成就感
- **稀有性**：不是每局都能触发，增加特殊性

---

**版本**: v1.0  
**功能**: Broken Mode - 极限突破  
**触发条件**: Combo > 500 + Overload 100%  
**状态**: ✅ 已实现并编译通过

**现在重启游戏，当你达到极限表现时，底部舞台会变成 Broken 版本！** 🔥💥








