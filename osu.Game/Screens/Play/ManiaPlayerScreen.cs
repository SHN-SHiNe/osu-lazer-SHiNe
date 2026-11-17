// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Framework.Screens;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Play
{
    /// <summary>
    /// 简化的 Mania 播放器屏幕，用于等待拖拽 MP3 文件
    /// </summary>
    public partial class ManiaPlayerScreen : OsuScreen
    {
        public override bool HideOverlaysOnEnter => true;
        public override bool CursorVisible => false;

        [BackgroundDependencyLoader]
        private void load()
        {
            Logger.Log("[ManiaMode] ManiaPlayerScreen: Loading waiting screen UI", LoggingTarget.Runtime, LogLevel.Important);

            InternalChildren = new Drawable[]
            {
                new FillFlowContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 20),
                    Children = new Drawable[]
                    {
                        new SpriteIcon
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Icon = FontAwesome.Solid.Music,
                            Size = new Vector2(80),
                            Colour = Color4.White,
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Text = "拖拽 MP3 文件到此窗口开始播放",
                            Font = OsuFont.Default.With(size: 32, weight: FontWeight.Bold),
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Text = "游戏会自动搜索 MP3 所在文件夹的 .osu 谱面文件",
                            Font = OsuFont.Default.With(size: 20),
                            Colour = Color4.Gray,
                        },
                    }
                }
            };
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            Logger.Log("[ManiaMode] ManiaPlayerScreen: Screen entered, waiting for MP3 drag & drop", LoggingTarget.Runtime, LogLevel.Important);
        }
    }
}

