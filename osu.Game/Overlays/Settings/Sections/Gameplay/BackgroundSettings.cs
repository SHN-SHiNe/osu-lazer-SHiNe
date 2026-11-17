// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.Gameplay
{
    public partial class BackgroundSettings : SettingsSubsection
    {
        protected override LocalisableString Header => GameplaySettingsStrings.BackgroundHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsSlider<double>
                {
                    LabelText = GameplaySettingsStrings.BackgroundDim,
                    Current = config.GetBindable<double>(OsuSetting.DimLevel),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
                new SettingsSlider<double>
                {
                    LabelText = GameplaySettingsStrings.BackgroundBlur,
                    Current = config.GetBindable<double>(OsuSetting.BlurLevel),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
                new SettingsCheckbox
                {
                    LabelText = GameplaySettingsStrings.LightenDuringBreaks,
                    Current = config.GetBindable<bool>(OsuSetting.LightenDuringBreaks),
                    Keywords = new[] { "dim", "level" }
                },
                new SettingsCheckbox
                {
                    LabelText = GameplaySettingsStrings.FadePlayfieldWhenHealthLow,
                    Current = config.GetBindable<bool>(OsuSetting.FadePlayfieldWhenHealthLow),
                },
                new SettingsSlider<float>
                {
                    LabelText = "音频反应震动强度",
                    TooltipText = "控制背景随音乐震动的强度",
                    Current = config.GetBindable<float>(OsuSetting.AudioReactiveShakeIntensity),
                    KeyboardStep = 0.1f,
                },
                new SettingsSlider<float>
                {
                    LabelText = "音频反应放大强度",
                    TooltipText = "控制背景随音乐放大的强度",
                    Current = config.GetBindable<float>(OsuSetting.AudioReactiveScaleMultiplier),
                    KeyboardStep = 0.1f,
                },
                new SettingsSlider<float>
                {
                    LabelText = "震动频率范围 - 最小值",
                    TooltipText = "控制震动效果频率范围的最小值 (1-50)",
                    Current = config.GetBindable<float>(OsuSetting.AudioReactiveShakeFrequencyMin),
                    KeyboardStep = 1.0f,
                },
                new SettingsSlider<float>
                {
                    LabelText = "震动频率范围 - 最大值",
                    TooltipText = "控制震动效果频率范围的最大值 (10-100)",
                    Current = config.GetBindable<float>(OsuSetting.AudioReactiveShakeFrequencyMax),
                    KeyboardStep = 1.0f,
                },
                new SettingsSlider<float>
                {
                    LabelText = "放大频率范围 - 最小值",
                    TooltipText = "控制放大效果频率范围的最小值 (1-50)",
                    Current = config.GetBindable<float>(OsuSetting.AudioReactiveScaleFrequencyMin),
                    KeyboardStep = 1.0f,
                },
                new SettingsSlider<float>
                {
                    LabelText = "放大频率范围 - 最大值",
                    TooltipText = "控制放大效果频率范围的最大值 (10-100)",
                    Current = config.GetBindable<float>(OsuSetting.AudioReactiveScaleFrequencyMax),
                    KeyboardStep = 1.0f,
                },
                new SettingsSlider<float>
                {
                    LabelText = "震动触发阈值",
                    TooltipText = "控制震动效果的触发敏感度 (0.01-0.3)",
                    Current = config.GetBindable<float>(OsuSetting.AudioReactiveShakeThreshold),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
                new SettingsSlider<float>
                {
                    LabelText = "放大触发阈值",
                    TooltipText = "控制放大效果的触发敏感度 (0.01-0.3)",
                    Current = config.GetBindable<float>(OsuSetting.AudioReactiveScaleThreshold),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
                new SettingsSlider<float>
                {
                    LabelText = "放大恢复速度",
                    TooltipText = "控制放大效果的恢复速度 (0.5-0.99，越大恢复越慢)",
                    Current = config.GetBindable<float>(OsuSetting.AudioReactiveScaleDecayRate),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
                new SettingsSlider<float>
                {
                    LabelText = "震动恢复速度",
                    TooltipText = "控制震动效果的恢复速度 (0.5-0.99，越大恢复越慢)",
                    Current = config.GetBindable<float>(OsuSetting.AudioReactiveShakeDecayRate),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
            };
        }
    }
}
