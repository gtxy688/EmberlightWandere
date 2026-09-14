using UnityEngine;
using TMPro;

namespace Emberlight
{
    /// <summary>Run director: Gods-Select-v3 pre-run (N + roster), wave offers, compensation, refreshes.</summary>
    public sealed class EmberGame : MonoBehaviour
    {
        public enum Mode { Loading, Menu, SelectLength, SelectDifficulty, SelectSlots, SelectRoster, Playing, Upgrade, Paused, Lost, Won }

        public static readonly string[] UpgradeNames =
        {
            "\u653b\u51fb\u529b",
            "\u653b\u901f",
            "\u5e78\u8fd0",
            "\u4f24\u5bb3\u589e\u5e45",
            "护盾", "", "", "", "", "",
            "\u7a7f\u900f\u706b\u77e2",
            "\u56de\u65cb\u70ec\u8776",
            "\u5929\u964d\u706b\u96e8",
            "\u706b\u7403",
            "\u73af\u706b",
            "\u71c3\u5730",
            "", "", "", "",
            "\u71ce\u539f",
            "\u65e5\u5195",
            "\u706b\u6d77",
            "\u7a7f\u6768",
            "\u6298\u8fd4\u4e0d\u5c3d",
            "\u5929\u706b",
            "", "", "", "",
            "\u8fde\u53d1",
            "\u6dfb\u85aa",
            "\u5ef6\u71c3",
            "\u8d2f\u7a7f",
            "\u56de\u9a6c",
            "\u591a\u843d\u70b9"
        };
                public static readonly string[] UpgradeDetails =
        {
            "\u6253\u5f97\u66f4\u75bc",
            "\u6253\u5f97\u66f4\u5feb",
            "\u66f4\u5bb9\u6613\u5237\u51fa\u597d\u5361",
            "\u6240\u6709\u6b66\u5668\u4f24\u5bb3\u518d\u4e58\u4e00\u622a",
            "立即获得护盾，受伤时优先消耗；可叠加，仅本局有效",
            "",
            "",
            "",
            "",
            "",
            "\u7ad9\u4f4f\u5f00\u706b\uff0c\u4e00\u7bad\u7a7f\u8fc7\u53bb",
            "\u4e22\u51fa\u53bb\u518d\u98de\u56de\u6765\u6253\u602a",
            "\u5148\u4eae\u5708\uff0c\u518d\u7838\u4e0b\u4e00\u7247\u706b",
            "\u81ea\u52a8\u6254\u706b\u7403",
            "\u706b\u56e2\u56f4\u7740\u4f60\u8f6c",
            "\u8d70\u8fc7\u7684\u5730\u65b9\u7740\u706b",
            "",
            "",
            "",
            "",
            "\u706b\u7403\u66f4\u5927\u66f4\u70eb\uff0c\u6253\u4e2d\u4f1a\u70e7\u5730",
            "\u706b\u56e2\u8f6c\u5f97\u66f4\u5927\u66f4\u5feb\u66f4\u70eb",
            "\u8eab\u8fb9\u4e00\u76f4\u6709\u4e00\u5708\u706b",
            "\u5c04\u4e2d\u540e\u6e85\u51fa\u4e00\u5c0f\u7bad",
            "\u591a\u98de\u4e00\u4e2a\u6765\u56de",
            "\u706b\u5708\u66f4\u5927\uff0c\u7838\u5f97\u66f4\u75bc",
            "",
            "",
            "",
            "",
            "\u591a\u6253\u51fa\u4e00\u4e2a\u706b\u7403",
            "\u591a\u4e00\u9897\u56f4\u7740\u8f6c\u7684\u706b",
            "\u706b\u70e7\u5f97\u66f4\u4e45\u66f4\u70eb",
            "\u80fd\u591a\u7a7f\u51e0\u4e2a\u602a",
            "\u53bb\u548c\u56de\u591a\u6253\u4e00\u4e0b",
            "\u591a\u7838\u4e00\u5904"
        };

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
        int selectedWaves = 25, pendingWavePicks;
        EmberDifficulty selectedDifficulty = EmberDifficulty.Standard;
        LevelConfig runLevel = LevelConfig.Default;
        Transform world, player;
        Camera cam;
        float hurtTimer;
        int pointer = -1;
        Vector2 origin, stick;
        EmberOffer[] offered = new EmberOffer[0];
        readonly System.Random random = new System.Random();
        int pendingSlots = 2;
        int[] pendingRoster = new int[0];
        int lastClearedWave;
        bool compensationPick;

        float MaxHealth { get { return 100 + (Progress != null ? Progress.BonusMaxHealth : 0); } }

        void Start()
        {
            EmberAudio.Ensure();
            State = Mode.Loading;
            combat = new EmberCombat(this);
            ui = new EmberMenuUi();
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
            gameObject.AddComponent<EmberIntro>().Begin(loadedFont =>
            {
                ui.Build(transform, loadedFont, Pause);
                State = Mode.Menu;
                ui.Overlay.SetActive(false);
                ui.BattleHud.Show(false);
            }, () => ShowMainMenu());
        }

        void OnDestroy() { Time.timeScale = 1; }

        public void ShowMainMenu()
        {
            State = Mode.Menu;
            EmberAudio.Ensure().PlayMenuMusic();
            if (world != null) world.gameObject.SetActive(false);
            ui.Show("烬灯行者", "25 波标准征程 · 50 波漫长征程\n自由选择难度，每局从相同的基础属性出发。\n清波选卡，击败最终守卫。",
                new[] { "提灯出发", "\u8bbe\u7f6e" }, new UnityEngine.Events.UnityAction[] { BeginRun, OpenSettingsFromMenu });
        }


        void OpenSettingsFromMenu()
        {
            if (ui == null || ui.SettingsPanel == null)
            {
                ShowMainMenu();
                return;
            }
            ui.Overlay.SetActive(false);
            ui.SettingsPanel.Show(ShowMainMenu, false);
        }

        /// <summary>Entry: pick N then single starter weapon (no skip).</summary>
        public void BeginRun()
        {
            if (ui == null || ui.BattleHud == null || ui.GodsSelect == null) return;
            if (ui.UpgradePanel != null) ui.UpgradePanel.Hide();
            ui.Overlay.SetActive(false);
            ui.BattleHud.Show(false);
            ui.StickBase.gameObject.SetActive(false);
            pendingSlots = 2;
            pendingRoster = new int[0];
            compensationPick = false;
            State = Mode.SelectLength;
            ui.Show("选择征程", "局长与难度独立选择。\n前 10 波每波选卡，之后每 2 波选卡；阶段 Boss 额外奖励一次。",
                new[] { "标准征程 · 25 波（推荐）", "漫长征程 · 50 波", "返回营地" },
                new UnityEngine.Events.UnityAction[] { () => ChooseLength(25), () => ChooseLength(50), ShowMainMenu });
        }

        void ChooseLength(int waves)
        {
            selectedWaves = waves;
            State = Mode.SelectDifficulty;
            ui.Show("选择难度", selectedWaves + " 波征程\n休闲适合轻松构筑，标准体验完整挑战，困难面对更多精英。",
                new[] { "休闲 · 怪物更少，伤害更低", "标准 · 均衡挑战", "困难 · 更多怪物与精英" },
                new UnityEngine.Events.UnityAction[] { () => ChooseDifficulty(EmberDifficulty.Casual),
                    () => ChooseDifficulty(EmberDifficulty.Standard), () => ChooseDifficulty(EmberDifficulty.Hard) });
        }

        void ChooseDifficulty(EmberDifficulty difficulty)
        {
            selectedDifficulty = difficulty;
            ui.Overlay.SetActive(false);
            State = Mode.SelectSlots;
            ui.GodsSelect.ShowSlots(OnSlotsChosen);
        }

        void OnSlotsChosen(int slots)
        {
            pendingSlots = slots;
            State = Mode.SelectRoster;
            ui.GodsSelect.ShowRoster(slots, OnRosterChosen);
        }

        void OnRosterChosen(int[] weapons)
        {
            pendingRoster = weapons ?? new[] { RunProgress.WeaponBasic };
            StartRunWith(pendingRoster, pendingSlots);
        }


        void StartRunWith(int[] weapons, int slots)
        {
            if (ui == null || ui.BattleHud == null) return;
            runLevel = LevelConfig.Create(selectedDifficulty, selectedWaves);
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
            player = EmberVisuals.Character("Keeper", world, new Color(.22f, .40f, .63f), true);
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
            ui.UpdateSafeArea();
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
        public void PreviewBoss() { combat.PreviewBoss(); }
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
            ui.UpgradePanel.Show(offered, Progress, UpgradeNames, UpgradeDetails, SelectUpgrade, TryRefreshOffer);
        }

        /// <summary>UI API: spend a free refresh and rebuild the offer panel.</summary>
        public bool TryRefreshOffer()
        {
            if (State != Mode.Upgrade || Progress == null) return false;
            EmberOffer[] next;
            if (!Progress.TryRefresh(random, lastClearedWave, out next) || next == null) return false;
            offered = next;
            ui.UpgradePanel.Show(offered, Progress, UpgradeNames, UpgradeDetails, SelectUpgrade, TryRefreshOffer);
            return true;
        }

        public void SelectUpgrade(EmberOffer offer)
        {
            if (State != Mode.Upgrade) return;
            bool ok = false;
            for (int i = 0; i < offered.Length; i++)
                if (offered[i].Id == offer.Id && offered[i].Rarity == offer.Rarity) { ok = true; break; }
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
                ui.SettingsPanel.Show(SetPlaying, true);
            else
                ui.Show("\u6682\u6b47\u706f\u7554", "\u7a0d\u4f5c\u4f11\u606f\uff0c\u518d\u8d74\u957f\u591c\u3002", new[] { "\u7ee7\u7eed\u6218\u6597" }, new UnityEngine.Events.UnityAction[] { SetPlaying });
        }

        void OnApplicationPause(bool paused) { if (paused) Pause(); }
        void OnApplicationFocus(bool focused) { if (!focused) Pause(); }

        void End(bool won)
        {
            if (State == Mode.Won || State == Mode.Lost || Progress == null) return;
            State = won ? Mode.Won : Mode.Lost;
            EmberAudio.Ensure().PlayMenuMusic();
            ui.Show(won ? "长夜破晓" : "灯火暂熄",
                LevelConfig.DifficultyName(runLevel.Difficulty) + " · " + runLevel.BossWave + " 波征程"
                + "\n到达第 " + combat.Wave + " 波 · 生存 " + Elapsed.ToString("0") + " 秒"
                + "\n击杀 " + Kills + " · 余烬 " + Progress.FinalScore(),
                new[] { "再战一次", "返回营地" }, new UnityEngine.Events.UnityAction[] { BeginRun, ShowMainMenu });
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
    }
}
