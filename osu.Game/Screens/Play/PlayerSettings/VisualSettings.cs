// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Game.Configuration;
using osu.Game.Localisation;

namespace osu.Game.Screens.Play.PlayerSettings
{
    public partial class VisualSettings : PlayerSettingsGroup
    {
        private readonly PlayerSliderBar<double> dimSliderBar;
        private readonly PlayerSliderBar<double> blurSliderBar;
        private readonly PlayerSliderBar<float> comboColourNormalisationSliderBar;
        private readonly PlayerCheckbox showStoryboardToggle;
        private readonly PlayerCheckbox beatmapSkinsToggle;
        private readonly PlayerCheckbox beatmapColorsToggle;

        // Mania background settings
        private readonly PlayerSliderBar<double> maniaBackgroundDimSliderBar;
        private readonly PlayerSliderBar<double> maniaBackgroundBlurSliderBar;
        private readonly PlayerSliderBar<double> maniaBackgroundPositionXSliderBar;
        private readonly PlayerSliderBar<double> maniaBackgroundPositionYSliderBar;
        private readonly PlayerSliderBar<double> maniaBackgroundWidthSliderBar;
        private readonly PlayerSliderBar<double> maniaBackgroundHeightSliderBar;
        private readonly PlayerSliderBar<double> maniaStagePositionXSliderBar;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private bool isManiaMode;

        public VisualSettings()
            : base("Visual Settings")
        {
            Children = new Drawable[]
            {
                dimSliderBar = new PlayerSliderBar<double>
                {
                    LabelText = GameplaySettingsStrings.BackgroundDim,
                    DisplayAsPercentage = true
                },
                blurSliderBar = new PlayerSliderBar<double>
                {
                    LabelText = GameplaySettingsStrings.BackgroundBlur,
                    DisplayAsPercentage = true
                },
                showStoryboardToggle = new PlayerCheckbox { LabelText = GraphicsSettingsStrings.StoryboardVideo },
                beatmapSkinsToggle = new PlayerCheckbox { LabelText = SkinSettingsStrings.BeatmapSkins },
                beatmapColorsToggle = new PlayerCheckbox { LabelText = SkinSettingsStrings.BeatmapColours },
                comboColourNormalisationSliderBar = new PlayerSliderBar<float>
                {
                    LabelText = GraphicsSettingsStrings.ComboColourNormalisation,
                    DisplayAsPercentage = true,
                },

                // Mania background settings (will be shown/hidden based on ruleset)
                maniaBackgroundDimSliderBar = new PlayerSliderBar<double>
                {
                    LabelText = "Mania背景变暗",
                    DisplayAsPercentage = true
                },
                maniaBackgroundBlurSliderBar = new PlayerSliderBar<double>
                {
                    LabelText = "Mania背景模糊",
                    DisplayAsPercentage = true
                },
                maniaBackgroundPositionXSliderBar = new PlayerSliderBar<double>
                {
                    LabelText = "Mania背景水平位置",
                    DisplayAsPercentage = true
                },
                maniaBackgroundPositionYSliderBar = new PlayerSliderBar<double>
                {
                    LabelText = "Mania背景垂直位置",
                    DisplayAsPercentage = true
                },
                maniaBackgroundWidthSliderBar = new PlayerSliderBar<double>
                {
                    LabelText = "Mania背景宽度",
                    DisplayAsPercentage = true
                },
                maniaBackgroundHeightSliderBar = new PlayerSliderBar<double>
                {
                    LabelText = "Mania背景高度",
                    DisplayAsPercentage = true
                },
                maniaStagePositionXSliderBar = new PlayerSliderBar<double>
                {
                    LabelText = "Mania舞台水平位置"
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Logger.Log($"VisualSettings: Loading with config instance: {config.GetHashCode()}");
            Logger.Log($"VisualSettings: Config type: {config.GetType().FullName}");

            dimSliderBar.Current = config.GetBindable<double>(OsuSetting.DimLevel);
            blurSliderBar.Current = config.GetBindable<double>(OsuSetting.BlurLevel);
            showStoryboardToggle.Current = config.GetBindable<bool>(OsuSetting.ShowStoryboard);
            beatmapSkinsToggle.Current = config.GetBindable<bool>(OsuSetting.BeatmapSkins);
            beatmapColorsToggle.Current = config.GetBindable<bool>(OsuSetting.BeatmapColours);
            comboColourNormalisationSliderBar.Current = config.GetBindable<float>(OsuSetting.ComboColourNormalisationAmount);

            // Mania background settings
            maniaBackgroundDimSliderBar.Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundDimLevel);
            maniaBackgroundBlurSliderBar.Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundBlurLevel);
            maniaBackgroundPositionXSliderBar.Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundPositionX);
            maniaBackgroundPositionYSliderBar.Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundPositionY);
            maniaBackgroundWidthSliderBar.Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundWidth);
            maniaBackgroundHeightSliderBar.Current = config.GetBindable<double>(OsuSetting.ManiaBackgroundHeight);
            maniaStagePositionXSliderBar.Current = config.GetBindable<double>(OsuSetting.ManiaStagePositionX);

            // Add debug logging for Mania settings changes
            maniaBackgroundDimSliderBar.Current.ValueChanged += v => Logger.Log($"VisualSettings: ManiaBackgroundDimLevel changed to {v.NewValue}");
            maniaBackgroundBlurSliderBar.Current.ValueChanged += v => Logger.Log($"VisualSettings: ManiaBackgroundBlurLevel changed to {v.NewValue}");
            maniaBackgroundPositionXSliderBar.Current.ValueChanged += v => Logger.Log($"VisualSettings: ManiaBackgroundPositionX changed to {v.NewValue}");
            maniaBackgroundPositionYSliderBar.Current.ValueChanged += v => Logger.Log($"VisualSettings: ManiaBackgroundPositionY changed to {v.NewValue}");
            maniaBackgroundWidthSliderBar.Current.ValueChanged += v => Logger.Log($"VisualSettings: ManiaBackgroundWidth changed to {v.NewValue}");
            maniaBackgroundHeightSliderBar.Current.ValueChanged += v => Logger.Log($"VisualSettings: ManiaBackgroundHeight changed to {v.NewValue}");
        }

        /// <summary>
        /// Sets whether the current ruleset is Mania mode, affecting which settings are visible.
        /// </summary>
        public void SetManiaMode(bool isMania)
        {
            isManiaMode = isMania;

            // Show/hide Mania-specific settings
            maniaBackgroundDimSliderBar.Alpha = isMania ? 1 : 0;
            maniaBackgroundBlurSliderBar.Alpha = isMania ? 1 : 0;
            maniaBackgroundPositionXSliderBar.Alpha = isMania ? 1 : 0;
            maniaBackgroundPositionYSliderBar.Alpha = isMania ? 1 : 0;
            maniaBackgroundWidthSliderBar.Alpha = isMania ? 1 : 0;
            maniaBackgroundHeightSliderBar.Alpha = isMania ? 1 : 0;
            maniaStagePositionXSliderBar.Alpha = isMania ? 1 : 0;
        }
    }
}
