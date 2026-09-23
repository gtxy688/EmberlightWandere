using UnityEngine;
using TMPro;

namespace Emberlight
{
    /// <summary>Run director: Gods-Select-v3 pre-run (N + roster), wave offers, compensation, refreshes.</summary>
    public sealed class EmberGame : MonoBehaviour, IEmberMenuHost
    {
        public enum Mode { Loading, Menu, SelectLength, SelectDifficulty, SelectSlots, SelectRoster, Playing, Upgrade, Paused, Lost, Won }


        public Mode State { get; private set; }
        public RunProgress Progress { get; private set; }
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        public float Health { get; private set; }
        public bool HurtReady { get { return hurtTimer <= 0; } }
        public int EnemyCount { get { return combat != null ? combat.EnemyCount : 0; } }

        /// <summary>UI API: refreshes left this run.</summary>
        public int RefreshRemaining { get { return Progress != null ? Progress.RefreshesRemaining : 0; } }
        /// <summary>UI API: current luck.</summary>
        public float LuckValue { get { return Progress != null ? Progress.Luck : 0f; } }
        /// <summary>UI API: last offered cards.</summary>
        public EmberOffer[] CurrentOffers { get { return offered; } }

        EmberCombat combat;
        EmberMenuUi ui;
        EmberMenuFlow menuFlow;
        int pendingWavePicks;
        LevelConfig runLevel = LevelConfig.Default;
        Transform world, player;
        Camera cam;
        float hurtTimer;
        int pointer = -1;
        Vector2 origin, stick;
        EmberOffer[] offered = new EmberOffer[0];
        readonly System.Random random = new System.Random();
        int lastClearedWave;
        bool compensationPick;
        int lastScreenWidth, lastScreenHeight;
        Rect lastSafeArea;

        float MaxHealth { get { return 100 + (Progress != null ? Progress.BonusMaxHealth : 0); } }

        void Start()
        {
            // Mobile defaults to a 30 fps cap; this is a 2D sprite game with a light
            // per-frame budget, so ask for 60 and keep VSync out of the way.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            EmberAudio.Ensure();
            State = Mode.Loading;
            combat = new EmberCombat(this);
            ui = new EmberMenuUi();
            menuFlow = new EmberMenuFlow(ui, this);
            cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.orthographicSize = 8;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.055f, .085f, .12f);
            // Start the camp ambience while the loading page is visible. ShowMainMenu
            // calls this again, but EmberAudio keeps the current track playing.
            EmberAudio.Ensure().PlayMenuMusic();
            gameObject.AddComponent<EmberIntro>().Begin(loadedFont =>
            {
                ui.Build(transform, loadedFont, Pause);
                State = Mode.Menu;
                ui.Overlay.SetActive(false);
                ui.BattleHud.Show(false);
            }, () => menuFlow.ShowMainMenu());
        }

        void OnDestroy() { Time.timeScale = 1; }

        void StartRunWith(int[] weapons, int slots)
        {
            if (ui == null || ui.BattleHud == null) return;
            runLevel = LevelConfig.Create(menuFlow.SelectedDifficulty, menuFlow.SelectedWaves);
            pendingWavePicks = 0;
            if (ui.GodsSelect != null) ui.GodsSelect.Hide();
            if (world != null)
            {
                world.gameObject.SetActive(false);
                Destroy(world.gameObject);
            }
            combat.TearDown();
            world = new GameObject("Run world").transform;
            var effects = world.gameObject.AddComponent<EmberEffects>();
            effects.Initialize(this);
            Progress = new RunProgress();
            Progress.ConfigureRun(weapons, slots);
            Health = MaxHealth;
            Elapsed = 0;
            Kills = 0;
            hurtTimer = 0;
            compensationPick = false;
            // The keeper rig belongs to EmberKeeperAnimation (hood rim, narrow eyes, swinging
            // lantern); EmberVisuals.Character only builds the plain shade silhouette.
            player = new GameObject("Keeper").transform;
            player.SetParent(world, false);
            player.gameObject.AddComponent<EmberKeeperAnimation>().Initialize();
            var deco = new System.Random(73);
            for (int i = 0; i < 160; i++)
            {
                Vector2 p = new Vector2((float)deco.NextDouble() * 46 - 23, (float)deco.NextDouble() * 46 - 23);
                EmberVisuals.Shape("Ruin stone", world, p, new Vector2(.3f + (float)deco.NextDouble(), .18f), new Color(.10f, .15f, .19f), 0);
            }
            var level = runLevel;
            level.ApplyWorld();
            EmberWorld.BuildBoundary(world);
            // Begin activates only weapons in slots (OwnsWeapon gate).
            combat.Begin(world, player, cam, effects, level);
            UpdateCamera();
            SetPlaying();
        }

        void SetPlaying()
        {
            if (ui.UpgradePanel != null) ui.UpgradePanel.Hide();
            if (ui.GodsSelect != null) ui.GodsSelect.Hide();
            if (ui.SettingsPanel != null) ui.SettingsPanel.Hide();
            State = Mode.Playing;
            // Restore the player's chosen speed; teardown and the editor reset timeScale to 1.
            GameSpeed.Apply();
            EmberAudio.Ensure().PlayCombatMusic();
            ui.Overlay.SetActive(false);
            ui.BattleHud.Show(true);
            UpdateHud();
            pointer = -1;
            stick = Vector2.zero;
            ui.StickBase.gameObject.SetActive(false);
        }

        void Update()
        {
            // Safe area only changes on a resolution / orientation change, so this is driven by
            // the event instead of being polled every frame by every state, including the ones
            // that return immediately below.
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight ||
                Screen.safeArea != lastSafeArea)
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                lastSafeArea = Screen.safeArea;
                ui.UpdateSafeArea();
            }
            if (State != Mode.Playing) return;
            float dt = Time.deltaTime;
            Elapsed += dt;
            hurtTimer -= dt;
            ReadInput();
            if (State != Mode.Playing) return;

            Vector2 move = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")) + stick, 1);
            player.position += (Vector3)(move * (3.3f * (1 + Progress.MoveSpeedBonus) * dt));
            player.position = EmberWorld.Clamp(player.position);
            UpdateCamera();

            combat.Tick(dt);
            if (State != Mode.Playing) return;
            UpdateHud();
        }


        void UpdateHud()
        {
            if (ui == null || ui.BattleHud == null || Progress == null) return;
            int wave = combat != null ? combat.Wave : 1;
            int remaining = combat != null ? combat.WaveRemaining : 0;
            int quota = combat != null ? combat.WaveQuota : 0;
            ui.BattleHud.Set(Health, MaxHealth, Progress, Elapsed, Kills, wave, remaining, quota);
            ui.BattleHud.Stage(wave, runLevel.BossWave, LevelConfig.DifficultyName(runLevel.Difficulty), runLevel.WaveName(wave));
            ui.BattleHud.Encounter(combat.EncounterText);
            var boss = combat.Boss;
            ui.BattleHud.Boss(
                boss == null ? 0 : boss.hp / boss.maxHp,
                boss == null ? "" : boss.ai.Phase,
                boss != null && State == Mode.Playing);
        }

        public bool NeedsHealing { get { return State == Mode.Playing && Health < MaxHealth; } }
        public static float HealedHealth(float current, float maximum, float amount)
        { return Mathf.Min(maximum, current + Mathf.Max(0, amount)); }
        public bool HealPlayer(float amount)
        {
            if (!NeedsHealing || amount <= 0) return false;
            Health = HealedHealth(Health, MaxHealth, amount);
            UpdateHud();
            return true;
        }
        /// <summary>Monster heal drop: pickable at any HP. Heal only; overflow discarded (shield is trait-only).</summary>
        public bool PickHealingDrop(float amount)
        {
            if (State != Mode.Playing || amount <= 0) return false;
            if (Health < MaxHealth)
                Health = HealedHealth(Health, MaxHealth, amount);
            UpdateHud();
            EmberAudio.Ensure().PlayPickupHeal();
            return true;
        }

        public void HurtPlayer(float damage)
        {
            if (State != Mode.Playing || hurtTimer > 0 || damage <= 0) return;
            Health = Mathf.Max(0, Health - Progress.AbsorbDamage(damage * runLevel.DamageMultiplier));
            EmberAudio.Ensure().PlayHurt();
            hurtTimer = .65f;
            UpdateHud();
            if (Health <= 0) EndRun(false);
        }

        public void PlayBossBurst(Vector2 point) { combat.PlayBossBurst(point); }
        public void RegisterKill() { Kills++; }
        public void EndRun(bool won) { End(won); }

        void UpdateCamera() { cam.transform.position = new Vector3(player.position.x, player.position.y, -10); }

        public void OnWaveCleared(int clearedWave)
        {
            if (State != Mode.Playing || Progress == null) return;
            lastClearedWave = clearedWave;
            compensationPick = false;
            pendingWavePicks = runLevel.PicksAfterWave(clearedWave);
            if (pendingWavePicks == 0) { combat.AdvanceAfterUpgrade(); SetPlaying(); return; }
            Progress.OfferChoices();
            OfferUpgrade(clearedWave);
        }

        public void OfferUpgrade(int clearedWave)
        {
            State = Mode.Upgrade;
            EmberAudio.Ensure().PlayCardOpen();
            ui.BattleHud.Show(false);
            stick = Vector2.zero;
            lastClearedWave = clearedWave;
            offered = Progress.Choices(random, clearedWave);
            ui.Overlay.SetActive(false);
            ui.StickBase.gameObject.SetActive(false);
            ui.UpgradePanel.Show(offered, Progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails, SelectUpgrade, TryRefreshOffer);
        }

        /// <summary>UI API: spend a free refresh and rebuild the offer panel.</summary>
        public bool TryRefreshOffer()
        {
            if (State != Mode.Upgrade || Progress == null) return false;
            EmberOffer[] next;
            if (!Progress.TryRefresh(random, lastClearedWave, out next) || next == null) return false;
            offered = next;
            ui.UpgradePanel.Show(offered, Progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails, SelectUpgrade, TryRefreshOffer);
            return true;
        }

        public void SelectUpgrade(EmberOffer offer)
        {
            if (State != Mode.Upgrade) return;
            bool ok = false;
            for (int i = 0; i < offered.Length; i++)
                if (offered[i].Id == offer.Id && offered[i].Rarity == offer.Rarity && offered[i].WeaponId == offer.WeaponId) { ok = true; break; }
            if (!ok) return;
            bool grantedNew;
            if (!Progress.Choose(offer, out grantedNew)) return;
            EmberAudio.Ensure().PlayCardPick();

            if (!compensationPick) pendingWavePicks = Mathf.Max(0, pendingWavePicks - 1);
            // New weapon → one immediate compensation per scheduled choice.
            if (grantedNew && !compensationPick)
            {
                compensationPick = true;
                Progress.OfferChoices();
                OfferUpgrade(lastClearedWave);
                return;
            }

            compensationPick = false;
            if (pendingWavePicks > 0) { Progress.OfferChoices(); OfferUpgrade(lastClearedWave); return; }
            combat.AdvanceAfterUpgrade();
            SetPlaying();
        }

        public void Pause()
        {
            if (State != Mode.Playing) return;
            State = Mode.Paused;
            stick = Vector2.zero;
            ui.BattleHud.Show(false);
            ui.StickBase.gameObject.SetActive(false);
            if (ui.UpgradePanel != null) ui.UpgradePanel.Hide();
            if (ui.GodsSelect != null) ui.GodsSelect.Hide();
            if (ui.SettingsPanel != null)
                ui.SettingsPanel.Show(SetPlaying, true, ReturnToCamp);
            else
                ui.Show(EmberCopy.PauseTitle, EmberCopy.PauseBody, new[] { EmberCopy.PauseChoice }, new UnityEngine.Events.UnityAction[] { SetPlaying });
        }

        void ReturnToCamp()
        {
            Time.timeScale = 1f;
            pointer = -1;
            stick = Vector2.zero;
            pendingWavePicks = 0;
            compensationPick = false;

            if (ui != null)
            {
                if (ui.UpgradePanel != null) ui.UpgradePanel.Hide();
                if (ui.GodsSelect != null) ui.GodsSelect.Hide();
                if (ui.BattleHud != null) ui.BattleHud.Show(false);
                if (ui.StickBase != null) ui.StickBase.gameObject.SetActive(false);
            }

            if (combat != null) combat.TearDown();
            if (world != null)
            {
                world.gameObject.SetActive(false);
                Destroy(world.gameObject);
            }
            world = null;
            player = null;
            Progress = null;
            menuFlow.ShowMainMenu();
        }

        void OnApplicationPause(bool paused) { if (paused) Pause(); }
        void OnApplicationFocus(bool focused) { if (!focused) Pause(); }

        void End(bool won)
        {
            if (State == Mode.Won || State == Mode.Lost || Progress == null) return;
            State = won ? Mode.Won : Mode.Lost;
            EmberAudio.Ensure().PlayMenuMusic();
            ui.Show(won ? EmberCopy.WinTitle : EmberCopy.LoseTitle,
                LevelConfig.DifficultyName(runLevel.Difficulty) + " · " + runLevel.BossWave + " 波征程"
                + "\n到达第 " + combat.Wave + " 波 · 生存 " + Elapsed.ToString("0") + " 秒"
                + "\n击杀 " + Kills + " · 余烬 " + Progress.FinalScore(),
                new[] { EmberCopy.RestartChoice, EmberCopy.ReturnChoice }, new UnityEngine.Events.UnityAction[] { menuFlow.BeginRun, menuFlow.ShowMainMenu });
        }

        void ReadInput()
        {
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began && pointer == -1 && t.position.y < Screen.height * .7f)
                    {
                        pointer = t.fingerId;
                        origin = t.position;
                    }
                    if (t.fingerId == pointer)
                    {
                        stick = Vector2.ClampMagnitude((t.position - origin) / (Screen.width * .14f), 1);
                        if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                        {
                            pointer = -1;
                            stick = Vector2.zero;
                        }
                    }
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0) && Input.mousePosition.y < Screen.height * .7f)
                {
                    pointer = -2;
                    origin = Input.mousePosition;
                }
                if (pointer == -2) stick = Vector2.ClampMagnitude(((Vector2)Input.mousePosition - origin) / (Screen.width * .14f), 1);
                if (Input.GetMouseButtonUp(0))
                {
                    pointer = -1;
                    stick = Vector2.zero;
                }
            }
            ui.SetStick(pointer != -1, origin, stick);
            if (Input.GetKeyDown(KeyCode.Escape)) Pause();
        }

        // ── IEmberMenuHost：界面流程的回调宿主 ─────────────────────────────
        // 状态与玩法仍归 EmberGame；界面流程只通过这三个入口请求动作。

        /// <summary>界面流程请求切换局长阶段。</summary>
        public void EnterMode(Mode mode) { State = mode; }

        /// <summary>界面流程回主菜单时，收起上一局的世界。</summary>
        public void HideRunWorld() { if (world != null) world.gameObject.SetActive(false); }

        /// <summary>起始武器选完，正式开一局。</summary>
        public void StartRun(int[] weapons, int slots) { StartRunWith(weapons, slots); }
    }
}
