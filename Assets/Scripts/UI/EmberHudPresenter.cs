namespace Emberlight
{
    /// <summary>战斗 HUD 的唯一推送点：每帧把只读数值喂给 <see cref="EmberHud"/>，UI 不主动拉取；只读，不写游戏状态，也不持有 <see cref="EmberGame"/>。</summary>
    public sealed class EmberHudPresenter
    {
        readonly EmberHud hud;
        readonly EmberRunState run;
        readonly EmberCombat combat;
        readonly System.Func<EmberRunPhase> phase;
        LevelConfig shownLevel;
        int shownWave = -1, shownTotal = -1;

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
            int total = level != null ? level.BossWave : 0;
            if (wave != shownWave || total != shownTotal || level != shownLevel)
            {
                hud.Stage(wave, total,
                    LevelConfig.DifficultyName(level != null ? level.Difficulty : EmberDifficulty.Standard),
                    level != null ? level.WaveName(wave) : "");
                shownWave = wave;
                shownTotal = total;
                shownLevel = level;
            }

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
