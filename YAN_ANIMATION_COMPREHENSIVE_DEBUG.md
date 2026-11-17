# 🔍 Yan 动画全面调试方案

## ❌ 当前问题

**所有尝试的名称都未找到素材：**
```
YanAnimation: 尝试加载素材: bottom4-yan
YanAnimation: 未找到素材: bottom4-yan
YanAnimation: 尝试加载素材: mania-stage-bottom\4K\bottom4-yan
YanAnimation: 未找到素材: mania-stage-bottom\4K\bottom4-yan
YanAnimation: 尝试加载素材: mania-stage-bottom-yan
YanAnimation: 未找到素材: mania-stage-bottom-yan
YanAnimation: 尝试加载素材: bottom4-yan
YanAnimation: 未找到素材: bottom4-yan
YanAnimation: 所有可能的素材名称都未找到
```

## 🧠 全面调试方案

### 1. **多种参数组合尝试**

#### 参数组合矩阵
```csharp
var attempts = new[]
{
    new { name = yanImageName, animatable = false, looping = true, desc = "非动画，循环" },
    new { name = yanImageName, animatable = true, looping = false, desc = "动画，不循环" },
    new { name = yanImageName, animatable = true, looping = true, desc = "动画，循环" },
    new { name = yanImageName, animatable = false, looping = false, desc = "非动画，不循环" }
};
```

#### 预期日志输出
```
YanAnimation: 尝试加载素材: bottom4-yan
YanAnimation: 尝试参数: 非动画，循环
YanAnimation: 未找到素材: bottom4-yan (参数: 非动画，循环)
YanAnimation: 尝试参数: 动画，不循环
YanAnimation: 未找到素材: bottom4-yan (参数: 动画，不循环)
YanAnimation: 尝试参数: 动画，循环
YanAnimation: 未找到素材: bottom4-yan (参数: 动画，循环)
YanAnimation: 尝试参数: 非动画，不循环
YanAnimation: 未找到素材: bottom4-yan (参数: 非动画，不循环)
```

### 2. **皮肤系统工作原理**

#### GetAnimation 方法
```csharp
skin.GetAnimation(componentName, animatable, looping)
```

#### 参数说明
- **componentName**: 素材名称（如 `bottom4-yan`）
- **animatable**: 是否支持动画（true = 寻找帧序列）
- **looping**: 是否循环播放

#### 帧序列查找逻辑
```
当 animatable = true 时：
1. 寻找 componentName-0, componentName-1, componentName-2...
2. 如果找到多个帧，创建动画序列
3. 如果只找到一个帧，返回单帧

当 animatable = false 时：
1. 直接寻找 componentName
2. 不查找帧序列
```

### 3. **实际文件分析**

#### 文件路径
```
C:\Users\Administrator\AppData\Local\Temp\...\mania-stage-bottom\4k\bottom4-yan-0@2x.png
```

#### 文件命名规则
- **基础名称**: `bottom4-yan`
- **帧序列**: `bottom4-yan-0`, `bottom4-yan-1`, `bottom4-yan-2`...
- **文件扩展**: `@2x.png`

#### 皮肤系统查找
```
animatable = true:
- 查找: bottom4-yan-0, bottom4-yan-1, bottom4-yan-2...
- 结果: 应该找到帧序列

animatable = false:
- 查找: bottom4-yan
- 结果: 可能找不到（因为没有 bottom4-yan@2x.png）
```

## 🎯 可能的问题

### 1. **路径问题**
- 皮肤系统可能无法正确处理包含路径的名称
- `mania-stage-bottom\4K\bottom4` 可能不是正确的查找方式

### 2. **大小写问题**
- 实际路径: `4k`（小写）
- 传入路径: `4K`（大写）
- 可能导致路径不匹配

### 3. **皮肤提供者问题**
- 皮肤系统可能没有正确注册该路径的提供者
- 需要检查皮肤配置

### 4. **文件格式问题**
- 文件可能存在但格式不正确
- 皮肤系统可能无法识别该文件

## 🔧 调试步骤

### 步骤 1: 运行游戏查看详细日志
```
YanAnimation: 构造函数，bottomImageBaseName = mania-stage-bottom\4K\bottom4
YanAnimation: 提取的基础名称: bottom4
YanAnimation: 尝试加载素材: bottom4-yan
YanAnimation: 尝试参数: 非动画，循环
YanAnimation: 未找到素材: bottom4-yan (参数: 非动画，循环)
...
```

### 步骤 2: 分析日志输出
- 确认所有参数组合都尝试了
- 确认所有可能的名称都尝试了
- 确认没有成功加载的素材

### 步骤 3: 检查皮肤系统
- 确认皮肤是否正确加载
- 确认文件路径是否正确
- 确认皮肤提供者是否注册

### 步骤 4: 验证文件存在
- 确认 `bottom4-yan-0@2x.png` 文件确实存在
- 确认文件格式正确
- 确认文件权限正确

## 🎯 预期结果

### 成功场景
```
YanAnimation: 尝试加载素材: bottom4-yan
YanAnimation: 尝试参数: 动画，不循环
YanAnimation: 成功加载素材: bottom4-yan (参数: 动画，不循环)
```

### 失败场景
```
YanAnimation: 所有可能的素材名称都未找到
```

## 🔍 进一步调试

### 如果仍然失败，可能需要：

#### 1. **检查皮肤配置**
- 确认皮肤是否正确配置
- 确认路径映射是否正确

#### 2. **检查文件系统**
- 确认文件确实存在
- 确认文件可读
- 确认文件格式正确

#### 3. **检查皮肤提供者**
- 确认皮肤提供者是否正确注册
- 确认路径解析是否正确

#### 4. **尝试直接路径**
- 尝试使用完整的文件路径
- 尝试使用相对路径
- 尝试使用不同的路径格式

## 📝 下一步行动

1. **运行游戏** - 查看新的详细日志
2. **分析输出** - 确认所有尝试都失败了
3. **检查文件** - 确认素材文件确实存在
4. **调整策略** - 根据日志结果调整加载策略

---

**版本**: v1.2  
**功能**: Yan 动画全面调试  
**状态**: ✅ 已实现多种参数组合尝试

**现在可以通过详细的日志来诊断素材加载问题了！** 🔍✨




