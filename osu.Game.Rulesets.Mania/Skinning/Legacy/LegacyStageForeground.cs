// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Graphics.Containers;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Mania.Objects.Drawables;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Skinning;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;
using osu.Game.Configuration;
using osuTK;

namespace osu.Game.Rulesets.Mania.Skinning.Legacy
{
    public partial class LegacyStageForeground : CompositeDrawable
    {
        private readonly IBindable<ScrollingDirection> direction = new Bindable<ScrollingDirection>();

        private Drawable? sprite;
        private Drawable? brokenSprite;
        private bool isBrokenModeActivated = false;
        private bool isBrokenAudioPlayed = false;
        private float originalY;
        private Playfield? playfield;

        // 抖动事件，供其他组件监听
        public event Action<float, float>? OnShakeAnimation;

        // FX 组件
        private LegacyStageFXBackground? fxBackground;

        // BPM 同步的底部特效
        private BPMSyncedBottomFX? bpmSyncedFX;

        // 基于表现的 Overload 特效
        private PerformanceBasedOverload? performanceOverload;

        // Yan 动画特效
        private YanAnimation? yanAnimation;

        // 底部容器（包含 sprite 和 bpmSyncedFX，一起抖动）
        private Container? bottomContainer;

        // 循环动画相关
        private ScheduledDelegate? monitoringDelegate;
        private ScheduledDelegate? shakeAnimationDelegate;
        private bool isAnyHoldNoteActive = false;

        // 设置绑定
        private Bindable<double>? shakeDisplacementSetting;
        private Bindable<double>? shakeRotationSetting;

        // 随机数生成器
        private readonly System.Random random = new System.Random();

        // 音频管理器
        [Resolved]
        private AudioManager audio { get; set; } = null!;

        // 皮肤源，用于加载音频文件
        private ISkinSource? skinSource;

        public LegacyStageForeground()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(ISkinSource skin, IScrollingInfo scrollingInfo, OsuConfigManager config, Playfield? pf = null)
        {
            // 保存皮肤源引用，用于音频播放
            skinSource = skin;

            string bottomImage = skin.GetManiaSkinConfig<string>(LegacyManiaSkinConfigurationLookups.BottomStageImage)?.Value
                                 ?? "mania-stage-bottom";

            sprite = skin.GetAnimation(bottomImage, true, true)?.With(d =>
            {
                d.Scale = new Vector2(1.6f);
                // sprite 在容器内，锚点设置为底部中心
                d.Anchor = Anchor.BottomCentre;
                d.Origin = Anchor.BottomCentre;
                d.Depth = 2; // 最底层
            });

            // 加载 broken 版本（预先加载，但不添加到容器）
            string brokenImageName = $"{bottomImage}-broken";
            brokenSprite = skin.GetAnimation(brokenImageName, true, true)?.With(d =>
            {
                d.Scale = new Vector2(1.6f);
                d.Anchor = Anchor.BottomCentre;
                d.Origin = Anchor.BottomCentre;
                d.Depth = 2; // 最底层（与原始 sprite 同层级）
            });

            // 创建 BPM 同步的底部特效
            bpmSyncedFX = new BPMSyncedBottomFX(skin, bottomImage);

            // 创建基于表现的 Overload 特效
            performanceOverload = new PerformanceBasedOverload(skin, bottomImage);
            performanceOverload.OnMaxOverloadReached += activateBrokenMode;

            // 创建 Yan 动画特效
            yanAnimation = new YanAnimation(skin, bottomImage);
            performanceOverload.OnMaxOverloadReached += yanAnimation.PlayAnimation;

            // 创建容器，包含 sprite 和 bpmSyncedFX，使它们一起抖动
            bottomContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                // 容器的初始位置：底部中心（向下滚动的默认方向）
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
            };

            // 将元素添加到容器中（层级从下到上）
            if (sprite != null)
            {
                bottomContainer.Add(sprite);
                originalY = bottomContainer.Y;
            }
            bottomContainer.Add(bpmSyncedFX);        // BPM 闪烁层
            bottomContainer.Add(performanceOverload); // Overload 层
            bottomContainer.Add(yanAnimation);        // Yan 动画层（最上）

            // 创建 FX 背景组件
            fxBackground = new LegacyStageFXBackground();

            // 构建最终的子元素列表
            InternalChildren = new Drawable[]
            {
                bottomContainer,  // 底部容器（包含 sprite 和 bpmSyncedFX）
                fxBackground,     // FX 背景在最上层
            };

            direction.BindTo(scrollingInfo.Direction);
            direction.BindValueChanged(onDirectionChanged, true);

            // 绑定设置
            shakeDisplacementSetting = config.GetBindable<double>(OsuSetting.ManiaStageShakeDisplacement);
            shakeRotationSetting = config.GetBindable<double>(OsuSetting.ManiaStageShakeRotation);

            // 尝试获取playfield来监听判定事件
            playfield = pf;
            if (playfield != null)
            {
                // Logger.Log("LegacyStageForeground: 成功获取到playfield");
                playfield.NewResult += onNewResult;
            }
            else
            {
                Logger.Log("LegacyStageForeground: 未能获取到playfield");
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // 启动定期监控长按音符状态
            Schedule(() => startHoldNoteMonitoring());
        }

        /// <summary>
        /// 启动长按音符监控 (模拟LightingL的实现方式)
        /// </summary>
        private void startHoldNoteMonitoring()
        {
            // Logger.Log("LegacyStageForeground: 启动长按音符监控");

            // 启动定期检查，每50ms检查一次长按音符状态
            monitoringDelegate = Scheduler.AddDelayed(() =>
            {
                checkHoldNotesAndUpdateAnimation();
            }, 50, true);
        }

        /// <summary>
        /// 检查长按音符状态并更新动画 (完全复制LightingL机制)
        /// </summary>
        private void checkHoldNotesAndUpdateAnimation()
        {
            bool hasActiveHoldNote = false;

            // 向上查找到Stage
            var stage = this.FindClosestParent<Stage>();
            if (stage == null) return;

            try
            {
                // 检查所有Column中的长按音符
                foreach (var column in stage.Columns)
                {
                    foreach (var hitObject in column.AllHitObjects)
                    {
                        if (hitObject is DrawableHoldNote holdNote && holdNote.IsHolding.Value)
                        {
                            hasActiveHoldNote = true;
                            // Logger.Log($"LegacyStageForeground: 发现活跃长按音符 {holdNote.GetHashCode()}, IsHolding: {holdNote.IsHolding.Value}");
                            break;
                        }
                    }

                    if (hasActiveHoldNote) break;
                }
            }
            catch (System.Exception ex)
            {
                Logger.Log($"LegacyStageForeground: 检查长按音符时出错: {ex.Message}");
                return;
            }

            // 根据状态控制循环动画
            if (hasActiveHoldNote && !isAnyHoldNoteActive)
            {
                // Logger.Log("LegacyStageForeground: 启动LightingL循环动画");
                startContinuousShakeAnimation();
            }
            else if (!hasActiveHoldNote && isAnyHoldNoteActive)
            {
                // Logger.Log("LegacyStageForeground: 停止LightingL循环动画");
                stopContinuousShakeAnimation();
            }
        }

        /// <summary>
        /// 激活 Broken 模式 - 永久替换为 broken 素材
        /// </summary>
        private void activateBrokenMode()
        {
            if (isBrokenModeActivated || brokenSprite == null || bottomContainer == null)
                return;

            isBrokenModeActivated = true;

            // 从容器中移除原始 sprite
            if (sprite != null)
            {
                bottomContainer.Remove(sprite, true);
                sprite = null;
            }

            // 添加 brokenSprite（Depth 值确保它在最底层）
            bottomContainer.Add(brokenSprite);
            sprite = brokenSprite;

            // 确保锚点正确
            var currentDirection = direction.Value;
            if (currentDirection == ScrollingDirection.Up)
                brokenSprite.Anchor = brokenSprite.Origin = Anchor.TopCentre;
            else
                brokenSprite.Anchor = brokenSprite.Origin = Anchor.BottomCentre;

            // 播放 Broken 模式音频效果
            playBrokenAudio();
        }

        private void onDirectionChanged(ValueChangedEvent<ScrollingDirection> direction)
        {
            if (direction.NewValue == ScrollingDirection.Up)
            {
                // 向上滚动：容器在顶部
                if (bottomContainer != null)
                    bottomContainer.Anchor = bottomContainer.Origin = Anchor.TopCentre;

                // sprite 在容器内也对齐到顶部
                if (sprite != null)
                    sprite.Anchor = sprite.Origin = Anchor.TopCentre;
                if (brokenSprite != null && isBrokenModeActivated)
                    brokenSprite.Anchor = brokenSprite.Origin = Anchor.TopCentre;
            }
            else
            {
                // 向下滚动：容器在底部
                if (bottomContainer != null)
                    bottomContainer.Anchor = bottomContainer.Origin = Anchor.BottomCentre;

                // sprite 在容器内也对齐到底部
                if (sprite != null)
                    sprite.Anchor = sprite.Origin = Anchor.BottomCentre;
                if (brokenSprite != null && isBrokenModeActivated)
                    brokenSprite.Anchor = brokenSprite.Origin = Anchor.BottomCentre;
            }

            // 更新原始Y位置
            if (bottomContainer != null)
                originalY = bottomContainer.Y;

            // 更新 BPM 同步特效的方向
            bpmSyncedFX?.UpdateDirection(direction.NewValue);

            // 更新 Overload 特效的方向
            performanceOverload?.UpdateDirection(direction.NewValue);

            // 更新 Yan 动画的方向
            yanAnimation?.UpdateDirection(direction.NewValue);
        }

        protected override void Update()
        {
            base.Update();

            // 每帧更新 Overload 特效（应用阻力衰减）
            performanceOverload?.UpdatePerFrame();
        }

        private void onNewResult(DrawableHitObject judgedObject, JudgementResult result)
        {
            // Logger.Log($"LegacyStageForeground: 收到判定 {result.Type}, 对象: {judgedObject?.GetType().Name}, bottomContainer存在: {bottomContainer != null}");

            // LightingN 机制：成功击中时触发单次抖动（类似爆炸效果）
            if (result.IsHit && judgedObject?.DisplayResult == true && bottomContainer != null)
            {
                // Logger.Log("LegacyStageForeground: 触发单次抖动动画 (LightingN机制)");
                performSingleShakeAnimation();
            }
        }

        /// <summary>
        /// 启动循环抖动动画 (LightingL 机制)
        /// </summary>
        private void startContinuousShakeAnimation()
        {
            if (bottomContainer == null || isAnyHoldNoteActive) return;

            isAnyHoldNoteActive = true;

            // 启动循环抖动动画，每50ms执行一次
            shakeAnimationDelegate = Scheduler.AddDelayed(() =>
            {
                if (isAnyHoldNoteActive && bottomContainer != null)
                {
                    // 获取设置值
                    float displacement = (float)(shakeDisplacementSetting?.Value ?? 15.0);
                    float rotationRange = (float)(shakeRotationSetting?.Value ?? 5.0);

                    // 生成随机角度偏转（-rotationRange 到 +rotationRange）
                    float randomRotation = (float)(random.NextDouble() * rotationRange * 2 - rotationRange);

                    // 执行循环抖动：向下移动设置的单位数，然后在50ms内回到原位置
                    bottomContainer.MoveToY(originalY + displacement, 0);
                    bottomContainer.MoveToY(originalY, 50, Easing.OutQuart);

                    // 添加随机角度偏转，以底部中心为圆心
                    bottomContainer.RotateTo(randomRotation, 0);
                    bottomContainer.RotateTo(0, 50, Easing.OutQuart);

                    // 每次抖动时都触发 FX 动画播放一次
                    fxBackground?.PlaySingleAnimation();

                    // 触发 Overload 特效增加强度
                    performanceOverload?.OnShake();

                    // 触发抖动事件，供其他组件监听
                    OnShakeAnimation?.Invoke(displacement, randomRotation);
                }
            }, 50, true);
        }

        /// <summary>
        /// 停止循环抖动动画 (LightingL 机制)
        /// </summary>
        private void stopContinuousShakeAnimation()
        {
            if (!isAnyHoldNoteActive) return;

            isAnyHoldNoteActive = false;

            // 停止循环动画
            shakeAnimationDelegate?.Cancel();
            shakeAnimationDelegate = null;

            // 确保 bottomContainer 回到原始位置和角度
            if (bottomContainer != null)
            {
                bottomContainer.MoveTo(new Vector2(bottomContainer.X, originalY), 100, Easing.OutQuart);
                bottomContainer.RotateTo(0, 100, Easing.OutQuart);
            }
        }

        /// <summary>
        /// 当有新的判定结果时调用此方法来触发动画
        /// </summary>
        public void OnNewJudgement(JudgementResult result)
        {
            onNewResult(null!, result);
        }

        /// <summary>
        /// 执行单次抖动动画 (LightingN 机制)
        /// </summary>
        private void performSingleShakeAnimation()
        {
            if (bottomContainer == null) return;

            // 获取设置值
            float displacement = (float)(shakeDisplacementSetting?.Value ?? 15.0);
            float rotationRange = (float)(shakeRotationSetting?.Value ?? 5.0);

            // 生成随机角度偏转（-rotationRange 到 +rotationRange）
            float randomRotation = (float)(random.NextDouble() * rotationRange * 2 - rotationRange);

            // 触发 FX 单次动画
            fxBackground?.PlaySingleAnimation();

            // 触发 Overload 特效增加强度
            performanceOverload?.OnShake();

            // 执行上下抖动动画：向下移动设置的单位数，然后在100ms内回到原位置
            bottomContainer.MoveToY(originalY + displacement, 0);
            bottomContainer.MoveToY(originalY, 100, Easing.OutQuart);

            // 添加随机角度偏转，以底部中心为圆心
            bottomContainer.RotateTo(randomRotation, 0);
            bottomContainer.RotateTo(0, 100, Easing.OutQuart);

            // 触发抖动事件，供其他组件监听
            OnShakeAnimation?.Invoke(displacement, randomRotation);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (playfield != null)
            {
                playfield.NewResult -= onNewResult;
            }

            // 停止所有定时器
            monitoringDelegate?.Cancel();
            monitoringDelegate = null;

            shakeAnimationDelegate?.Cancel();
            shakeAnimationDelegate = null;

            isAnyHoldNoteActive = false;

            base.Dispose(isDisposing);
        }

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
                EarlyActivationMilliseconds = 40;
                Depth = 1; // 中间层（在 bottom 之上，overload 之下）
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                string fxImageName = $"{bottomImageBaseName}-fx";

                fxSprite = skin.GetAnimation(fxImageName, true, true)?.With(d =>
                {
                    d.Scale = new Vector2(1.6f);
                    d.Origin = Anchor.BottomCentre;
                    d.Anchor = Anchor.BottomCentre;
                    d.Alpha = 0;
                });

                if (fxSprite != null)
                    AddInternal(fxSprite);
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

            protected override void OnNewBeat(int beatIndex, TimingControlPoint timingPoint, EffectControlPoint effectPoint, ChannelAmplitudes amplitudes)
            {
                base.OnNewBeat(beatIndex, timingPoint, effectPoint, amplitudes);

                if (fxSprite == null)
                    return;

                double length = timingPoint.BeatLength;

                while (length < 250)
                    length *= 2;

                fxSprite
                    .FadeTo(1f, 40, Easing.Out)
                    .Then()
                    .FadeTo(0f, length, Easing.Out);
            }
        }

        /// <summary>
        /// 基于玩家表现的 Overload 特效 - 转速系统
        /// </summary>
        public partial class PerformanceBasedOverload : Container
        {
            private Drawable? overloadSprite;
            private readonly ISkinSource skin;
            private readonly string bottomImageBaseName;

            // 转速系统
            private double currentRotationSpeed = 0.0;
            private const double BASE_ROTATION_SPEED = 0.0;
            private const double HIT_SPEED_INCREASE = 2.5;
            private const double DRAG_COEFFICIENT = 0.96;

            // 透明度计算范围
            private const double MIN_SPEED = 1.0;
            private const double MAX_SPEED = 8.0;

            // Combo 检查
            private const int MIN_COMBO_THRESHOLD = 500;
            private IBindable<int>? combo = null;

            // 最大 Overload 事件
            public event Action? OnMaxOverloadReached;
            private bool hasReachedMaxOverload = false;

            // 高透明度持续时间追踪
            private double highAlphaDuration = 0.0;
            private const double HIGH_ALPHA_THRESHOLD = 0.3;  // 30% 透明度阈值
            private const double HIGH_ALPHA_DURATION_REQUIRED = 2000.0;  // 2秒持续时间

            // 永久激活状态
            private bool overloadPermanentlyActivated = false;

            public PerformanceBasedOverload(ISkinSource skin, string bottomImageBaseName)
            {
                this.skin = skin;
                this.bottomImageBaseName = bottomImageBaseName;

                RelativeSizeAxes = Axes.Both;
                Depth = 0; // 第二层（在 BPM FX 之上，Yan 之下）
            }

            [BackgroundDependencyLoader]
            private void load(ScoreProcessor? scoreProcessor = null)
            {
                string overloadImageName = $"{bottomImageBaseName}-overload";

                overloadSprite = skin.GetAnimation(overloadImageName, true, true)?.With(d =>
                {
                    d.Scale = new Vector2(1.6f);
                    d.Anchor = Anchor.BottomCentre;
                    d.Origin = Anchor.BottomCentre;
                    d.Alpha = 0;
                });

                if (overloadSprite != null)
                    AddInternal(overloadSprite);

                // Only bind to combo if ScoreProcessor is available (e.g., during gameplay)
                if (scoreProcessor != null)
                    combo = scoreProcessor.Combo.GetBoundCopy();
            }

            public void UpdateDirection(ScrollingDirection direction)
            {
                if (overloadSprite == null)
                    return;

                if (direction == ScrollingDirection.Up)
                    overloadSprite.Anchor = overloadSprite.Origin = Anchor.TopCentre;
                else
                    overloadSprite.Anchor = overloadSprite.Origin = Anchor.BottomCentre;
            }

            /// <summary>
            /// 每次抖动时调用 - 增加转速（一旦突破 500 Combo 就永久激活）
            /// </summary>
            public void OnShake()
            {
                if (overloadSprite == null)
                    return;

                // 检查是否首次达到 combo 阈值（仅当 combo 可用时）
                if (!overloadPermanentlyActivated && combo != null && combo.Value > MIN_COMBO_THRESHOLD)
                    overloadPermanentlyActivated = true;

                // 如果未永久激活且 Combo <= 500，不响应抖动
                if (!overloadPermanentlyActivated)
                    return;

                currentRotationSpeed += HIT_SPEED_INCREASE;
                updateAlpha();
            }

            /// <summary>
            /// 每帧更新 - 应用阻力衰减（一旦突破 500 Combo 就永久激活）
            /// </summary>
            public void UpdatePerFrame()
            {
                if (overloadSprite == null)
                    return;

                // 检查是否首次达到 combo 阈值（持续检查，确保激活，仅当 combo 可用时）
                if (!overloadPermanentlyActivated && combo != null && combo.Value > MIN_COMBO_THRESHOLD)
                    overloadPermanentlyActivated = true;

                currentRotationSpeed *= DRAG_COEFFICIENT;

                if (currentRotationSpeed < BASE_ROTATION_SPEED)
                    currentRotationSpeed = BASE_ROTATION_SPEED;

                updateAlpha();

                // 更新高透明度持续时间追踪
                updateHighAlphaDuration();
            }

            /// <summary>
            /// 根据转速计算并更新透明度（一旦突破 500 Combo 就永久激活）
            /// </summary>
            private void updateAlpha()
            {
                if (overloadSprite == null)
                    return;

                // 如果未永久激活且 Combo <= 500（或 combo 不可用），则强制透明度为 0
                if (!overloadPermanentlyActivated && (combo == null || combo.Value <= MIN_COMBO_THRESHOLD))
                {
                    overloadSprite.Alpha = 0;
                    return;
                }

                // 计算基于转速的透明度
                double alpha = (currentRotationSpeed - MIN_SPEED) / (MAX_SPEED - MIN_SPEED);
                alpha = Math.Clamp(alpha, 0, 1);

                overloadSprite.Alpha = (float)alpha;
            }

            /// <summary>
            /// 更新高透明度持续时间追踪
            /// </summary>
            private void updateHighAlphaDuration()
            {
                if (overloadSprite == null)
                    return;

                // 如果透明度 >= 95%，增加持续时间
                if (overloadSprite.Alpha >= HIGH_ALPHA_THRESHOLD)
                {
                    highAlphaDuration += Clock.ElapsedFrameTime;

                    // 如果持续时间达到要求且未触发过事件，则触发
                    if (!hasReachedMaxOverload && highAlphaDuration >= HIGH_ALPHA_DURATION_REQUIRED)
                    {
                        hasReachedMaxOverload = true;
                        OnMaxOverloadReached?.Invoke();
                    }
                }
                else
                {
                    // 透明度低于阈值，重置持续时间
                    highAlphaDuration = 0.0;
                }
            }

            /// <summary>
            /// 重置转速系统（用于 retry）
            /// </summary>
            public void Reset()
            {
                currentRotationSpeed = BASE_ROTATION_SPEED;
                if (overloadSprite != null)
                    overloadSprite.Alpha = 0;

                hasReachedMaxOverload = false;
                highAlphaDuration = 0.0;  // 重置高透明度持续时间
                overloadPermanentlyActivated = false;  // 重置永久激活状态
            }
        }

        /// <summary>
        /// Yan 动画特效 - 当 Overload 透明度保持在 30% 以上持续 2 秒时播放
        /// </summary>
        public partial class YanAnimation : Container
        {
            private readonly ISkinSource skin;
            private readonly string bottomImageBaseName;
            private Drawable? yanSprite;
            private Drawable? yanSSprite;

            public YanAnimation(ISkinSource skin, string bottomImageBaseName)
            {
                this.skin = skin;
                this.bottomImageBaseName = bottomImageBaseName;

                RelativeSizeAxes = Axes.Both;
                Depth = -1; // 最上层（在所有元素之上）
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                // 尝试多种可能的素材名称（按优先级排序）
                string[] possibleNames = {
                    $"{bottomImageBaseName}-yan",         // 完整路径（最可能成功）
                    $"{System.IO.Path.GetFileName(bottomImageBaseName)}-yan", // 基础名称
                    "mania-stage-bottom-yan",             // 标准名称
                    "bottom4-yan"                         // 直接使用 bottom4-yan
                };

                foreach (string yanImageName in possibleNames)
                {
                    // 使用成功的参数组合：动画，不循环，60fps
                    yanSprite = skin.GetAnimation(yanImageName, true, false, false, "-", true, 1000.0 / 60.0)?.With(d =>
                    {
                        d.Scale = new Vector2(1.6f);
                        d.Anchor = Anchor.BottomCentre;
                        d.Origin = Anchor.BottomCentre;
                    });

                    if (yanSprite != null)
                    {
                        Add(yanSprite);
                        yanSprite.Alpha = 0; // 初始隐藏
                        break; // 找到素材后退出循环
                    }
                }

                if (yanSprite == null)
                {
                    // 未找到 Yan 动画素材
                }

                // 加载 yanS 动画（循环播放）
                string[] possibleYanSNames = {
                    $"{bottomImageBaseName}-yanS",         // 完整路径
                    $"{System.IO.Path.GetFileName(bottomImageBaseName)}-yanS", // 基础名称
                    "mania-stage-bottom-yanS",             // 标准名称
                    "bottom4-yanS"                         // 直接使用 bottom4-yanS
                };

                foreach (string yanSImageName in possibleYanSNames)
                {
                    // 使用循环播放的参数：动画，循环，60fps
                    yanSSprite = skin.GetAnimation(yanSImageName, true, true, false, "-", true, 1000.0 / 60.0)?.With(d =>
                    {
                        d.Scale = new Vector2(1.6f);
                        d.Anchor = Anchor.BottomCentre;
                        d.Origin = Anchor.BottomCentre;
                    });

                    if (yanSSprite != null)
                    {
                        Add(yanSSprite);
                        yanSSprite.Alpha = 0; // 初始隐藏
                        break; // 找到素材后退出循环
                    }
                }

                if (yanSSprite == null)
                {
                    // 未找到 YanS 动画素材
                }
            }

            /// <summary>
            /// 播放 Yan 动画（当 Overload 透明度保持在 30% 以上持续 2 秒时调用）
            /// </summary>
            public void PlayAnimation()
            {
                if (yanSprite == null)
                    return;

                // 立即显示并播放动画
                yanSprite.Alpha = 1.0f;

                // 尝试重置动画（如果支持的话）
                try
                {
                    var resetMethod = yanSprite.GetType().GetMethod("GotoFrame");
                    if (resetMethod != null)
                    {
                        resetMethod.Invoke(yanSprite, new object[] { 0 });
                    }
                }
                catch (Exception)
                {
                    // 重置失败，忽略
                }

                // 3秒后自动淡出，然后触发 yanS 动画
                yanSprite.Delay(4000).FadeOut(100, Easing.Out).OnComplete(_ =>
                {
                    // yan 动画播放完毕后，触发 yanS 动画
                    PlayYanSAnimation();
                });
            }

            /// <summary>
            /// 播放 YanS 动画（循环播放）
            /// </summary>
            private void PlayYanSAnimation()
            {
                if (yanSSprite == null)
                    return;

                // 立即显示并开始循环播放
                yanSSprite.Alpha = 1.0f;

                // 尝试重置动画到开始（如果支持的话）
                try
                {
                    var resetMethod = yanSSprite.GetType().GetMethod("GotoFrame");
                    if (resetMethod != null)
                    {
                        resetMethod.Invoke(yanSSprite, new object[] { 0 });
                    }
                }
                catch (Exception)
                {
                    // 重置失败，忽略
                }
            }

            /// <summary>
            /// 更新滚动方向
            /// </summary>
            public void UpdateDirection(ScrollingDirection direction)
            {
                if (yanSprite != null)
                {
                    if (direction == ScrollingDirection.Up)
                        yanSprite.Anchor = yanSprite.Origin = Anchor.TopCentre;
                    else
                        yanSprite.Anchor = yanSprite.Origin = Anchor.BottomCentre;
                }

                if (yanSSprite != null)
                {
                    if (direction == ScrollingDirection.Up)
                        yanSSprite.Anchor = yanSSprite.Origin = Anchor.TopCentre;
                    else
                        yanSSprite.Anchor = yanSSprite.Origin = Anchor.BottomCentre;
                }
            }

            /// <summary>
            /// 重置动画状态
            /// </summary>
            public void Reset()
            {
                if (yanSprite != null)
                {
                    yanSprite.Alpha = 0;
                    yanSprite.ClearTransforms();
                }

                if (yanSSprite != null)
                {
                    yanSSprite.Alpha = 0;
                    yanSSprite.ClearTransforms();
                }
            }
        }

        /// <summary>
        /// 播放 Broken 模式音频效果
        /// </summary>
        private void playBrokenAudio()
        {
            // 防止重复播放
            if (isBrokenAudioPlayed)
            {
                Logger.Log($"[BrokenAudio] 音频已播放过，跳过重复播放", level: LogLevel.Verbose);
                return;
            }

            isBrokenAudioPlayed = true;
            Logger.Log($"[BrokenAudio] 开始尝试播放 mania-stage-bottom-broken.mp3", level: LogLevel.Verbose);

            // 尝试通过皮肤系统获取音频样本
            // 音频文件应该放在皮肤文件夹的根目录下
            try
            {
                var sampleInfo = new HitSampleInfo("mania-stage-bottom-broken");
                var sampleChannel = skinSource?.GetSample(sampleInfo);

                Logger.Log($"[BrokenAudio] 皮肤对象: {skinSource?.GetType().Name}", level: LogLevel.Verbose);
                Logger.Log($"[BrokenAudio] 样本信息: {sampleInfo?.LookupNames.FirstOrDefault()}", level: LogLevel.Verbose);
                Logger.Log($"[BrokenAudio] 获取的样本通道: {(sampleChannel != null ? "成功" : "失败")}", level: LogLevel.Verbose);

                if (sampleChannel != null)
                {
                    // 设置音量为特效音量
                    sampleChannel.Volume.Value = audio.VolumeSample.Value;
                    Logger.Log($"[BrokenAudio] 设置音量: {audio.VolumeSample.Value}", level: LogLevel.Verbose);

                    sampleChannel.Play();
                    Logger.Log($"[BrokenAudio] 音频播放成功!", level: LogLevel.Verbose);
                }
                else
                {
                    Logger.Log($"[BrokenAudio] 未能通过皮肤系统找到音频文件: mania-stage-bottom-broken.mp3", level: LogLevel.Verbose);
                    Logger.Log($"[BrokenAudio] 请确保音频文件位于皮肤文件夹的根目录下", level: LogLevel.Verbose);

                    // 尝试备用方法：直接通过 AudioManager 获取
                    Logger.Log($"[BrokenAudio] 尝试备用方法: audio.Samples.Get()", level: LogLevel.Verbose);
                    var fallbackSample = audio.Samples.Get("mania-stage-bottom-broken");
                    if (fallbackSample != null)
                    {
                        fallbackSample.Volume.Value = audio.VolumeSample.Value;
                        fallbackSample.Play();
                        Logger.Log($"[BrokenAudio] 备用方法播放成功!", level: LogLevel.Verbose);
                    }
                    else
                    {
                        Logger.Log($"[BrokenAudio] 备用方法也失败了", level: LogLevel.Verbose);
                        Logger.Log($"[BrokenAudio] 可能的原因:", level: LogLevel.Verbose);
                        Logger.Log($"[BrokenAudio] 1. 音频文件不存在于皮肤文件夹", level: LogLevel.Verbose);
                        Logger.Log($"[BrokenAudio] 2. 文件格式不正确（应为 .mp3）", level: LogLevel.Verbose);
                        Logger.Log($"[BrokenAudio] 3. 文件名不匹配（应为 mania-stage-bottom-broken.mp3）", level: LogLevel.Verbose);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[BrokenAudio] 播放音频时发生异常: {ex.Message}", level: LogLevel.Important);
                Logger.Log($"[BrokenAudio] 异常详情: {ex.StackTrace}", level: LogLevel.Error);
            }
        }
    }
}
