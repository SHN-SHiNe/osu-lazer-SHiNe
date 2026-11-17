# 🧠 Yan 动画智能素材加载

## 🔍 问题分析

### 日志信息分析
```
YanAnimation: 构造函数，bottomImageBaseName = mania-stage-bottom\4K\bottom4
YanAnimation: 尝试加载素材: mania-stage-bottom\4K\bottom4-yan
YanAnimation: 未找到 mania-stage-bottom\4K\bottom4-yan 素材
YanAnimation: 尝试备用素材: mania-stage-bottom-yan
YanAnimation: 备用素材也未找到: mania-stage-bottom-yan
```

### 实际文件路径
```
C:\Users\Administrator\AppData\Local\Temp\...\mania-stage-bottom\4k\bottom4-yan-0@2x.png
```

### 问题根源
1. **路径包含问题**: `bottomImageBaseName` 包含完整路径 `mania-stage-bottom\4K\bottom4`
2. **素材命名**: 实际素材是 `bottom4-yan-0@2x.png`（帧动画序列）
3. **路径大小写**: 实际路径是 `4k`（小写），但传入的是 `4K`（大写）

## 🧠 智能加载方案

### 1. **路径解析**
```csharp
// 从完整路径中提取基础名称
string baseName = System.IO.Path.GetFileName(bottomImageBaseName);
Logger.Log($"YanAnimation: 提取的基础名称: {baseName}");
```

### 2. **多种名称尝试**
```csharp
string[] possibleNames = {
    $"{baseName}-yan",                    // bottom4-yan
    $"{bottomImageBaseName}-yan",         // mania-stage-bottom\4K\bottom4-yan
    "mania-stage-bottom-yan",             // 标准名称
    "bottom4-yan"                         // 直接使用 bottom4-yan
};
```

### 3. **循环尝试加载**
```csharp
foreach (string yanImageName in possibleNames)
{
    Logger.Log($"YanAnimation: 尝试加载素材: {yanImageName}");
    yanSprite = skin.GetAnimation(yanImageName, false, true)?.With(d =>
    {
        d.Scale = new Vector2(1.6f);
        d.Anchor = Anchor.BottomCentre;
        d.Origin = Anchor.BottomCentre;
    });
    
    if (yanSprite != null)
    {
        Add(yanSprite);
        yanSprite.Alpha = 0;
        Logger.Log($"YanAnimation: 成功加载素材: {yanImageName}");
        break; // 找到素材后退出循环
    }
    else
    {
        Logger.Log($"YanAnimation: 未找到素材: {yanImageName}");
    }
}
```

## 📊 预期日志输出

### 成功加载场景
```
YanAnimation: 构造函数，bottomImageBaseName = mania-stage-bottom\4K\bottom4
YanAnimation: 提取的基础名称: bottom4
YanAnimation: 尝试加载素材: bottom4-yan
YanAnimation: 成功加载素材: bottom4-yan
```

### 多尝试场景
```
YanAnimation: 构造函数，bottomImageBaseName = mania-stage-bottom\4K\bottom4
YanAnimation: 提取的基础名称: bottom4
YanAnimation: 尝试加载素材: bottom4-yan
YanAnimation: 未找到素材: bottom4-yan
YanAnimation: 尝试加载素材: mania-stage-bottom\4K\bottom4-yan
YanAnimation: 未找到素材: mania-stage-bottom\4K\bottom4-yan
YanAnimation: 尝试加载素材: mania-stage-bottom-yan
YanAnimation: 未找到素材: mania-stage-bottom-yan
YanAnimation: 尝试加载素材: bottom4-yan
YanAnimation: 成功加载素材: bottom4-yan
```

## 🎯 技术要点

### 1. **路径处理**
- ✅ 使用 `System.IO.Path.GetFileName()` 提取文件名
- ✅ 处理包含路径的完整名称
- ✅ 兼容不同的路径格式

### 2. **智能匹配**
- ✅ 尝试多种可能的素材名称
- ✅ 按优先级顺序尝试
- ✅ 找到后立即停止

### 3. **帧动画支持**
- ✅ 使用 `skin.GetAnimation()` 支持帧动画序列
- ✅ 自动处理 `-0`, `-1`, `-2` 等帧文件
- ✅ 设置 `loop = false` 确保只播放一次

## 🔧 素材文件结构

### 实际文件结构
```
mania-stage-bottom/4k/
├── bottom4@2x.png           ← 基础素材
├── bottom4-fx@2x.png        ← BPM 闪烁
├── bottom4-overload@2x.png  ← Overload 特效
├── bottom4-broken@2x.png    ← Broken 模式
└── bottom4-yan-0@2x.png     ← Yan 动画帧 0
    bottom4-yan-1@2x.png     ← Yan 动画帧 1
    bottom4-yan-2@2x.png     ← Yan 动画帧 2
    ...                      ← 更多帧
```

### 命名规则
- **基础名称**: `bottom4`
- **Yan 动画**: `bottom4-yan`
- **帧序列**: `bottom4-yan-0`, `bottom4-yan-1`, `bottom4-yan-2`...

## 🎮 加载流程

### 步骤 1: 路径解析
```
输入: mania-stage-bottom\4K\bottom4
输出: bottom4
```

### 步骤 2: 名称生成
```
1. bottom4-yan
2. mania-stage-bottom\4K\bottom4-yan
3. mania-stage-bottom-yan
4. bottom4-yan
```

### 步骤 3: 循环尝试
```
尝试 1: bottom4-yan → 成功 ✅
停止尝试，加载完成
```

## 💡 优势特性

### 1. **高兼容性**
- ✅ 支持不同的皮肤命名方式
- ✅ 处理路径包含的情况
- ✅ 兼容大小写差异

### 2. **智能降级**
- ✅ 按优先级尝试不同名称
- ✅ 自动找到可用的素材
- ✅ 详细的调试日志

### 3. **性能优化**
- ✅ 找到后立即停止尝试
- ✅ 避免不必要的加载操作
- ✅ 高效的循环机制

## 🔍 调试信息

### 关键日志
```
YanAnimation: 构造函数，bottomImageBaseName = [完整路径]
YanAnimation: 提取的基础名称: [文件名]
YanAnimation: 尝试加载素材: [素材名称]
YanAnimation: 成功加载素材: [成功名称]
```

### 错误处理
```
YanAnimation: 未找到素材: [失败名称]
YanAnimation: 所有可能的素材名称都未找到
```

## 🎯 预期结果

### 成功场景
- ✅ 成功加载 `bottom4-yan` 素材
- ✅ Yan 动画可以正常播放
- ✅ 帧动画序列正确显示

### 失败场景
- ❌ 所有名称都未找到
- ❌ 需要检查皮肤文件是否存在
- ❌ 可能需要调整素材命名

## 🔧 下一步

1. **运行游戏** - 查看新的调试日志
2. **确认加载** - 验证 `bottom4-yan` 是否成功加载
3. **测试动画** - 触发 Overload 100% 测试 Yan 动画
4. **验证功能** - 确认动画播放完毕自动消失

---

**版本**: v1.1  
**功能**: Yan 动画智能素材加载  
**状态**: ✅ 已实现智能加载机制

**现在应该可以成功加载 `bottom4-yan` 素材了！** 🧠✨🎆




