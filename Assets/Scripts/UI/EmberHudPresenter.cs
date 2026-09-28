namespace Emberlight
{
    /// <summary>
    /// 战斗 HUD 的唯一推送点：每帧把只读数值喂给 <see cref="EmberHud"/>。
    ///
    /// UI 不主动拉取。原先这段拼装逻辑写在 `EmberGame.UpdateHud()` 里，把战斗读数、关卡文案
    /// 与呈现细节混在入口脚本中；搬出来之后 `EmberGame` 不必再知道 HUD 的字段构成，
    /// 表现层也可以整体替换而不动玩法代码。
    ///
    /// 本类只读，不写任何游戏状态，也不持有 <see cref="EmberGame"/>。
    /// </summary>
    public sealed class EmberHudPresenter
    {
        readonly EmberHud hud;
        readonly EmberRunState run;
        readonly EmberCombat combat;
        readonly System.Func<EmberRunPhase> phase;

        public EmberHudPresenter(EmberHud battleHud, EmberRunState runState, EmberCombat battle, System.Func<EmberRunPhase> phaseProvider)
        {
            hud = battleHud;
            run = runState;
            combat = battle;
            phase = phaseProvider;
        }

        /// <summary>把当前只读状态推到 HUD；缺任一前置条件时静默跳过。</summary>
        public void Push()
        {
            if (hud == null || run == null || run.Progress == null) return;

            var level = run.Level;
            int wave = combat != null ? combat.Wave : 1;
            int remaining = combat != null ? combat.WaveRemaining : 0;
            int quota = combat != null ? combat.WaveQuota : 0;

            hud.Set(run.Health, run.MaxHealth, run.Progress, run.Elapsed, run.Kills, wave, remaining, quota);
            hud.Stage(wave, level != null ? level.BossWave : 0,
                LevelConfig.DifficultyName(level != null ? level.Difficulty : EmberDifficulty.Standard),
                level != null ? level.WaveName(wave) : "");

            if (combat == null) return;
            hud.Encounter(combat.EncounterText);
            var boss = combat.Boss;
            hud.Boss(
                boss == null ? 0f : boss.hp / boss.maxHp,
                boss == null ? "" : boss.ai.Phase,
                boss != null && phase != null && phase() == EmberRunPhase.Playing);
        }
    }
}
