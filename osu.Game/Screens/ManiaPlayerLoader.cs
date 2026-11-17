// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Logging;
using osu.Framework.Screens;
using osu.Game.Screens.Play;

namespace osu.Game.Screens
{
    /// <summary>
    /// 简化的加载器，直接加载 ManiaPlayerScreen
    /// </summary>
    public partial class ManiaPlayerLoader : Loader
    {
        protected override OsuScreen CreateLoadableScreen()
        {
            Logger.Log("[ManiaMode] ManiaPlayerLoader: Creating ManiaPlayerScreen (skipping intro and main menu)", LoggingTarget.Runtime, LogLevel.Important);
            // 直接返回 mania 播放器等待屏幕，跳过介绍和主菜单
            return new ManiaPlayerScreen();
        }
    }
}

