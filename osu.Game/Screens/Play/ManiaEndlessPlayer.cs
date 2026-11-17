// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Logging;
using osu.Framework.Screens;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking;

namespace osu.Game.Screens.Play
{
    /// <summary>
    /// 无尽模式的 Mania 播放器，不会死亡，播放完毕后不显示结算界面
    /// </summary>
    public partial class ManiaEndlessPlayer : SoloPlayer
    {
        public Action? OnBeatmapCompleted { get; set; }

        public ManiaEndlessPlayer()
            : base(new PlayerConfiguration
            {
                ShowResults = false,  // 不显示结算界面
                AllowPause = false,   // 不允许暂停
                AllowUserInteraction = false,  // 减少用户交互
            })
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Logger.Log("[ManiaMode] ManiaEndlessPlayer: Loading player", LoggingTarget.Runtime, LogLevel.Important);

            // 添加 NoFail mod 以防止死亡
            if (!Mods.Value.Any(m => m is ModNoFail))
            {
                Logger.Log("[ManiaMode] ManiaEndlessPlayer: Adding NoFail mod", LoggingTarget.Runtime, LogLevel.Important);
                var mods = Mods.Value.ToList();
                var noFail = Ruleset.Value.CreateInstance().CreateMod<ModNoFail>();
                if (noFail != null)
                {
                    mods.Add(noFail);
                    Mods.Value = mods.ToArray();
                    Logger.Log("[ManiaMode] ManiaEndlessPlayer: NoFail mod added successfully", LoggingTarget.Runtime, LogLevel.Important);
                }
                else
                {
                    Logger.Log("[ManiaMode] ManiaEndlessPlayer: WARNING - Could not create NoFail mod", LoggingTarget.Runtime, LogLevel.Important);
                }
            }
            else
            {
                Logger.Log("[ManiaMode] ManiaEndlessPlayer: NoFail mod already present", LoggingTarget.Runtime, LogLevel.Important);
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Logger.Log("[ManiaMode] ManiaEndlessPlayer: LoadComplete called", LoggingTarget.Runtime, LogLevel.Important);

            // 监听分数处理器的完成事件
            if (ScoreProcessor != null)
            {
                Logger.Log("[ManiaMode] ManiaEndlessPlayer: Setting up completion callback", LoggingTarget.Runtime, LogLevel.Important);
                // 监听完成事件
                ScoreProcessor.HasCompleted.ValueChanged += e =>
                {
                    if (e.NewValue)
                    {
                        Logger.Log("[ManiaMode] ManiaEndlessPlayer: Beatmap playback completed!", LoggingTarget.Runtime, LogLevel.Important);
                        // 歌曲播放完成后，直接退出
                        OnBeatmapCompleted?.Invoke();
                        Schedule(() =>
                        {
                            if (this.IsCurrentScreen())
                            {
                                Logger.Log("[ManiaMode] ManiaEndlessPlayer: Exiting player screen", LoggingTarget.Runtime, LogLevel.Important);
                                this.Exit();
                            }
                        });
                    }
                };
            }
            else
            {
                Logger.Log("[ManiaMode] ManiaEndlessPlayer: WARNING - ScoreProcessor is null", LoggingTarget.Runtime, LogLevel.Important);
            }
        }

        protected override void PerformFail()
        {
            Logger.Log("[ManiaMode] ManiaEndlessPlayer: PerformFail called - ignoring (no-death mode)", LoggingTarget.Runtime, LogLevel.Important);
            // 完全禁用失败逻辑
            // 不调用 base.PerformFail()
        }

        protected override ResultsScreen CreateResults(ScoreInfo score)
        {
            Logger.Log("[ManiaMode] ManiaEndlessPlayer: CreateResults called - returning null (no results screen)", LoggingTarget.Runtime, LogLevel.Important);
            // 不创建结算界面
            return null!;
        }

        protected override Task PrepareScoreForResultsAsync(Score score)
        {
            // 跳过分数准备
            return Task.CompletedTask;
        }

        public override bool OnBackButton()
        {
            Logger.Log("[ManiaMode] ManiaEndlessPlayer: Back button pressed", LoggingTarget.Runtime, LogLevel.Important);
            // 允许返回按钮退出
            OnBeatmapCompleted?.Invoke();
            return base.OnBackButton();
        }
    }
}

