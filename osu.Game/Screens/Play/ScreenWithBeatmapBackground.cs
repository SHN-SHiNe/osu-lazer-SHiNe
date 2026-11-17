// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Screens;
using osu.Game.Screens.Backgrounds;

namespace osu.Game.Screens.Play
{
    public abstract partial class ScreenWithBeatmapBackground : OsuScreen
    {
        protected override BackgroundScreen CreateBackground()
        {
            // For Mania mode, a DualBackgroundManager will be created and replaced in Player.setupManiaDualBackground()
            return new BackgroundScreenBeatmap(Beatmap.Value);
        }

        public void ApplyToBackground(Action<BackgroundScreenBeatmap> action)
        {
            Debug.Assert(this.IsCurrentScreen());

            base.ApplyToBackground(b =>
            {
                if (b is BackgroundScreenBeatmap beatmapBackground)
                {
                    action.Invoke(beatmapBackground);
                }
                // If it's not a BackgroundScreenBeatmap (e.g., DualBackgroundManager), ignore the action
            });
        }
    }
}
