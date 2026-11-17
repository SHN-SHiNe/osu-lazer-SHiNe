// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Skinning;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mania.UI;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Mania.Skinning.Legacy
{
    public partial class LegacyHitTarget : CompositeDrawable
    {
        private readonly IBindable<ScrollingDirection> direction = new Bindable<ScrollingDirection>();

        private Container directionContainer = null!;

        // 抖动跟随相关
        private Bindable<bool>? followShakeSetting;
        private LegacyStageForeground? stageForeground;
        private float originalY;
        private bool isShaking = false;

        [BackgroundDependencyLoader]
        private void load(ISkinSource skin, IScrollingInfo scrollingInfo, OsuConfigManager config)
        {
            string targetImage = skin.GetManiaSkinConfig<string>(LegacyManiaSkinConfigurationLookups.HitTargetImage)?.Value
                                 ?? "mania-stage-hint";

            bool showJudgementLine = skin.GetManiaSkinConfig<bool>(LegacyManiaSkinConfigurationLookups.ShowJudgementLine)?.Value
                                     ?? true;

            Color4 lineColour = skin.GetManiaSkinConfig<Color4>(LegacyManiaSkinConfigurationLookups.JudgementLineColour)?.Value
                                ?? Color4.White;

            InternalChild = directionContainer = new Container
            {
                Origin = Anchor.CentreLeft,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new Sprite
                    {
                        Texture = skin.GetTexture(targetImage),
                        Scale = new Vector2(1, 0.9f * 1.6025f),
                        RelativeSizeAxes = Axes.X,
                        Width = 1
                    },
                    new Box
                    {
                        Anchor = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.X,
                        Height = 1,
                        Colour = LegacyColourCompatibility.DisallowZeroAlpha(lineColour),
                        Alpha = showJudgementLine ? 0.9f : 0
                    }
                }
            };

            direction.BindTo(scrollingInfo.Direction);
            direction.BindValueChanged(onDirectionChanged, true);

            // 绑定跟随抖动设置
            followShakeSetting = config.GetBindable<bool>(OsuSetting.ManiaStageHintFollowShake);
            followShakeSetting.ValueChanged += onFollowShakeSettingChanged;

            // 尝试找到 LegacyStageForeground 组件
            Schedule(() => findStageForeground());
        }

        private void onDirectionChanged(ValueChangedEvent<ScrollingDirection> direction)
        {
            if (direction.NewValue == ScrollingDirection.Up)
            {
                directionContainer.Anchor = Anchor.TopLeft;
                directionContainer.Scale = new Vector2(1, -1);
            }
            else
            {
                directionContainer.Anchor = Anchor.BottomLeft;
                directionContainer.Scale = Vector2.One;
            }

            // 更新原始Y位置
            originalY = directionContainer.Y;
        }

        private void onFollowShakeSettingChanged(ValueChangedEvent<bool> e)
        {
            if (e.NewValue)
            {
                // 启用跟随抖动
                if (stageForeground != null)
                {
                    stageForeground.OnShakeAnimation += onStageShake;
                }
            }
            else
            {
                // 禁用跟随抖动
                if (stageForeground != null)
                {
                    stageForeground.OnShakeAnimation -= onStageShake;
                }

                // 停止当前抖动并回到原位置
                if (isShaking)
                {
                    directionContainer.MoveToY(originalY, 100, Easing.OutQuart);
                    directionContainer.RotateTo(0, 100, Easing.OutQuart);
                    isShaking = false;
                }
            }
        }

        private void findStageForeground()
        {
            try
            {
                // 使用反射来查找LegacyStageForeground
                var stage = this.FindClosestParent<Stage>();
                if (stage != null)
                {
                    // 使用反射访问InternalChildren
                    var internalChildrenProperty = typeof(CompositeDrawable).GetProperty("InternalChildren",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (internalChildrenProperty?.GetValue(stage) is IEnumerable<Drawable> children)
                    {
                        stageForeground = findInChildren(children, 0, 5); // 限制递归深度为5层

                        if (stageForeground != null && followShakeSetting?.Value == true)
                        {
                            stageForeground.OnShakeAnimation += onStageShake;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果查找失败，记录错误但不影响游戏运行
                Logger.Log($"LegacyHitTarget: 查找LegacyStageForeground时出错: {ex.Message}");
            }
        }

        private LegacyStageForeground? findInChildren(IEnumerable<Drawable> children, int currentDepth, int maxDepth)
        {
            if (currentDepth >= maxDepth)
                return null;

            foreach (var child in children)
            {
                if (child is LegacyStageForeground foreground)
                    return foreground;

                // 只对CompositeDrawable进行递归查找
                if (child is CompositeDrawable compositeChild)
                {
                    try
                    {
                        var internalChildrenProperty = typeof(CompositeDrawable).GetProperty("InternalChildren",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (internalChildrenProperty?.GetValue(compositeChild) is IEnumerable<Drawable> subChildren)
                        {
                            var found = findInChildren(subChildren, currentDepth + 1, maxDepth);
                            if (found != null)
                                return found;
                        }
                    }
                    catch
                    {
                        // 忽略反射访问错误，继续查找下一个组件
                        continue;
                    }
                }
            }
            return null;
        }

        private void onStageShake(float displacement, float rotation)
        {
            if (followShakeSetting?.Value != true || isShaking) return;

            isShaking = true;

            // 执行与stage-bottom完全相同的抖动动画
            directionContainer.MoveToY(originalY + displacement, 0);
            directionContainer.MoveToY(originalY, 100, Easing.OutQuart);

            directionContainer.RotateTo(rotation, 0);
            directionContainer.RotateTo(0, 100, Easing.OutQuart);

            // 动画完成后重置状态
            Schedule(() => isShaking = false);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (stageForeground != null)
            {
                stageForeground.OnShakeAnimation -= onStageShake;
            }

            if (followShakeSetting != null)
            {
                followShakeSetting.ValueChanged -= onFollowShakeSettingChanged;
            }

            base.Dispose(isDisposing);
        }
    }
}
