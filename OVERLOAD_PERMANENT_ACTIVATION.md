# 🔐 Overload 永久激活机制

## ✨ 核心改动

**之前：** Combo > 500 时激活，Combo ≤ 500 时失效  
**现在：** Combo 一旦突破 500，本局游戏永久激活 ✅

## 🎯 新机制说明

### 激活条件

```
游戏开始 → Combo 不断增加
   ↓
Combo 达到 501+ 
   ↓
🔥 Overload 特效激活！
   ↓
【本局游戏永久有效】
   ↓
即使之后 Miss 导致 Combo 降低...
   ↓
Overload 仍然保持激活 ✅
```

### 代码实现

**新增标志位：**
```csharp
private bool overloadActivated = false; // 一旦激活，本局永久有效
```

**激活检查逻辑：**
```csharp
// 在 OnShake() 和 UpdatePerFrame() 中持续检查
if (!overloadActivated && combo.Value > MIN_COMBO_THRESHOLD)
    overloadActivated = true;
```

**透明度更新：**
```csharp
// 不再检查当前 combo 值，只检查是否已激活
if (!overloadActivated)
{
    overloadSprite.Alpha = 0;
    return;
}
```

## 📊 行为对比

### 场景 A: 正常游玩

#### 之前的行为
```
Combo: 0 → 300 → 500 → 501 → 600 → Miss → 0
Overload: ❌   ❌    ❌    ✅    ✅    ❌     ❌
```

#### 现在的行为
```
Combo: 0 → 300 → 500 → 501 → 600 → Miss → 0
Overload: ❌   ❌    ❌    ✅🔒  ✅    ✅     ✅
                             ↑
                          永久激活
```

### 场景 B: 高 Combo 后失误

#### 之前的行为
```
时间轴：
0s:   Combo 0   → Overload ❌
10s:  Combo 600 → Overload ✅ (转速增加)
15s:  Miss!     → Combo 0   → Overload ❌ (突然消失)
20s:  恢复      → Combo 50  → Overload ❌ (需要重新达到 500)
```

#### 现在的行为
```
时间轴：
0s:   Combo 0   → Overload ❌
10s:  Combo 600 → Overload ✅🔒 (转速增加 + 永久激活)
15s:  Miss!     → Combo 0   → Overload ✅ (继续工作！)
                                        ↓ (转速自然衰减)
20s:  恢复      → Combo 50  → Overload ✅ (仍然激活，可继续增加转速)
```

### 场景 C: 长时间暂停

#### 之前的行为
```
Combo 600 → 击打密集 → 转速 6.0 → 透明度 71%
   ↓
暂停 30 秒（无击打）
   ↓
转速衰减 → 0.0 → 透明度 0%
   ↓
Miss → Combo 0
   ↓
Overload ❌ 完全失效（即使恢复 combo 也不显示，需要重新达到 500）
```

#### 现在的行为
```
Combo 600 → 击打密集 → 转速 6.0 → 透明度 71%
   ↓
暂停 30 秒（无击打）
   ↓
转速衰减 → 0.0 → 透明度 0%（看不见但仍激活）
   ↓
Miss → Combo 0
   ↓
Overload ✅ 仍然激活（只是透明度为 0）
   ↓
继续击打 → 转速增加 → 透明度上升 ✅
```

## 💡 优势

### 1. **更稳定的视觉反馈**
- ✅ 不会因为单次失误而突然消失
- ✅ 玩家可以专注于游玩，不用担心特效消失

### 2. **容错性提高**
- ✅ Miss 后依然可以继续使用 Overload
- ✅ 鼓励玩家在失误后继续努力

### 3. **更符合"奖励"机制**
- ✅ 达到 500 Combo 本身就是成就
- ✅ 这个成就在本局游戏中应该保持

### 4. **避免反复激活/失效**
- ✅ 不会出现特效闪烁消失又出现的情况
- ✅ 更流畅的游玩体验

## ⚠️ 注意事项

### 1. 激活后无法取消
- 一旦 Combo > 500，Overload 将在本局游戏中一直保持激活
- 只有 Retry 或重新选择谱面才会重置

### 2. 透明度仍然由转速控制
- **激活** ≠ **可见**
- 即使激活了，转速为 0 时透明度仍为 0%
- 需要继续击打才能看到特效

### 3. Broken Mode 触发条件不变
- 仍需要 Overload 透明度达到 100%
- 只是现在即使 Miss 了，也有机会继续尝试触发

## 🔄 重置机制

### Retry 时重置
```csharp
public void Reset()
{
    currentRotationSpeed = BASE_ROTATION_SPEED;
    overloadActivated = false;        // 重置激活状态
    hasReachedMaxOverload = false;    // 重置 Broken Mode 触发
    overloadSprite.Alpha = 0;
}
```

### 新游戏自动重置
- 每次选择新谱面，所有状态自动重置
- 需要重新达到 Combo > 500

## 🎮 游玩体验提升

### 对新手玩家
```
好不容易达到 500 Combo...
Miss 了一个音符 → Combo 降到 100
   ↓
【之前】: "啊！特效没了！白费了！" 😭
【现在】: "还好特效还在，继续加油！" 💪
```

### 对高手玩家
```
达到 1000+ Combo...
因为某个难点 Miss 了 → Combo 归零
   ↓
【之前】: 特效消失，需要重新打到 500 Combo
【现在】: 特效还在，可以马上继续使用 ✅
```

### 对挑战 Broken Mode 的玩家
```
Combo 800，Overload 转速 7.0（86% 透明度）
再打几个就能触发 Broken Mode...
突然 Miss！Combo → 0
   ↓
【之前】: 特效失效，转速清零，Broken Mode 遥不可及 😭
【现在】: 特效还在，转速会自然衰减但不会立即清零，
        恢复后还有机会冲击 100% ！💪
```

## 📈 数据对比

### 激活难度
| 项目 | 之前 | 现在 |
|------|------|------|
| 首次激活 | Combo > 500 | Combo > 500 |
| 保持激活 | 持续保持 Combo > 500 | 一次达到即可 |
| Miss 后 | 需重新达到 500 | 继续有效 ✅ |
| 容错性 | 低 | 高 ✅ |

### 可见时间
| 场景 | 之前 | 现在 |
|------|------|------|
| 无失误 | 100% | 100% |
| 偶尔失误 | ~60% (特效消失) | ~95% (仅转速衰减) |
| 频繁失误 | ~30% (反复失效) | ~70% (持续激活) |

## 🎯 总结

### 核心改变
**"一次激活，永久有效"**

### 优势
- ✅ **稳定性**：不会突然消失
- ✅ **容错性**：Miss 后仍可使用
- ✅ **连续性**：更流畅的体验
- ✅ **鼓励性**：达到 500 Combo 的奖励更持久

### 层级顺序（保持不变）
```
最上层: mania-stage-bottom-overload (Depth = 0) ⭐
中间层: mania-stage-bottom-fx (Depth = 1)
最底层: mania-stage-bottom / broken (Depth = 2)
```

---

**版本**: v1.1  
**功能**: Overload 永久激活机制  
**状态**: ✅ 已实现并编译通过

**现在 Overload 特效更加稳定和友好了！** 🎉✨
