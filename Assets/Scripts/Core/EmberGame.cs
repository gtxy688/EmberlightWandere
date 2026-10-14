using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 组合根（M0）：本作唯一的入口脚本，挂在主场景的常驻对象上。
    ///
    /// 它只做四件事——持有阶段与本局状态、装配各单元、驱动每帧顺序、把各单元的回调接起来。
    /// 业务规则分别属于：生命结算 <see cref="EmberKeeperVitals"/>、选卡推进 <see cref="RunFlowController"/>、
    /// 开局界面流程 <see cref="EmberMenuFlow"/>、战斗与波次 <see cref="EmberCombat"/>、构筑与卡池
    /// <see cref="RunProgress"/>。**不要在这里重新堆业务规则。**
    /// </summary>
    public sealed class EmberGame : MonoBehaviour, IEmberMenuHost, RunFlowController.Host, IEmberCombatHost, EmberInput.Presenter
    {
        [SerializeField] EmberUiRoot uiRoot;
        [SerializeField] EmberIntroView introRoot;

        /// <summary>当前局长阶段；只有 Playing 阶段推进战斗与 HUD。</summary>
        public EmberRunPhase Phase { get; private set; }

        /// <summary>本局数据；不在局内时为 null。生命结算经 <see cref="EmberRunState.Vitals"/> 访问。</summary>
        public EmberRunState Run { get; private set; }

        /// <summary>本局已进行的游戏内秒数。</summary>
        public float Elapsed { get { return Run.Elapsed; } }

        /// <summary>本局构筑进度；未开局时为 null。</summary>
        public RunProgress Progress { get { return Run.Progress; } }

        /// <summary>UI API：本局剩余刷新次数。</summary>
        public int RefreshRemaining { get { return Run.Progress != null ? Run.Progress.RefreshesRemaining : 0; } }

        /// <summary>UI API：当前幸运值。</summary>
        public float LuckValue { get { return Run.Progress != null ? Run.Progress.Luck : 0f; } }

        /// <summary>UI API：最近一次发到桌面上的候选。</summary>
        public EmberOffer[] CurrentOffers { get { return offered; } }

        EmberCombat combat;
        EmberMenuUi ui;
        EmberMenuFlow menuFlow;
        RunFlowController flow;
        /// <summary>战斗 HUD 的唯一推送点；在 `EmberMenuUi.Bind` 之后才有目标可推。</summary>
        EmberHudPresenter hud;
        EmberInput input;
        readonly System.Random random = new System.Random();
        EmberOffer[] offered = new EmberOffer[0];

        RunProgress RunFlowController.Host.Progress { get { return Run.Progress; } }

        int RunFlowController.Host.FinalWave { get { return Run.Level != null ? Run.Level.BossWave : 0; } }

        int RunFlowController.Host.PicksAfterWave(int clearedWave)
        {
            return Run.Level != null ? Run.Level.PicksAfterWave(clearedWave) : 0;
        }

        EmberRunPhase RunFlowController.Host.Phase { get { return Phase; } }

        EmberOffer[] RunFlowController.Host.Offers { get { return offered; } }

        void RunFlowController.Host.RequestChoices()
        {
            offered = Run.Progress.Choices(random, Run.Level != null ? Run.Level.BossWave : 0);
        }

        void RunFlowController.Host.OnUpgradeOffered(EmberOffer[] offers)
        {
            ShowUpgradeNotice(offers);
        }

        void RunFlowController.Host.OnUpgradeSequenceFinished()
        {
            ReturnToPlay();
        }

        void Awake()
        {
            Run = new EmberRunState(() => Phase);
            flow = new RunFlowController(this, random);
            input = new EmberInput(this);
        }

        void Start()
        {
            // 移动端默认封顶 30 fps；这是 2D 精灵游戏，显式要 60 并关掉 VSync。
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            EmberAudio.Ensure();
            Phase = EmberRunPhase.Loading;
            combat = new EmberCombat(this);
            ui = new EmberMenuUi();
            menuFlow = new EmberMenuFlow(ui, this);
            var cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = 8;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.055f, .085f, .12f);
            Run.Cam = cam;
            // 加载页期间就开始放营地环境音；ShowMainMenu 会再调一次，音频模块会保持当前曲目。
            EmberAudio.Ensure().PlayMenuMusic();
            if (uiRoot == null) uiRoot = Object.FindObjectOfType<EmberUiRoot>(true);
            if (uiRoot == null) throw new System.InvalidOperationException("The scene requires a configured UIRoot prefab.");
            uiRoot.gameObject.SetActive(false);
            if (introRoot == null) introRoot = Object.FindObjectOfType<EmberIntroView>(true);
            if (introRoot == null) throw new System.InvalidOperationException("The scene requires a configured IntroRoot prefab.");
            gameObject.AddComponent<EmberIntro>().Begin(introRoot, loadedFont =>
            {
                ui.Bind(uiRoot, loadedFont, Pause);
                ui.WatchSafeArea();
                hud = new EmberHudPresenter(ui.BattleHud, Run, combat, () => Phase);
                Phase = EmberRunPhase.Menu;
                ui.Overlay.SetActive(false);
                ui.BattleHud.Show(false);
            }, () => menuFlow.ShowMainMenu());
        }

        void OnDestroy()
        {
            if (ui != null)
                ui.UnwatchSafeArea();
            Time.timeScale = 1;
        }

        void Update()
        {
            if (Phase != EmberRunPhase.Playing)
                return;
            float dt = Time.deltaTime;
            Run.Elapsed += dt;
            Run.Vitals.Tick(dt);
            input.Tick();
            // 输入与生命结算都可能改阶段（暂停 / 收局），每步之后重新确认还在局内。
            if (Phase != EmberRunPhase.Playing)
                return;
            EmberKeeperMotion.Step(Run, input.Stick, dt);
            combat.Tick(dt);
            if (Phase != EmberRunPhase.Playing)
                return;
            hud.Push();
        }

        /// <summary>装配一局：建关卡配置与世界节点、重置构筑与生命、启动波次。</summary>
        void StartRunWith(int[] weapons, int slots)
        {
            if (ui == null || ui.BattleHud == null)
                return;
            Run.Level = LevelConfig.Create(menuFlow.SelectedDifficulty, menuFlow.SelectedWaves);
            flow.Reset();
            if (ui.GodsSelect != null)
                ui.GodsSelect.Hide();
            if (Run.World != null)
            {
                Run.World.gameObject.SetActive(false);
                Destroy(Run.World.gameObject);
            }

            combat.TearDown();
            Run.World = new GameObject("Run world").transform;
            Run.Effects = Run.World.gameObject.AddComponent<EmberEffects>();
            Run.Effects.Initialize(() => Phase);
            Run.Progress = new RunProgress();
            Run.Progress.ConfigureRun(weapons, slots);
            Run.Kills = 0;
            Run.Vitals.Reset();
            // 灯芯的外形由 EmberKeeperAnimation 负责（兜帽轮廓、细眼、提灯摆动）。
            Run.Keeper = new GameObject("Keeper").transform;
            Run.Keeper.SetParent(Run.World, false);
            Run.Keeper.gameObject.AddComponent<EmberKeeperAnimation>().Initialize(() => Phase);
            var deco = new System.Random(73);
            for (int i = 0; i < 160; i++)
            {
                Vector2 p = new Vector2((float)deco.NextDouble() * 46 - 23, (float)deco.NextDouble() * 46 - 23);
                EmberVisuals.Shape("Ruin stone", Run.World, p, new Vector2(.3f + (float)deco.NextDouble(), .18f), new Color(.10f, .15f, .19f), 0);
            }

            var level = Run.Level;
            level.ApplyWorld();
            EmberWorld.BuildBoundary(Run.World);
            // Begin 只激活槽位内的武器（由 OwnsWeapon 门控）。
            combat.Begin(Run.World, Run.Keeper, Run.Cam, Run.Effects, level);
            EmberKeeperMotion.Follow(Run);
            SetPlaying();
            hud.Push();
        }

        /// <summary>收起所有面板、恢复玩家选择的倍速并回到 Playing 阶段。</summary>
        void SetPlaying()
        {
            if (ui.UpgradePanel != null)
                ui.UpgradePanel.Hide();
            if (ui.GodsSelect != null)
                ui.GodsSelect.Hide();
            if (ui.SettingsPanel != null)
                ui.SettingsPanel.Hide();
            Phase = EmberRunPhase.Playing;
            GameSpeed.Apply();
            EmberAudio.Ensure().PlayCombatMusic();
            ui.Overlay.SetActive(false);
            ui.BattleHud.Show(true);
            input.Reset();
            ui.StickBase.gameObject.SetActive(false);
        }

        /// <summary>进入 Upgrade 阶段并画出候选；选卡规则本身在 <see cref="RunFlowController"/>。</summary>
        void ShowUpgradeNotice(EmberOffer[] offers)
        {
            offered = offers;
            Phase = EmberRunPhase.Upgrade;
            EmberAudio.Ensure().PlayCardOpen();
            ui.BattleHud.Show(false);
            input.Reset();
            ui.Overlay.SetActive(false);
            ui.StickBase.gameObject.SetActive(false);
            ui.UpgradePanel.Show(offered, Run.Progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails, SelectUpgrade, TryRefreshOffer);
        }

        /// <summary>选卡序列走完：开始下一波并回到战斗。</summary>
        void ReturnToPlay()
        {
            combat.AdvanceAfterUpgrade();
            SetPlaying();
            hud.Push();
        }

        /// <summary>界面回流：清波后请求发卡。</summary>
        public void OnWaveCleared(int clearedWave)
        {
            flow.OnWaveCleared(clearedWave);
        }

        /// <summary>UI API：扣一次免费刷新并重绘候选面板。</summary>
        public bool TryRefreshOffer()
        {
            if (!flow.TryRefresh())
                return false;
            ui.UpgradePanel.Show(offered, Run.Progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails, SelectUpgrade, TryRefreshOffer);
            return true;
        }

        /// <summary>UI 回调：玩家点了一张卡。</summary>
        public void SelectUpgrade(EmberOffer offer)
        {
            if (!flow.Choose(offer))
                return;
            EmberAudio.Ensure().PlayCardPick();
        }

        public void Pause()
        {
            if (Phase != EmberRunPhase.Playing)
                return;
            Phase = EmberRunPhase.Paused;
            input.Reset();
            ui.BattleHud.Show(false);
            ui.StickBase.gameObject.SetActive(false);
            if (ui.UpgradePanel != null)
                ui.UpgradePanel.Hide();
            if (ui.GodsSelect != null)
                ui.GodsSelect.Hide();
            if (ui.SettingsPanel != null)
                ui.SettingsPanel.Show(SetPlaying, true, ReturnToCamp);
            else
                ui.Show(EmberCopy.PauseTitle, EmberCopy.PauseBody, new[] { EmberCopy.PauseChoice }, new UnityEngine.Events.UnityAction[] { SetPlaying });
        }

        /// <summary>收局回营地：拆掉世界、战斗与本局状态，回到主菜单。</summary>
        void ReturnToCamp()
        {
            Time.timeScale = 1f;
            input.Reset();
            flow.Reset();
            if (ui != null)
            {
                if (ui.UpgradePanel != null)
                    ui.UpgradePanel.Hide();
                if (ui.GodsSelect != null)
                    ui.GodsSelect.Hide();
                if (ui.BattleHud != null)
                    ui.BattleHud.Show(false);
                if (ui.StickBase != null)
                    ui.StickBase.gameObject.SetActive(false);
            }

            if (combat != null)
                combat.TearDown();
            if (Run.World != null)
            {
                Run.World.gameObject.SetActive(false);
                Destroy(Run.World.gameObject);
            }

            Run.Clear();
            menuFlow.ShowMainMenu();
        }

        /// <summary>收局：胜利由最终 Boss 死亡触发，失败由生命归零触发。</summary>
        void End(bool won)
        {
            if (Phase == EmberRunPhase.Won || Phase == EmberRunPhase.Lost || Run.Progress == null)
                return;
            Phase = won ? EmberRunPhase.Won : EmberRunPhase.Lost;
            EmberAudio.Ensure().PlayMenuMusic();
            var level = Run.Level;
            string result = LevelConfig.DifficultyName(level.Difficulty) + " · " + level.BossWave + " 波征程"
                + "\n到达第 " + combat.Wave + " 波 · 生存 " + Run.Elapsed.ToString("0") + " 秒"
                + "\n击杀 " + Run.Kills + " · 余烬 " + Run.Progress.FinalScore();
            ui.Show(won ? EmberCopy.WinTitle : EmberCopy.LoseTitle, result,
                new[] { EmberCopy.RestartChoice, EmberCopy.ReturnChoice },
                new UnityEngine.Events.UnityAction[] { menuFlow.BeginRun, ReturnToCamp });
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                Pause();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
                Pause();
        }

        /// <summary>摇杆状态交给界面摆放底盘；输入解析本身在 <see cref="EmberInput"/>。</summary>
        void EmberInput.Presenter.OnStick(bool active, Vector2 screenOrigin, Vector2 stick)
        {
            if (ui != null)
                ui.SetStick(active, screenOrigin, stick);
        }

        void EmberInput.Presenter.OnPauseRequested()
        {
            Pause();
        }

        /// <summary>UI 与战斗共用：无敌帧是否已结束。</summary>
        public bool HurtReady { get { return Run.Vitals.HurtReady; } }

        /// <summary>UI 与掉落共用：是否处在"残血且战斗中"。</summary>
        public bool NeedsHealing { get { return Run.Vitals.NeedsHealing; } }

        /// <summary>受伤入口：结算护盾与生命，生命归零则收局。</summary>
        public void HurtPlayer(float damage)
        {
            if (Run.Vitals.Hurt(damage) == EmberVitalOutcome.Rejected)
                return;
            EmberAudio.Ensure().PlayHurt();
            hud.Push();
            if (Run.Vitals.CommitHealthChange())
                End(false);
        }

        /// <summary>治疗入口（护盾词条之外的唯一回血来源）。</summary>
        public bool HealPlayer(float amount)
        {
            var outcome = Run.Vitals.Heal(amount);
            if (outcome == EmberVitalOutcome.Rejected)
                return false;
            hud.Push();
            return true;
        }

        /// <summary>怪物治疗掉落：任意血量可拾取，溢出丢弃。</summary>
        public bool PickHealingDrop(float amount)
        {
            if (Run.Vitals.PickHealingDrop(amount) == EmberVitalOutcome.Rejected)
                return false;
            hud.Push();
            EmberAudio.Ensure().PlayPickupHeal();
            return true;
        }

        /// <summary>Boss 招式落点的爆发特效转发。</summary>
        public void PlayBossBurst(Vector2 point)
        {
            combat.PlayBossBurst(point);
        }

        public void RegisterKill()
        {
            Run.Kills++;
        }

        public void EndRun(bool won)
        {
            End(won);
        }

        /// <summary>IEmberMenuHost：界面流程请求切换局长阶段。</summary>
        public void EnterMode(EmberRunPhase mode)
        {
            Phase = mode;
        }

        /// <summary>IEmberMenuHost：界面流程回主菜单时收起上一局的世界。</summary>
        public void HideRunWorld()
        {
            if (Run.World != null)
                Run.World.gameObject.SetActive(false);
        }

        /// <summary>IEmberMenuHost：起始武器选完，正式开一局。</summary>
        public void StartRun(int[] weapons, int slots)
        {
            StartRunWith(weapons, slots);
        }
    }
}
