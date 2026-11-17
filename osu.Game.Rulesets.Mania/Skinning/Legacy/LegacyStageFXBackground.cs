// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Graphics.Shapes;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Skinning;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Mania.Skinning.Legacy
{
    /// <summary>
    /// A component that handles the FX animations for stage left and right elements.
    /// These animations will be displayed on top of all other stage elements.
    /// FX animations play when mania-stage-bottom shakes.
    /// Uses object pooling for independent animation instances.
    /// </summary>
    public partial class LegacyStageFXBackground : CompositeDrawable
    {
        // 对象池：左右FX动画实例（减少数量避免过多对象）
        private readonly DrawablePool<FXAnimationInstance> leftFXPool = new DrawablePool<FXAnimationInstance>(10);
        private readonly DrawablePool<FXAnimationInstance> rightFXPool = new DrawablePool<FXAnimationInstance>(10);

        // 容器：存放所有活跃的FX实例
        private Container leftFXContainer;
        private Container rightFXContainer;

        // 最大同时存在的动画实例数（减少以避免过多对象）
        private const int MAX_CONCURRENT_FX = 5;

        // 随机数生成器，用于角度偏移
        private readonly Random random = new Random();

        // 皮肤源引用，用于对象池中创建实例
        private ISkinSource skinSource;

        // FX 模板精灵（用于调试和回退）
        private Drawable leftFXTemplate;
        private Drawable rightFXTemplate;

        public LegacyStageFXBackground()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(ISkinSource skin, StageDefinition stageDefinition)
        {
            skinSource = skin;

            // Use dedicated FX image names - these will look for sequence frames automatically
            // The GetAnimation method will search for: mania-stage-left-fx-0.png, mania-stage-left-fx-1.png, etc.
            string leftFXImage = "mania-stage-left-fx";
            string rightFXImage = "mania-stage-right-fx";

            // 创建模板（用于检测是否存在皮肤资源）
            leftFXTemplate = skin.GetAnimation(leftFXImage, true, false);
            rightFXTemplate = skin.GetAnimation(rightFXImage, true, false);

            // Debug: Log whether FX sprites were found
            System.Console.WriteLine($"[DEBUG] LegacyStageFXBackground: leftFXTemplate = {(leftFXTemplate != null ? "FOUND" : "NOT FOUND")}");
            System.Console.WriteLine($"[DEBUG] LegacyStageFXBackground: rightFXTemplate = {(rightFXTemplate != null ? "FOUND" : "NOT FOUND")}");

            var children = new List<Drawable>();

            // 添加对象池
            children.Add(leftFXPool);
            children.Add(rightFXPool);

            // 左侧FX容器
            leftFXContainer = new Container
            {
                Name = "Left FX Container",
                RelativeSizeAxes = Axes.Both,
            };
            children.Add(leftFXContainer);

            // 右侧FX容器
            rightFXContainer = new Container
            {
                Name = "Right FX Container",
                RelativeSizeAxes = Axes.Both,
            };
            children.Add(rightFXContainer);

            // 如果没有找到FX皮肤资源，添加调试用的回退Box
            if (leftFXTemplate == null)
            {
                children.Add(new Box
                {
                    Name = "Left FX Fallback",
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomRight,
                    X = 0.05f,
                    Width = 10,
                    Height = 100,
                    Colour = Color4.Red,
                    Alpha = 0.3f
                });
                System.Console.WriteLine("[DEBUG] Using fallback red box for left FX");
            }

            if (rightFXTemplate == null)
            {
                children.Add(new Box
                {
                    Name = "Right FX Fallback",
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomLeft,
                    X = -0.05f,
                    Width = 10,
                    Height = 100,
                    Colour = Color4.Blue,
                    Alpha = 0.3f
                });
                System.Console.WriteLine("[DEBUG] Using fallback blue box for right FX");
            }

            InternalChildren = children.ToArray();
        }

        /// <summary>
        /// 当 mania-stage-bottom 执行单次抖动时调用
        /// 在长按期间，这个方法会每50ms被调用一次
        /// </summary>
        public void PlaySingleAnimation()
        {
            // 生成 -5° 到 15° 范围内的随机角度
            float baseAngle = (float)(random.NextDouble() * 20.0 - 5.0); // -5° 到 15°

            // 左侧角度：镜像处理（向外偏转）
            float leftAngle = -baseAngle;   // 例如：baseAngle=10° → leftAngle=-10°（向左偏转）

            // 右侧角度：向外偏转
            float rightAngle = baseAngle;   // 例如：baseAngle=10° → rightAngle=10°（向右偏转）

            System.Console.WriteLine($"[DEBUG] FX: Playing animation - Left: {leftAngle:F1}°, Right: {rightAngle:F1}°");

            // 启动左侧FX动画
            if (leftFXTemplate != null)
            {
                // 检查并清理过多的动画实例
                if (leftFXContainer.Count > MAX_CONCURRENT_FX)
                {
                    var oldestLeft = leftFXContainer.FirstOrDefault();
                    if (oldestLeft != null)
                    {
                        oldestLeft.ClearTransforms();
                        oldestLeft.FadeOut(100).Expire();
                    }
                }

                // 从对象池获取左侧FX实例
                leftFXPool.Get(fxInstance =>
                {
                    fxInstance.SetupAnimation(skinSource, "mania-stage-left-fx", leftAngle, true, DrawHeight);
                    leftFXContainer.Add(fxInstance);
                });
            }

            // 启动右侧FX动画
            if (rightFXTemplate != null)
            {
                // 检查并清理过多的动画实例
                if (rightFXContainer.Count > MAX_CONCURRENT_FX)
                {
                    var oldestRight = rightFXContainer.FirstOrDefault();
                    if (oldestRight != null)
                    {
                        oldestRight.ClearTransforms();
                        oldestRight.FadeOut(100).Expire();
                    }
                }

                // 从对象池获取右侧FX实例
                rightFXPool.Get(fxInstance =>
                {
                    fxInstance.SetupAnimation(skinSource, "mania-stage-right-fx", rightAngle, false, DrawHeight);
                    rightFXContainer.Add(fxInstance);
                });
            }
        }
    }

    /// <summary>
    /// 可池化的FX动画实例
    /// 每个实例都是独立的，有自己的动画状态和生命周期
    /// </summary>
    public partial class FXAnimationInstance : PoolableDrawable
    {
        private Drawable animationSprite;
        private bool isLeft;

        public FXAnimationInstance()
        {
            RelativeSizeAxes = Axes.Both;
        }

        /// <summary>
        /// 设置动画参数并开始播放
        /// </summary>
        /// <param name="skin">皮肤源</param>
        /// <param name="animationName">动画名称</param>
        /// <param name="rotationAngle">旋转角度</param>
        /// <param name="isLeftSide">是否为左侧</param>
        /// <param name="stageHeight">舞台高度（用于缩放）</param>
        public void SetupAnimation(ISkinSource skin, string animationName, float rotationAngle, bool isLeftSide, float stageHeight)
        {
            isLeft = isLeftSide;

            // 从皮肤获取新的动画实例
            animationSprite = skin.GetAnimation(animationName, true, false,false, "-", true, 1000.0/120.0);

            if (animationSprite != null)
            {
                // 设置锚点和原点（旋转中心在底部）
                if (isLeft)
                {
                    animationSprite.Anchor = Anchor.BottomLeft;
                    animationSprite.Origin = Anchor.BottomRight;
                    animationSprite.X = 0.05f;
                }
                else
                {
                    animationSprite.Anchor = Anchor.BottomRight;
                    animationSprite.Origin = Anchor.BottomLeft;
                    animationSprite.X = -0.05f;
                }

                // 设置旋转角度
                animationSprite.Rotation = rotationAngle;

                // 缩放到适合舞台高度
                if (animationSprite.Height > 0)
                    animationSprite.Scale = new Vector2(1, stageHeight / animationSprite.Height);

                // 清除之前的子元素并添加新的动画
                ClearInternal();
                AddInternal(animationSprite);

                System.Console.WriteLine($"[DEBUG] FXAnimationInstance: Setup {(isLeft ? "left" : "right")} animation at {rotationAngle:F1}°");
            }
            else
            {
                // 如果没有动画，使用调试用的Box
                ClearInternal();
                AddInternal(new Box
                {
                    Anchor = isLeft ? Anchor.BottomLeft : Anchor.BottomRight,
                    Origin = isLeft ? Anchor.BottomRight : Anchor.BottomLeft,
                    X = isLeft ? 0.05f : -0.05f,
                    Width = 10,
                    Height = 100,
                    Colour = isLeft ? Color4.Red : Color4.Blue,
                    Alpha = 0.5f,
                    Rotation = rotationAngle
                });
                System.Console.WriteLine($"[DEBUG] FXAnimationInstance: Using fallback box for {(isLeft ? "left" : "right")} side");
            }
        }

        protected override void PrepareForUse()
        {
            base.PrepareForUse();

            const int fx_fade_in_duration = 50;   // 快速淡入
            const int fx_animation_duration = 1000; // 动画持续时间
            const int fx_fade_out_duration = 300;  // 淡出时间

            // 重置状态
            Alpha = 0;
            Scale = Vector2.Zero;

            // 独立的动画序列：淡入 → 保持 → 淡出
            this
                .FadeTo(0.8f, fx_fade_in_duration, Easing.OutQuint)          // 快速淡入
                .ScaleTo(1.0f, fx_fade_in_duration, Easing.OutQuint)         // 快速缩放到正常大小
                .Then()
                .Delay(fx_animation_duration - fx_fade_in_duration - fx_fade_out_duration) // 保持可见
                .FadeOut(fx_fade_out_duration, Easing.InQuint)               // 淡出
                .ScaleTo(0.8f, fx_fade_out_duration, Easing.InQuint)         // 稍微缩小
                .Expire(); // 动画结束后自动回收到池中

            // 如果动画精灵是纹理动画，启动播放
            if (animationSprite is LegacySkinExtensions.SkinnableTextureAnimation textureAnim)
            {
                textureAnim.Loop = false;  // 不循环
                textureAnim.IsPlaying = false;
                textureAnim.GotoFrame(0);   // 回到第一帧
                textureAnim.IsPlaying = true;  // 开始播放

                System.Console.WriteLine($"[DEBUG] FXAnimationInstance: Started texture animation for {(isLeft ? "left" : "right")} side");
            }

            System.Console.WriteLine($"[DEBUG] FXAnimationInstance: PrepareForUse - {(isLeft ? "left" : "right")} side prepared");
        }

        protected override void FreeAfterUse()
        {
            base.FreeAfterUse();

            // 清理资源
            if (animationSprite is LegacySkinExtensions.SkinnableTextureAnimation textureAnim)
            {
                textureAnim.IsPlaying = false;
            }

            // 不要设置 InternalChild = null，这会导致崩溃
            // 只需要清理引用即可，对象池会处理剩余的清理工作
            animationSprite = null;

            System.Console.WriteLine($"[DEBUG] FXAnimationInstance: FreeAfterUse - {(isLeft ? "left" : "right")} side freed");
        }
    }
}
