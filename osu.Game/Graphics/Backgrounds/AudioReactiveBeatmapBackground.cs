// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Framework.Timing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.Graphics.Backgrounds;
using osu.Game.Screens.Play;
using osuTK;

namespace osu.Game.Graphics.Backgrounds
{
    /// <summary>
    /// A beatmap background that reacts to audio frequencies with scaling and shaking effects.
    /// </summary>
    public partial class AudioReactiveBeatmapBackground : BeatmapBackground, IBeatSyncProvider
    {
        private const float BASS_SCALE_MULTIPLIER = 0.3f;
        private const float MAX_SHAKE_OFFSET = 10.0f;

        private readonly Random random = new Random();
        private float currentBassScale = 1.0f;
        private Vector2 currentShakeOffset = Vector2.Zero;

        [Resolved]
        private IBeatSyncProvider beatSyncProvider { get; set; } = null!;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private readonly string fallbackTextureName;

        public AudioReactiveBeatmapBackground(WorkingBeatmap beatmap, string fallbackTextureName = @"Backgrounds/bg1")
            : base(beatmap, fallbackTextureName)
        {
            this.fallbackTextureName = fallbackTextureName;

            // Set anchor and origin to center for proper scaling
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(LargeTextureStore textures)
        {
            // Set the texture manually since we're overriding the base load method
            Sprite.Texture = Beatmap?.GetBackground() ?? textures.Get(fallbackTextureName);
        }

        protected override void Update()
        {
            base.Update();

            // Continuously update amplitude-based effects
            updateAmplitudeEffects();

            // Apply decay to current effects
            applyDecay();
        }

        private void updateAmplitudeEffects()
        {
            // Check if this is a null reference case
            if (beatSyncProvider?.CurrentAmplitudes == null)
            {
                return;
            }

            var amplitudes = beatSyncProvider.CurrentAmplitudes.FrequencyAmplitudes.Span;

            // Get configurable frequency ranges
            int shakeFreqMin, shakeFreqMax, scaleFreqMin, scaleFreqMax;
            try
            {
                shakeFreqMin = (int)config.Get<float>(OsuSetting.AudioReactiveShakeFrequencyMin);
                shakeFreqMax = (int)config.Get<float>(OsuSetting.AudioReactiveShakeFrequencyMax);
                scaleFreqMin = (int)config.Get<float>(OsuSetting.AudioReactiveScaleFrequencyMin);
                scaleFreqMax = (int)config.Get<float>(OsuSetting.AudioReactiveScaleFrequencyMax);
            }
            catch (Exception)
            {
                shakeFreqMin = 5; shakeFreqMax = 25;
                scaleFreqMin = 5; scaleFreqMax = 25;
            }

            // Calculate bass amplitude for shake effect (using frequency range)
            float shakeBassAmplitude = 0f;
            int shakeStartIdx = Math.Max(0, Math.Min(shakeFreqMin, amplitudes.Length));
            int shakeEndIdx = Math.Max(shakeStartIdx, Math.Min(shakeFreqMax, amplitudes.Length));
            int shakeBassCount = shakeEndIdx - shakeStartIdx;

            // Calculate bass amplitude for scale effect (using frequency range)
            float scaleBassAmplitude = 0f;
            int scaleStartIdx = Math.Max(0, Math.Min(scaleFreqMin, amplitudes.Length));
            int scaleEndIdx = Math.Max(scaleStartIdx, Math.Min(scaleFreqMax, amplitudes.Length));
            int scaleBassCount = scaleEndIdx - scaleStartIdx;

            // Calculate shake bass amplitude within frequency range
            if (shakeBassCount > 0)
            {
                for (int i = shakeStartIdx; i < shakeEndIdx; i++)
                {
                    shakeBassAmplitude += amplitudes[i];
                }
                shakeBassAmplitude /= shakeBassCount;
            }

            // Calculate scale bass amplitude within frequency range
            if (scaleBassCount > 0)
            {
                for (int i = scaleStartIdx; i < scaleEndIdx; i++)
                {
                    scaleBassAmplitude += amplitudes[i];
                }
                scaleBassAmplitude /= scaleBassCount;
            }

            // Get configurable parameters (with fallback to avoid crash)
            float shakeMultiplier, scaleMultiplier, shakeThreshold, scaleThreshold;
            try
            {
                shakeMultiplier = config.Get<float>(OsuSetting.AudioReactiveShakeIntensity);
                scaleMultiplier = config.Get<float>(OsuSetting.AudioReactiveScaleMultiplier);
                shakeThreshold = config.Get<float>(OsuSetting.AudioReactiveShakeThreshold);
                scaleThreshold = config.Get<float>(OsuSetting.AudioReactiveScaleThreshold);
            }
            catch (Exception)
            {
                shakeMultiplier = 1.0f;
                scaleMultiplier = 1.0f;
                shakeThreshold = 0.05f;
                scaleThreshold = 0.05f;
            }

            // Apply scale effect
            if (scaleBassAmplitude > scaleThreshold)
            {
                float targetScale = 1.0f + (scaleBassAmplitude * BASS_SCALE_MULTIPLIER * scaleMultiplier);
                if (targetScale > currentBassScale)
                {
                    currentBassScale = targetScale;
                }
            }

            // Apply shake effect
            if (shakeBassAmplitude > shakeThreshold)
            {
                float shakeIntensity = Math.Min(shakeBassAmplitude * MAX_SHAKE_OFFSET * shakeMultiplier, MAX_SHAKE_OFFSET * shakeMultiplier);
                currentShakeOffset = new Vector2(
                    (float)(random.NextDouble() - 0.5) * shakeIntensity * 2,
                    (float)(random.NextDouble() - 0.5) * shakeIntensity * 2
                );
            }

            // Apply the current effects to the entire background (not just the sprite)
            // This ensures effects are visible even with background blur/dim overlays
            this.Scale = new Vector2(currentBassScale);
            this.Position = currentShakeOffset;
        }

        private void applyDecay()
        {
            double elapsed = Time.Elapsed;

            // Get configurable decay rates
            float scaleDecayRate, shakeDecayRate;
            try
            {
                scaleDecayRate = config.Get<float>(OsuSetting.AudioReactiveScaleDecayRate);
                shakeDecayRate = config.Get<float>(OsuSetting.AudioReactiveShakeDecayRate);
            }
            catch (Exception)
            {
                scaleDecayRate = 0.90f;
                shakeDecayRate = 0.75f;
            }

            // Decay scale back to 1.0
            currentBassScale = (float)Interpolation.Damp(currentBassScale, 1.0f, scaleDecayRate, elapsed);

            // Decay shake back to zero
            currentShakeOffset = Vector2.Multiply(currentShakeOffset, (float)Math.Pow(shakeDecayRate, elapsed / 16.0));
        }

        // IBeatSyncProvider implementation (delegates to injected provider)
        public ChannelAmplitudes CurrentAmplitudes => beatSyncProvider?.CurrentAmplitudes ?? new ChannelAmplitudes();
        public ControlPointInfo? ControlPoints => beatSyncProvider?.ControlPoints;
        public new IClock Clock => beatSyncProvider?.Clock ?? base.Clock;
    }
}
