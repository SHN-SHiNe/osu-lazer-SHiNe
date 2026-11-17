// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Game.Configuration;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.Gameplay
{
    public partial class ManiaBackgroundSettings : SettingsSubsection
    {
        protected override LocalisableString Header => "Mania背景设置";

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Logger.Log("Mania背景设置: 正在加载设置界面");

            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = "启用Mania独立背景层",
                    TooltipText = "为Mania模式启用独立的背景层，可以单独控制舞台区域的背景效果",
                    Current = config.GetBindable<bool>(OsuSetting.EnableManiaBackgroundLayer),
                },
                new SettingsSlider<double>
                {
                    LabelText = "Mania背景暗化",
                    TooltipText = "控制Mania舞台区域背景的暗化程度",
                    Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundDimLevel),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
                new SettingsSlider<double>
                {
                    LabelText = "Mania背景模糊",
                    TooltipText = "控制Mania舞台区域背景的模糊程度",
                    Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundBlurLevel),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
                new SettingsSlider<double>
                {
                    LabelText = "Mania背景X位置",
                    TooltipText = "控制Mania背景层的水平中心位置 (0.0 = 左边缘, 1.0 = 右边缘)。背景会根据宽度自动居中显示。",
                    Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundPositionX),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
                new SettingsSlider<double>
                {
                    LabelText = "Mania背景Y位置",
                    TooltipText = "控制Mania背景层的垂直位置 (0.0 = 上边缘, 1.0 = 下边缘)",
                    Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundPositionY),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
                new SettingsSlider<double>
                {
                    LabelText = "Mania背景宽度",
                    TooltipText = "控制Mania背景层的宽度 (0.0 = 无宽度, 1.0 = 全屏宽度)",
                    Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundWidth),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
                new SettingsSlider<double>
                {
                    LabelText = "Mania背景高度",
                    TooltipText = "控制Mania背景层的高度 (0.0 = 无高度, 1.0 = 全屏高度)",
                    Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundHeight),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
                new SettingsSlider<double>
                {
                    LabelText = "Mania背景圆角",
                    TooltipText = "控制Mania背景层的圆角半径 (0.0 = 无圆角, 0.5 = 最大圆角)",
                    Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundCornerRadius),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
                new SettingsCheckbox
                {
                    LabelText = "启用Mania背景边缘羽化",
                    TooltipText = "为Mania背景层启用边缘羽化效果，可以创建柔和的边缘过渡",
                    Current = config.GetBindable<bool>(OsuSetting.ManiaBackgroundEdgeEffectEnabled),
                },
                new SettingsSlider<double>
                {
                    LabelText = "边缘羽化类型",
                    TooltipText = "选择边缘羽化效果类型 (0 = 阴影, 1 = 发光)",
                    Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundEdgeEffectType),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = false
                },
                new SettingsSlider<double>
                {
                    LabelText = "边缘羽化半径",
                    TooltipText = "控制边缘羽化的扩散半径 (0-50像素)",
                    Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundEdgeEffectRadius),
                    KeyboardStep = 0.1f,
                    DisplayAsPercentage = false
                },
                new SettingsSlider<double>
                {
                    LabelText = "边缘羽化透明度",
                    TooltipText = "控制边缘羽化效果的透明度 (0.0 = 完全透明, 1.0 = 完全不透明)",
                    Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundEdgeEffectOpacity),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
                new SettingsSlider<double>
                {
                    LabelText = "舞台抖动位移",
                    TooltipText = "控制Mania舞台抖动的垂直位移像素数 (0-50px)",
                    Current = config.GetBindable<double>(OsuSetting.ManiaStageShakeDisplacement),
                    KeyboardStep = 1.0f
                },
                new SettingsSlider<double>
                {
                    LabelText = "舞台抖动角度",
                    TooltipText = "控制Mania舞台抖动的随机旋转角度范围 (0-20°)",
                    Current = config.GetBindable<double>(OsuSetting.ManiaStageShakeRotation),
                    KeyboardStep = 0.5f
                },
                new SettingsCheckbox
                {
                    LabelText = "mania-stage-hint跟随抖动",
                    TooltipText = "让mania-stage-hint跟随mania-stage-bottom的抖动动画，抖动幅度完全一致",
                    Current = config.GetBindable<bool>(OsuSetting.ManiaStageHintFollowShake),
                },
                new SettingsSlider<double>
                {
                    LabelText = "舞台水平位置",
                    TooltipText = "控制Mania舞台区域的水平位置偏移 (-400px 到 +400px)",
                    Current = config.GetBindable<double>(OsuSetting.ManiaStagePositionX),
                    KeyboardStep = 1.0f
                },
                new SettingsSlider<double>
                {
                    LabelText = "舞台垂直位置",
                    TooltipText = "控制Mania舞台区域的垂直位置偏移 (-400px 到 +400px)",
                    Current = config.GetBindable<double>(OsuSetting.ManiaStagePositionY),
                    KeyboardStep = 1.0f
                },
            };
        }
    }
}
