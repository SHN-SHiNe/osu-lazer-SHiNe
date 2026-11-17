# 🔍 Yan 动画素材加载调试

## ❌ 问题描述

**错误信息：** `YanAnimation: 未找到 mania-stage-bottom\4K\bottom4-yan 素材`

**问题分析：** 素材名称拼接错误，实际皮肤中的素材名称是 `bottom4`，但拼接后变成了 `bottom4-yan`

## 🔧 调试方案

### 1. **添加调试日志**

#### 构造函数调试
```csharp
public YanAnimation(ISkinSource skin, string bottomImageBaseName)
{
    this.skin = skin;
    this.bottomImageBaseName = bottomImageBaseName;
    Logger.Log($"YanAnimation: 构造函数，bottomImageBaseName = {bottomImageBaseName}");
    // ...
}
```

#### 素材加载调试
```csharp
[BackgroundDependencyLoader]
private void load()
{
    string yanImageName = $"{bottomImageBaseName}-yan";
    Logger.Log($"YanAnimation: 尝试加载素材: {yanImageName}");
    // ...
}
```

### 2. **备用素材名称**

#### 主素材加载失败时的备用方案
```csharp
if (yanSprite != null)
{
    // 成功加载主素材
    Logger.Log($"YanAnimation: 成功加载素材: {yanImageName}");
}
else
{
    Logger.Log($"YanAnimation: 未找到 {yanImageName} 素材");
    
    // 尝试备用素材名称
    string fallbackYanImageName = "mania-stage-bottom-yan";
    Logger.Log($"YanAnimation: 尝试备用素材: {fallbackYanImageName}");
    yanSprite = skin.GetAnimation(fallbackYanImageName, false, true)?.With(d =>
    {
        d.Scale = new Vector2(1.6f);
        d.Anchor = Anchor.BottomCentre;
        d.Origin = Anchor.BottomCentre;
    });
    
    if (yanSprite != null)
    {
        Logger.Log($"YanAnimation: 成功加载备用素材: {fallbackYanImageName}");
    }
    else
    {
        Logger.Log($"YanAnimation: 备用素材也未找到: {fallbackYanImageName}");
    }
}
```

## 📊 素材名称分析

### 当前情况
```
皮肤配置: bottomImage = "bottom4"
拼接结果: bottomImageBaseName + "-yan" = "bottom4-yan"
实际需要: "mania-stage-bottom-yan"
```

### 可能的原因

#### 1. **皮肤配置问题**
- 皮肤中的 `BottomStageImage` 配置为 `"bottom4"`
- 但 Yan 动画素材名称应该是 `"mania-stage-bottom-yan"`

#### 2. **素材命名不一致**
- 基础素材: `bottom4@2x.png`
- Yan 素材: `mania-stage-bottom-yan@2x.png`
- 命名规则不统一

#### 3. **皮肤文件夹结构**
```
皮肤文件夹/
├── bottom4@2x.png                    ← 基础素材
├── bottom4-fx@2x.png                 ← BPM 闪烁素材
├── bottom4-overload@2x.png           ← Overload 素材
├── bottom4-broken@2x.png             ← Broken 素材
└── mania-stage-bottom-yan@2x.png     ← Yan 动画素材（命名不一致）
```

## 🎯 解决方案

### 方案 1: 统一素材命名（推荐）

#### 重命名 Yan 素材
```
将: mania-stage-bottom-yan@2x.png
改为: bottom4-yan@2x.png
```

#### 优势
- ✅ 命名规则统一
- ✅ 自动拼接正确
- ✅ 不需要修改代码

### 方案 2: 使用备用素材名称（已实现）

#### 代码逻辑
```csharp
// 首先尝试拼接名称
string yanImageName = $"{bottomImageBaseName}-yan";

// 如果失败，尝试标准名称
string fallbackYanImageName = "mania-stage-bottom-yan";
```

#### 优势
- ✅ 兼容多种命名方式
- ✅ 自动降级处理
- ✅ 不需要修改皮肤文件

### 方案 3: 智能素材名称检测

#### 检测逻辑
```csharp
// 尝试多种可能的名称
string[] possibleNames = {
    $"{bottomImageBaseName}-yan",
    "mania-stage-bottom-yan",
    "bottom-yan",
    "stage-bottom-yan"
};

foreach (string name in possibleNames)
{
    yanSprite = skin.GetAnimation(name, false, true);
    if (yanSprite != null)
    {
        Logger.Log($"YanAnimation: 找到素材: {name}");
        break;
    }
}
```

## 🔍 调试步骤

### 1. **运行游戏并查看日志**
```
YanAnimation: 构造函数，bottomImageBaseName = bottom4
YanAnimation: 尝试加载素材: bottom4-yan
YanAnimation: 未找到 bottom4-yan 素材
YanAnimation: 尝试备用素材: mania-stage-bottom-yan
YanAnimation: 成功加载备用素材: mania-stage-bottom-yan
```

### 2. **确认素材文件存在**
检查皮肤文件夹中是否有以下文件：
- `bottom4@2x.png`
- `bottom4-fx@2x.png`
- `bottom4-overload@2x.png`
- `bottom4-broken@2x.png`
- `mania-stage-bottom-yan@2x.png` 或 `bottom4-yan@2x.png`

### 3. **验证素材加载**
如果备用素材加载成功，说明问题已解决。

## 📝 日志输出示例

### 成功加载主素材
```
YanAnimation: 构造函数，bottomImageBaseName = bottom4
YanAnimation: 尝试加载素材: bottom4-yan
YanAnimation: 成功加载素材: bottom4-yan
```

### 成功加载备用素材
```
YanAnimation: 构造函数，bottomImageBaseName = bottom4
YanAnimation: 尝试加载素材: bottom4-yan
YanAnimation: 未找到 bottom4-yan 素材
YanAnimation: 尝试备用素材: mania-stage-bottom-yan
YanAnimation: 成功加载备用素材: mania-stage-bottom-yan
```

### 完全加载失败
```
YanAnimation: 构造函数，bottomImageBaseName = bottom4
YanAnimation: 尝试加载素材: bottom4-yan
YanAnimation: 未找到 bottom4-yan 素材
YanAnimation: 尝试备用素材: mania-stage-bottom-yan
YanAnimation: 备用素材也未找到: mania-stage-bottom-yan
```

## 🎯 预期结果

### 如果备用素材存在
- ✅ Yan 动画可以正常播放
- ✅ 日志显示成功加载备用素材
- ✅ 功能完全正常

### 如果备用素材不存在
- ❌ Yan 动画无法播放
- ❌ 日志显示所有素材都未找到
- ❌ 需要检查皮肤文件

## 🔧 下一步行动

1. **运行游戏** - 查看调试日志输出
2. **检查日志** - 确认素材加载状态
3. **验证功能** - 测试 Yan 动画是否正常播放
4. **调整素材** - 根据日志结果决定是否需要重命名素材文件

---

**版本**: v1.0  
**功能**: Yan 动画素材加载调试  
**状态**: ✅ 已实现调试功能和备用素材加载

**现在可以通过日志清楚地看到素材加载过程了！** 🔍✨




