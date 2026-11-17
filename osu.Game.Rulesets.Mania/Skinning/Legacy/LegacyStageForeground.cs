// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Framework.Threading;
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
        private float originalY;
        private Playfield? playfield;

        // 抖动事件，供其他组件监听
        public event Action<float, float>? OnShakeAnimation;

        // FX 组件
        private LegacyStageFXBackground? fxBackground;

        // 循环动画相关
        private ScheduledDelegate? monitoringDelegate;
        private ScheduledDelegate? shakeAnimationDelegate;
        private bool isAnyHoldNoteActive = false;

        // 设置绑定
        private Bindable<double>? shakeDisplacementSetting;
        private Bindable<double>? shakeRotationSetting;

        // 随机数生成器
        private readonly System.Random random = new System.Random();

        public LegacyStageForeground()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(ISkinSource skin, IScrollingInfo scrollingInfo, OsuConfigManager config, Playfield? pf = null)
        {
            string bottomImage = skin.GetManiaSkinConfig<string>(LegacyManiaSkinConfigurationLookups.BottomStageImage)?.Value
                                 ?? "mania-stage-bottom";

            sprite = skin.GetAnimation(bottomImage, true, true)?.With(d =>
            {
                d.Scale = new Vector2(1.6f);
                // 设置旋转中心为底部中心
                d.Origin = Anchor.BottomCentre;
            });

            // 创建 FX 背景组件
            fxBackground = new LegacyStageFXBackground();

            var children = new List<Drawable>();

            // 先添加 bottom sprite（如果存在）
            if (sprite != null)
            {
                children.Add(sprite);
                originalY = sprite.Y;
            }

            // 然后添加 FX 背景，确保它在最上层
            children.Add(fxBackground);

            InternalChildren = children.ToArray();

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

        private void onDirectionChanged(ValueChangedEvent<ScrollingDirection> direction)
        {
            if (sprite == null)
                return;

            if (direction.NewValue == ScrollingDirection.Up)
                sprite.Anchor = sprite.Origin = Anchor.TopCentre;
            else
                sprite.Anchor = sprite.Origin = Anchor.BottomCentre;

            // 更新原始Y位置
            originalY = sprite.Y;
        }

        private void onNewResult(DrawableHitObject judgedObject, JudgementResult result)
        {
            // Logger.Log($"LegacyStageForeground: 收到判定 {result.Type}, 对象: {judgedObject?.GetType().Name}, sprite存在: {sprite != null}");

            // LightingN 机制：成功击中时触发单次抖动（类似爆炸效果）
            if (result.IsHit && judgedObject?.DisplayResult == true && sprite != null)
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
            if (sprite == null || isAnyHoldNoteActive) return;

            isAnyHoldNoteActive = true;

            // 启动循环抖动动画，每50ms执行一次
            shakeAnimationDelegate = Scheduler.AddDelayed(() =>
            {
                if (isAnyHoldNoteActive && sprite != null)
                {
                    // 获取设置值
                    float displacement = (float)(shakeDisplacementSetting?.Value ?? 15.0);
                    float rotationRange = (float)(shakeRotationSetting?.Value ?? 5.0);

                    // 生成随机角度偏转（-rotationRange 到 +rotationRange）
                    float randomRotation = (float)(random.NextDouble() * rotationRange * 2 - rotationRange);

                    // 执行循环抖动：向下移动设置的单位数，然后在50ms内回到原位置
                    sprite.MoveToY(originalY + displacement, 0);
                    sprite.MoveToY(originalY, 50, Easing.OutQuart);

                    // 添加随机角度偏转，以底部中心为圆心
                    sprite.RotateTo(randomRotation, 0);
                    sprite.RotateTo(0, 50, Easing.OutQuart);

                    // 每次抖动时都触发 FX 动画播放一次
                    fxBackground?.PlaySingleAnimation();

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

            // 确保sprite回到原始位置和角度
            if (sprite != null)
            {
                sprite.MoveTo(new Vector2(sprite.X, originalY), 100, Easing.OutQuart);
                sprite.RotateTo(0, 100, Easing.OutQuart);
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
            if (sprite == null) return;

            // 获取设置值
            float displacement = (float)(shakeDisplacementSetting?.Value ?? 15.0);
            float rotationRange = (float)(shakeRotationSetting?.Value ?? 5.0);

            // 生成随机角度偏转（-rotationRange 到 +rotationRange）
            float randomRotation = (float)(random.NextDouble() * rotationRange * 2 - rotationRange);

            // 触发 FX 单次动画
            fxBackground?.PlaySingleAnimation();

            // 执行上下抖动动画：向下移动设置的单位数，然后在100ms内回到原位置
            sprite.MoveToY(originalY + displacement, 0);
            sprite.MoveToY(originalY, 100, Easing.OutQuart);

            // 添加随机角度偏转，以底部中心为圆心
            sprite.RotateTo(randomRotation, 0);
            sprite.RotateTo(0, 100, Easing.OutQuart);

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
    }
}
