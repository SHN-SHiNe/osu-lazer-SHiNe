// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics.Backgrounds;
using osu.Game.Graphics;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Backgrounds
{
    /// <summary>
    /// Manages dual background layers for Mania mode - one for the main background and one for the stage area.
    /// </summary>
    public partial class DualBackgroundManager : BackgroundScreen
    {
        private BackgroundScreenBeatmap.DimmableBackground mainBackground;
        private BackgroundScreenBeatmap_Mania.DimmableBackground maniaBackground;
        private object maniaPlayfield; // Using object to avoid circular dependency
        private WorkingBeatmap beatmap;

        [Resolved]
        private OsuConfigManager config { get; set; }

        private readonly Bindable<bool> enableManiaBackground = new Bindable<bool>();
        private readonly Bindable<double> maniaBackgroundDimLevel = new Bindable<double>();
        private readonly Bindable<double> maniaBackgroundBlurLevel = new Bindable<double>();
        private readonly Bindable<double> maniaBackgroundPositionX = new Bindable<double>();
        private readonly Bindable<double> maniaBackgroundPositionY = new Bindable<double>();
        private readonly Bindable<double> maniaBackgroundWidth = new Bindable<double>();
        private readonly Bindable<double> maniaBackgroundHeight = new Bindable<double>();
        private readonly Bindable<double> maniaBackgroundCornerRadius = new Bindable<double>();
        private readonly Bindable<bool> maniaBackgroundEdgeEffectEnabled = new Bindable<bool>();
        private readonly Bindable<double> maniaBackgroundEdgeEffectType = new Bindable<double>();
        private readonly Bindable<double> maniaBackgroundEdgeEffectRadius = new Bindable<double>();
        private readonly Bindable<double> maniaBackgroundEdgeEffectOpacity = new Bindable<double>();

        public WorkingBeatmap Beatmap
        {
            get => beatmap;
            set
            {
                beatmap = value;
                if (IsLoaded)
                    LoadBackground();
            }
        }

        public object ManiaPlayfield
        {
            get => maniaPlayfield;
            set
            {
                maniaPlayfield = value;
                // Update masking when playfield changes
                if (maniaBackground != null)
                    updateManiaMasking();
            }
        }

        public DualBackgroundManager()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Logger.Log($"DualBackgroundManager: Loading with config instance: {config?.GetHashCode()}");
            Logger.Log($"DualBackgroundManager: Config type: {config?.GetType().FullName}");

            // Create main background (always present)
            AddInternal(mainBackground = new BackgroundScreenBeatmap.DimmableBackground
            {
                RelativeSizeAxes = Axes.Both,
            });

            // Create Mania background directly
            AddInternal(maniaBackground = new BackgroundScreenBeatmap_Mania.DimmableBackground
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = 1, // Initially visible for immediate dual background effect
            });

            // Bind to configuration
            Logger.Log("DualBackgroundManager: Binding to configuration...");
            enableManiaBackground.BindTo(config.GetBindable<bool>(OsuSetting.EnableManiaBackgroundLayer));
            maniaBackgroundDimLevel.BindTo(config.GetBindable<double>(OsuSetting.ManiaBackgroundDimLevel));
            maniaBackgroundBlurLevel.BindTo(config.GetBindable<double>(OsuSetting.ManiaBackgroundBlurLevel));
            maniaBackgroundPositionX.BindTo(config.GetBindable<double>(OsuSetting.ManiaBackgroundPositionX));
            maniaBackgroundPositionY.BindTo(config.GetBindable<double>(OsuSetting.ManiaBackgroundPositionY));
            maniaBackgroundWidth.BindTo(config.GetBindable<double>(OsuSetting.ManiaBackgroundWidth));
            maniaBackgroundHeight.BindTo(config.GetBindable<double>(OsuSetting.ManiaBackgroundHeight));
            maniaBackgroundCornerRadius.BindTo(config.GetBindable<double>(OsuSetting.ManiaBackgroundCornerRadius));
            maniaBackgroundEdgeEffectEnabled.BindTo(config.GetBindable<bool>(OsuSetting.ManiaBackgroundEdgeEffectEnabled));
            maniaBackgroundEdgeEffectType.BindTo(config.GetBindable<double>(OsuSetting.ManiaBackgroundEdgeEffectType));
            maniaBackgroundEdgeEffectRadius.BindTo(config.GetBindable<double>(OsuSetting.ManiaBackgroundEdgeEffectRadius));
            maniaBackgroundEdgeEffectOpacity.BindTo(config.GetBindable<double>(OsuSetting.ManiaBackgroundEdgeEffectOpacity));

            // Debug: Check current values after binding
            Logger.Log($"DualBackgroundManager: After binding - Blur: {maniaBackgroundBlurLevel.Value}, Dim: {maniaBackgroundDimLevel.Value}, Width: {maniaBackgroundWidth.Value}");
            Logger.Log("DualBackgroundManager: Configuration binding completed");

            Logger.Log("DualBackgroundManager: Setting up event handlers...");
            enableManiaBackground.ValueChanged += onManiaBackgroundToggled;
            maniaBackgroundDimLevel.ValueChanged += onManiaBackgroundSettingsChanged;
            maniaBackgroundBlurLevel.ValueChanged += onManiaBackgroundSettingsChanged;
            maniaBackgroundPositionX.ValueChanged += onManiaBackgroundPositionChanged;
            maniaBackgroundPositionY.ValueChanged += onManiaBackgroundPositionChanged;
            maniaBackgroundWidth.ValueChanged += onManiaBackgroundPositionChanged;
            maniaBackgroundHeight.ValueChanged += onManiaBackgroundPositionChanged;
            maniaBackgroundCornerRadius.ValueChanged += onManiaBackgroundPositionChanged;
            maniaBackgroundEdgeEffectEnabled.ValueChanged += onManiaBackgroundEdgeEffectChanged;
            maniaBackgroundEdgeEffectType.ValueChanged += onManiaBackgroundEdgeEffectChanged;
            maniaBackgroundEdgeEffectRadius.ValueChanged += onManiaBackgroundEdgeEffectChanged;
            maniaBackgroundEdgeEffectOpacity.ValueChanged += onManiaBackgroundEdgeEffectChanged;
            Logger.Log("DualBackgroundManager: Event handlers setup completed");

            // Debug: Manually trigger updates to test if event handlers work
            Logger.Log("DualBackgroundManager: Testing event handlers...");
            onManiaBackgroundSettingsChanged(new ValueChangedEvent<double>(0, maniaBackgroundDimLevel.Value));
            onManiaBackgroundPositionChanged(new ValueChangedEvent<double>(0, maniaBackgroundPositionX.Value));

            // Debug: Add value change logging to verify bindings work
            maniaBackgroundBlurLevel.ValueChanged += (e) => Logger.Log($"DualBackgroundManager: maniaBackgroundBlurLevel changed to {e.NewValue}");
            maniaBackgroundDimLevel.ValueChanged += (e) => Logger.Log($"DualBackgroundManager: maniaBackgroundDimLevel changed to {e.NewValue}");
            maniaBackgroundPositionX.ValueChanged += (e) => Logger.Log($"DualBackgroundManager: maniaBackgroundPositionX changed to {e.NewValue}");

            // Load background if beatmap is already set
            if (beatmap != null)
                LoadBackground();
        }

        private void LoadBackground()
        {
            if (beatmap == null)
            {
                Logger.Log("DualBackgroundManager: Beatmap is null, cannot load background");
                return;
            }

            Logger.Log($"DualBackgroundManager: Loading background with file: {beatmap.Metadata.BackgroundFile}");

            // Create background for main layer using the same method as BackgroundScreenBeatmap
            var mainBg = new AudioReactiveBeatmapBackground(beatmap);
            mainBackground.Background = mainBg;
            Logger.Log("DualBackgroundManager: Main background added");

            // Create background for Mania layer using the same method as BackgroundScreenBeatmap
            var maniaBg = new AudioReactiveBeatmapBackground(beatmap);
            maniaBackground.Background = maniaBg;
            Logger.Log("DualBackgroundManager: Mania background added");

            // Force Mania background to be visible and log its state
            maniaBackground.Alpha = 1.0f;
            maniaBackground.AlwaysPresent = true;

            // Apply visual effects based on user settings
            maniaBackground.BlurAmount.Value = (float)maniaBackgroundBlurLevel.Value * 20.0f; // Scale to reasonable blur range
            maniaBackground.IgnoreUserSettings.Value = true; // Use our custom settings instead of global ones

            // Apply dim level using the ignored dim level property
            maniaBackground.DimWhenUserSettingsIgnored.Value = (float)maniaBackgroundDimLevel.Value;

            Logger.Log($"DualBackgroundManager: Mania background forced visible - Alpha: {maniaBackground.Alpha}, AlwaysPresent: {maniaBackground.AlwaysPresent}, Blur: {maniaBackground.BlurAmount.Value}, Dim: {maniaBackgroundDimLevel.Value:F2}, IgnoredDim: {maniaBackground.DimWhenUserSettingsIgnored.Value}");

            // Update masking
            updateManiaMasking();
        }

        private void updateManiaMasking()
        {
            if (maniaPlayfield == null)
            {
                Logger.Log("DualBackgroundManager: ManiaPlayfield is null, cannot update masking");
                return;
            }

            try
            {
                // Use reflection to access ManiaPlayfield properties
                var playfieldType = maniaPlayfield.GetType();
                var skinnableComponentProperty = playfieldType.GetProperty("SkinnableComponentScreenSpaceDrawQuad");

                if (skinnableComponentProperty != null)
                {
                    // Calculate the stage area bounds
                    var stageBounds = (osu.Framework.Graphics.Primitives.Quad)skinnableComponentProperty.GetValue(maniaPlayfield);
                    Logger.Log($"DualBackgroundManager: Raw stage bounds - TopLeft: {stageBounds.TopLeft}, BottomRight: {stageBounds.BottomRight}, Size: {stageBounds.Size}");

                    // Convert screen space bounds to local space
                    var localBounds = ToLocalSpace(stageBounds);
                    Logger.Log($"DualBackgroundManager: Local bounds - TopLeft: {localBounds.TopLeft}, BottomRight: {localBounds.BottomRight}, Size: {localBounds.Size}");

                    // Update the Mania background to only show the stage area
                    if (localBounds.Size.X > 0 && localBounds.Size.Y > 0)
                    {
                        // Use actual stage bounds
                        maniaBackground.Position = localBounds.TopLeft;
                        maniaBackground.Size = localBounds.Size;
                        maniaBackground.RelativeSizeAxes = Axes.None; // Use absolute positioning
                        Logger.Log($"DualBackgroundManager: Mania masking updated - Position: {maniaBackground.Position}, Size: {maniaBackground.Size}, RelativeSizeAxes: {maniaBackground.RelativeSizeAxes}");
                    }
                    else
                    {
                        // Try to get the playfield's actual size and position
                        var drawableProperty = playfieldType.GetProperty("DrawSize");
                        var positionProperty = playfieldType.GetProperty("Position");

                        if (drawableProperty != null && positionProperty != null)
                        {
                            var drawSize = (Vector2)drawableProperty.GetValue(maniaPlayfield);
                            var position = (Vector2)positionProperty.GetValue(maniaPlayfield);

                            Logger.Log($"DualBackgroundManager: Playfield DrawSize: {drawSize}, Position: {position}");

                            if (drawSize.X > 0 && drawSize.Y > 0)
                            {
                                // Check if playfield size is reasonable (not full screen)
                                if (drawSize.X < 1000 && drawSize.Y < 600) // Reasonable stage area size
                                {
                                    // Use playfield size and position
                                    maniaBackground.Position = position;
                                    maniaBackground.Size = drawSize;
                                    maniaBackground.RelativeSizeAxes = Axes.None;
                                    Logger.Log($"DualBackgroundManager: Using playfield bounds - Position: {maniaBackground.Position}, Size: {maniaBackground.Size}");
                                }
                                else
                                {
                                    // Playfield size is too large (full screen), use slider settings
                                    Logger.Log($"DualBackgroundManager: Playfield size too large ({drawSize}), using slider settings");

                                    // Calculate the actual position considering the width for proper centering
                                    float actualPositionX = (float)(maniaBackgroundPositionX.Value - maniaBackgroundWidth.Value / 2.0);
                                    actualPositionX = Math.Max(0, actualPositionX);

                                    maniaBackground.Position = new Vector2(actualPositionX, (float)maniaBackgroundPositionY.Value);
                                    maniaBackground.Size = new Vector2((float)maniaBackgroundWidth.Value, (float)maniaBackgroundHeight.Value);
                                    maniaBackground.RelativeSizeAxes = Axes.Both;
                                    maniaBackground.RelativePositionAxes = Axes.Both;

                                    // Update corner radius
                                    float cornerRadius = (float)maniaBackgroundCornerRadius.Value * Math.Min(maniaBackground.Size.X, maniaBackground.Size.Y);
                                    maniaBackground.CornerRadius = cornerRadius;

                                    // Update edge effect
                                    updateManiaBackgroundEdgeEffect();

                                    Logger.Log($"DualBackgroundManager: Using slider settings - Center X: {maniaBackgroundPositionX.Value:F2}, Actual X: {actualPositionX:F2}, Position: {maniaBackground.Position}, Size: {maniaBackground.Size}, RelativeSizeAxes: {maniaBackground.RelativeSizeAxes}, RelativePositionAxes: {maniaBackground.RelativePositionAxes}, CornerRadius: {maniaBackgroundCornerRadius.Value:F2} (actual: {cornerRadius:F2})");
                                }
                            }
                            else
                            {
                                // Use slider settings when playfield size is zero
                                Logger.Log("DualBackgroundManager: Playfield size is zero, using slider settings");

                                // Calculate the actual position considering the width for proper centering
                                float actualPositionX = (float)(maniaBackgroundPositionX.Value - maniaBackgroundWidth.Value / 2.0);
                                actualPositionX = Math.Max(0, actualPositionX);

                                maniaBackground.Position = new Vector2(actualPositionX, (float)maniaBackgroundPositionY.Value);
                                maniaBackground.Size = new Vector2((float)maniaBackgroundWidth.Value, (float)maniaBackgroundHeight.Value);
                                maniaBackground.RelativeSizeAxes = Axes.Both;
                                maniaBackground.RelativePositionAxes = Axes.Both;

                                // Update corner radius
                                float cornerRadius = (float)maniaBackgroundCornerRadius.Value * Math.Min(maniaBackground.Size.X, maniaBackground.Size.Y);
                                maniaBackground.CornerRadius = cornerRadius;

                                // Update edge effect
                                updateManiaBackgroundEdgeEffect();

                                Logger.Log($"DualBackgroundManager: Using slider settings - Center X: {maniaBackgroundPositionX.Value:F2}, Actual X: {actualPositionX:F2}, Position: {maniaBackground.Position}, Size: {maniaBackground.Size}, RelativeSizeAxes: {maniaBackground.RelativeSizeAxes}, RelativePositionAxes: {maniaBackground.RelativePositionAxes}, CornerRadius: {maniaBackgroundCornerRadius.Value:F2} (actual: {cornerRadius:F2})");
                            }
                        }
                        else
                        {
                            // Use slider settings when properties are not found
                            Logger.Log("DualBackgroundManager: Properties not found, using slider settings");

                            // Calculate the actual position considering the width for proper centering
                            float actualPositionX = (float)(maniaBackgroundPositionX.Value - maniaBackgroundWidth.Value / 2.0);
                            actualPositionX = Math.Max(0, actualPositionX);

                            maniaBackground.Position = new Vector2(actualPositionX, (float)maniaBackgroundPositionY.Value);
                            maniaBackground.Size = new Vector2((float)maniaBackgroundWidth.Value, (float)maniaBackgroundHeight.Value);
                            maniaBackground.RelativeSizeAxes = Axes.Both;
                            maniaBackground.RelativePositionAxes = Axes.Both;

                            // Update corner radius
                            float cornerRadius = (float)maniaBackgroundCornerRadius.Value * Math.Min(maniaBackground.Size.X, maniaBackground.Size.Y);
                            maniaBackground.CornerRadius = cornerRadius;

                            // Update edge effect
                            updateManiaBackgroundEdgeEffect();

                            Logger.Log($"DualBackgroundManager: Using slider settings - Center X: {maniaBackgroundPositionX.Value:F2}, Actual X: {actualPositionX:F2}, Position: {maniaBackground.Position}, Size: {maniaBackground.Size}, RelativeSizeAxes: {maniaBackground.RelativeSizeAxes}, RelativePositionAxes: {maniaBackground.RelativePositionAxes}, CornerRadius: {maniaBackgroundCornerRadius.Value:F2} (actual: {cornerRadius:F2})");
                        }
                    }
                }
                else
                {
                    Logger.Log("DualBackgroundManager: SkinnableComponentScreenSpaceDrawQuad property not found");
                }
            }
            catch (Exception ex)
            {
                // If reflection fails, disable masking
                maniaBackground.Masking = false;
                Logger.Log($"DualBackgroundManager: Masking update failed: {ex.Message}");
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Set initial state
            onManiaBackgroundToggled(new ValueChangedEvent<bool>(false, enableManiaBackground.Value));
        }

        private void onManiaBackgroundToggled(ValueChangedEvent<bool> e)
        {
            if (maniaBackground == null) return;

            if (e.NewValue)
            {
                maniaBackground.FadeIn(500, Easing.OutQuint);
                updateManiaMasking();
            }
            else
            {
                maniaBackground.FadeOut(500, Easing.OutQuint);
            }
        }

        private void onManiaBackgroundSettingsChanged(ValueChangedEvent<double> e)
        {
            if (maniaBackground == null) return;

            Logger.Log($"DualBackgroundManager: onManiaBackgroundSettingsChanged triggered - NewValue: {e.NewValue}");

            // Update blur amount
            maniaBackground.BlurAmount.Value = (float)maniaBackgroundBlurLevel.Value * 20.0f;

            // Update dim level by setting the ignored dim level (when IgnoreUserSettings is true)
            maniaBackground.DimWhenUserSettingsIgnored.Value = (float)maniaBackgroundDimLevel.Value;

            Logger.Log($"DualBackgroundManager: Settings updated - Blur: {maniaBackground.BlurAmount.Value}, Dim: {maniaBackgroundDimLevel.Value:F2}, IgnoredDim: {maniaBackground.DimWhenUserSettingsIgnored.Value}");
        }

        private void onManiaBackgroundPositionChanged(ValueChangedEvent<double> e)
        {
            if (maniaBackground == null) return;

            Logger.Log($"DualBackgroundManager: onManiaBackgroundPositionChanged triggered - NewValue: {e.NewValue}");

            // Calculate the actual position considering the width for proper centering
            // PositionX represents the center position, so we need to offset by half the width
            float actualPositionX = (float)(maniaBackgroundPositionX.Value - maniaBackgroundWidth.Value / 2.0);

            // Ensure the position doesn't go below 0
            actualPositionX = Math.Max(0, actualPositionX);

            // Update position and size based on slider values
            // When using RelativeSizeAxes = Axes.Both, Position also needs to be relative
            maniaBackground.Position = new Vector2(actualPositionX, (float)maniaBackgroundPositionY.Value);
            maniaBackground.Size = new Vector2((float)maniaBackgroundWidth.Value, (float)maniaBackgroundHeight.Value);
            maniaBackground.RelativeSizeAxes = Axes.Both;
            maniaBackground.RelativePositionAxes = Axes.Both; // Enable relative positioning

            // Update corner radius - scale by the smaller dimension for consistent appearance
            float cornerRadius = (float)maniaBackgroundCornerRadius.Value * Math.Min(maniaBackground.Size.X, maniaBackground.Size.Y);
            maniaBackground.CornerRadius = cornerRadius;

            // Update edge effect
            updateManiaBackgroundEdgeEffect();

            Logger.Log($"DualBackgroundManager: Position updated - Center X: {maniaBackgroundPositionX.Value:F2}, Actual X: {actualPositionX:F2}, Y: {maniaBackgroundPositionY.Value:F2}, Width: {maniaBackgroundWidth.Value:F2}, Height: {maniaBackgroundHeight.Value:F2}, CornerRadius: {maniaBackgroundCornerRadius.Value:F2} (actual: {cornerRadius:F2})");
        }

        private void onManiaBackgroundEdgeEffectChanged(ValueChangedEvent<double> e)
        {
            if (maniaBackground == null) return;

            updateManiaBackgroundEdgeEffect();
        }

        private void onManiaBackgroundEdgeEffectChanged(ValueChangedEvent<bool> e)
        {
            if (maniaBackground == null) return;

            updateManiaBackgroundEdgeEffect();
        }

        private void updateManiaBackgroundEdgeEffect()
        {
            if (maniaBackground == null) return;

            if (maniaBackgroundEdgeEffectEnabled.Value)
            {
                // Enable masking for edge effects to work
                maniaBackground.Masking = true;

                var edgeEffectType = maniaBackgroundEdgeEffectType.Value == 0 ? EdgeEffectType.Shadow : EdgeEffectType.Glow;
                var edgeColor = edgeEffectType == EdgeEffectType.Shadow
                    ? new Color4(0, 0, 0, (float)maniaBackgroundEdgeEffectOpacity.Value)
                    : new Color4(1, 1, 1, (float)maniaBackgroundEdgeEffectOpacity.Value);

                maniaBackground.EdgeEffect = new EdgeEffectParameters
                {
                    Type = edgeEffectType,
                    Colour = edgeColor,
                    Radius = (float)maniaBackgroundEdgeEffectRadius.Value,
                    Hollow = false,
                };

                Logger.Log($"DualBackgroundManager: Edge effect updated - Type: {edgeEffectType}, Color: {edgeColor}, Radius: {maniaBackgroundEdgeEffectRadius.Value:F2}, Opacity: {maniaBackgroundEdgeEffectOpacity.Value:F2}, Masking: {maniaBackground.Masking}");
            }
            else
            {
                maniaBackground.EdgeEffect = new EdgeEffectParameters();
                // Keep masking enabled for corner radius to work
                maniaBackground.Masking = true;
                Logger.Log("DualBackgroundManager: Edge effect disabled, masking kept enabled for corner radius");
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            if (enableManiaBackground != null)
                enableManiaBackground.ValueChanged -= onManiaBackgroundToggled;
            if (maniaBackgroundDimLevel != null)
                maniaBackgroundDimLevel.ValueChanged -= onManiaBackgroundSettingsChanged;
            if (maniaBackgroundBlurLevel != null)
                maniaBackgroundBlurLevel.ValueChanged -= onManiaBackgroundSettingsChanged;
            if (maniaBackgroundPositionX != null)
                maniaBackgroundPositionX.ValueChanged -= onManiaBackgroundPositionChanged;
            if (maniaBackgroundPositionY != null)
                maniaBackgroundPositionY.ValueChanged -= onManiaBackgroundPositionChanged;
            if (maniaBackgroundWidth != null)
                maniaBackgroundWidth.ValueChanged -= onManiaBackgroundPositionChanged;
            if (maniaBackgroundHeight != null)
                maniaBackgroundHeight.ValueChanged -= onManiaBackgroundPositionChanged;
        }
    }
}
