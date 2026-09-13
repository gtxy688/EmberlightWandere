using UnityEngine;
using TMPro;

namespace Emberlight
{
    /// <summary>Run director: state machine, start, input, upgrades, results.</summary>
    public sealed class EmberGame : MonoBehaviour
    {
        public enum Mode { Loading, Menu, Playing, Upgrade, Paused, Lost, Won }

        public static readonly string[] UpgradeNames =
        {
            "\u53cc\u751f\u706b",
            "\u75be\u901f\u71c3\u70e7",
            "\u70bd\u70ed\u4e4b\u7130",
            "\u73af\u7ed5\u706b\u79cd",
            "\u8e0f\u706b\u800c\u884c",
            "\u706f\u57df\u6269\u5f20",
            "\u8f7b\u76c8\u6b65\u4f10",
            "\u6696\u706f\u5e87\u62a4",
            "\u4f59\u70ec\u706b\u6d6a",
            "\u706f\u706b\u56de\u6625"
        };
        public static readonly string[] UpgradeDetails =
        {
            "\u6bcf\u6b21\u653b\u51fb\u989d\u5916\u53d1\u5c04\u706b\u7403",
            "\u63d0\u9ad8\u653b\u51fb\u901f\u5ea6",
            "\u63d0\u9ad8\u706b\u7403\u4f24\u5bb3",
            "\u589e\u52a0\u73af\u7ed5\u706b\u79cd",
            "\u7559\u4e0b\u706b\u7130\u8def\u5f84\uff0c\u9020\u6210\u6301\u7eed\u4f24\u5bb3",
            "\u6269\u5927\u4f59\u70ec\u62fe\u53d6\u8303\u56f4",
            "\u63d0\u9ad8\u79fb\u52a8\u901f\u5ea6",
            "\u63d0\u9ad8\u751f\u547d\u4e0a\u9650\uff0c\u5e76\u6062\u590d\u751f\u547d",
            "\u5468\u671f\u91ca\u653e\u706b\u6d6a",
            "\u6062\u590d\u751f\u547d"
        };

        public Mode State { get; private set; }
        public RunProgress Progress { get; private set; }
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        public float Health { get; private set; }
        public bool HurtReady { get { return hurtTimer <= 0; } }
        public int EnemyCount { get { return combat != null ? combat.EnemyCount : 0; } }

        EmberCombat combat;
        EmberMenuUi ui;
        Transform world, player;
        Camera cam;
        float hurtTimer;
        int pointer = -1;
        Vector2 origin, stick;
        EmberOffer[] offered = new EmberOffer[0];
        readonly System.Random random = new System.Random();

        float MaxHealth { get { return 100 + Progress.BonusMaxHealth; } }

        void Start()
        {
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
            }, BeginRun);
        }

        void OnDestroy() { Time.timeScale = 1; }

        public void BeginRun()
        {
            if (ui == null || ui.BattleHud == null) return;
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
            Health = 100;
            Elapsed = 0;
            Kills = 0;
            hurtTimer = 0;
            player = EmberVisuals.Character("Keeper", world, new Color(.22f, .40f, .63f), true);
            var deco = new System.Random(73);
            for (int i = 0; i < 160; i++)
            {
                Vector2 p = new Vector2((float)deco.NextDouble() * 46 - 23, (float)deco.NextDouble() * 46 - 23);
                EmberVisuals.Shape("Ruin stone", world, p, new Vector2(.3f + (float)deco.NextDouble(), .18f), new Color(.10f, .15f, .19f), 0);
            }
            var level = LevelConfig.Default;
            level.ApplyWorld();
            EmberWorld.BuildBoundary(world);
            combat.Begin(world, player, cam, effects, level);
            UpdateCamera();
            SetPlaying();
        }

        void SetPlaying()
        {
            if (ui.UpgradePanel != null) ui.UpgradePanel.Hide();
            State = Mode.Playing;
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
            // Upgrades open only via OnWaveCleared (no XP Pending path).
            UpdateHud();
        }

        void UpdateHud()
        {
            if (ui == null || ui.BattleHud == null || Progress == null) return;
            int wave = combat != null ? combat.Wave : 1;
            int remaining = combat != null ? combat.WaveRemaining : 0;
            int quota = combat != null ? combat.WaveQuota : 0;
            ui.BattleHud.Set(Health, MaxHealth, Progress, Elapsed, Kills, wave, remaining, quota);
            var boss = combat.Boss;
            ui.BattleHud.Boss(
                boss == null ? 0 : boss.hp / boss.maxHp,
                boss == null ? "" : boss.ai.Phase,
                boss != null && State == Mode.Playing);
        }

        public void HurtPlayer(float damage)
        {
            if (State != Mode.Playing || hurtTimer > 0) return;
            Health = Mathf.Max(0, Health - damage);
            hurtTimer = .65f;
            UpdateHud();
            if (Health <= 0) EndRun(false);
        }

        public void PlayBossBurst(Vector2 point) { combat.PlayBossBurst(point); }
        public void PreviewBoss() { combat.PreviewBoss(); }
        public void RegisterKill() { Kills++; }
        public void EndRun(bool won) { End(won); }

        void UpdateCamera() { cam.transform.position = new Vector3(player.position.x, player.position.y, -10); }

        /// <summary>Wave 1-5 clear: forced 3-choice card, then AdvanceAfterUpgrade on Choose.</summary>
        public void OnWaveCleared(int clearedWave)
        {
            if (State != Mode.Playing || Progress == null) return;
            Progress.OfferChoices();
            OfferUpgrade(clearedWave);
        }

        public void OfferUpgrade(int clearedWave)
        {
            State = Mode.Upgrade;
            ui.BattleHud.Show(false);
            stick = Vector2.zero;
            offered = Progress.Choices(random, clearedWave);
            ui.Overlay.SetActive(false);
            ui.StickBase.gameObject.SetActive(false);
            ui.UpgradePanel.Show(offered, Progress, UpgradeNames, UpgradeDetails, SelectUpgrade);
        }

        public void SelectUpgrade(EmberOffer offer)
        {
            if (State != Mode.Upgrade) return;
            bool ok = false;
            for (int i = 0; i < offered.Length; i++)
                if (offered[i].Id == offer.Id && offered[i].Rarity == offer.Rarity) { ok = true; break; }
            if (!ok || !Progress.Choose(offer)) return;
            if (offer.Id == 7) Health = Mathf.Min(MaxHealth, Health + Progress.HealAmount(offer.Rarity));
            if (offer.Id == 9) Health = Mathf.Min(MaxHealth, Health + Progress.HealAmount(offer.Rarity));
            combat.AdvanceAfterUpgrade();
            SetPlaying();
        }

        public void Pause()
        {
            if (State != Mode.Playing) return;
            State = Mode.Paused;
            stick = Vector2.zero;
            ui.Show("\u6682\u6b47\u706f\u7554", "\u7a0d\u4f5c\u4f11\u606f\uff0c\u518d\u8d74\u957f\u591c\u3002", new[] { "\u7ee7\u7eed\u6218\u6597" }, new UnityEngine.Events.UnityAction[] { SetPlaying });
        }

        void OnApplicationPause(bool paused) { if (paused) Pause(); }
        void OnApplicationFocus(bool focused) { if (!focused) Pause(); }

        void End(bool won)
        {
            State = won ? Mode.Won : Mode.Lost;
            int best = PlayerPrefs.GetInt("Emberlight.BestKills", 0);
            if (Kills > best)
            {
                best = Kills;
                PlayerPrefs.SetInt("Emberlight.BestKills", best);
                PlayerPrefs.Save();
            }
            int score = Progress != null ? Progress.Score : 0;
            ui.Show(
                won ? "\u957f\u591c\u7834\u6653" : "\u706f\u706b\u6682\u7184",
                string.Format("\u751f\u5b58\u0020\u007b\u0030\u003a\u0030\u007d\u0020\u79d2\u0020\u00b7\u0020\u51fb\u6740\u0020\u007b\u0031\u007d\u000a\u4f59\u70ec\u0020\u007b\u0032\u007d\u000a\u6700\u9ad8\u7eaa\u5f55\uff1a\u51fb\u6740\u0020\u007b\u0033\u007d", Elapsed, Kills, score, best),
                new[] { "\u518d\u6218\u4e00\u6b21" },
                new UnityEngine.Events.UnityAction[] { BeginRun });
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
