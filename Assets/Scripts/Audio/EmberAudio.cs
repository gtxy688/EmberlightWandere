using UnityEngine;

namespace Emberlight
{
    /// <summary>音频总线：2D 音乐 + 音效，音量存 PlayerPrefs，clip 缺失时静默降级并告警。</summary>
    public sealed class EmberAudio : MonoBehaviour
    {
        public static EmberAudio Instance { get; private set; }

        const float FireHitMinInterval = 0.05f;
        const float MeteorImpactMinInterval = 0.02f;
        const string PrefMusic = "ember_vol_music";
        const string PrefSfx = "ember_vol_sfx";

        [Header("Volumes")]
        [SerializeField] float musicVolume = 0.7f;
        [SerializeField] float sfxVolume = 1f;

        [Header("Music")]
        [SerializeField] AudioClip menuAmbient;
        [SerializeField] AudioClip combatLoop;

        [Header("SFX")]
        [SerializeField] AudioClip uiClick;
        [SerializeField] AudioClip uiConfirm;
        [SerializeField] AudioClip fire;
        [SerializeField] AudioClip hit;
        [SerializeField] AudioClip hurt;
        [SerializeField] AudioClip pickupHeal;
        [SerializeField] AudioClip cardOpen;
        [SerializeField] AudioClip cardPick;
        [SerializeField] AudioClip meteorImpact;

        [Header("Weapon SFX (fall back to fire/meteorImpact when missing)")]
        [SerializeField] AudioClip weaponFireball;
        [SerializeField] AudioClip weaponOrbitIgnite;
        [SerializeField] AudioClip weaponBurnGround;
        [SerializeField] AudioClip weaponPierceArrow;
        [SerializeField] AudioClip weaponBoomerang;
        [SerializeField] AudioClip weaponMeteor;

        AudioSource music;
        AudioSource sfx;
        float nextFireTime;
        float nextHitTime;
        float nextMeteorImpactTime;
        float nextWeaponShotTime;
        float nextWeaponArrowTime;
        float nextWeaponBoomTime;
        float nextWeaponBurnTime;
        float nextWeaponOrbitTime;
        AudioClip currentMusic;

        public float MusicVolume
        {
            get { return musicVolume; }
            set
            {
                musicVolume = Mathf.Clamp01(value);
                if (music != null) music.volume = musicVolume;
                PlayerPrefs.SetFloat(PrefMusic, musicVolume);
                PlayerPrefs.Save();
            }
        }

        public float SfxVolume
        {
            get { return sfxVolume; }
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                if (sfx != null) sfx.volume = sfxVolume;
                PlayerPrefs.SetFloat(PrefSfx, sfxVolume);
                PlayerPrefs.Save();
            }
        }

        public static EmberAudio Ensure()
        {
            if (Instance != null)
            {
                Instance.EnsureListener();
                return Instance;
            }
            var existing = Object.FindObjectOfType<EmberAudio>();
            if (existing != null)
            {
                Instance = existing;
                existing.Bootstrap();
                return existing;
            }
            var go = new GameObject("EmberAudio");
            Object.DontDestroyOnLoad(go);
            var audio = go.AddComponent<EmberAudio>();
            audio.Bootstrap();
            return audio;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Bootstrap();
        }

        void Bootstrap()
        {
            if (PlayerPrefs.HasKey(PrefMusic)) musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMusic, 0.7f));
            else musicVolume = 0.7f;
            if (PlayerPrefs.HasKey(PrefSfx)) sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfx, 1f));
            else sfxVolume = 1f;

            if (music == null)
            {
                music = gameObject.AddComponent<AudioSource>();
                music.playOnAwake = false;
                music.loop = true;
            }
            if (sfx == null)
            {
                sfx = gameObject.AddComponent<AudioSource>();
                sfx.playOnAwake = false;
                sfx.loop = false;
            }

            music.spatialBlend = 0f;
            sfx.spatialBlend = 0f;
            music.ignoreListenerPause = true;
            sfx.ignoreListenerPause = true;
            music.volume = musicVolume;
            sfx.volume = sfxVolume;

            EnsureListener();
            TryLoadFromResources();
        }

        void EnsureListener()
        {
            if (Object.FindObjectOfType<AudioListener>() != null) return;
            var cam = Camera.main;
            if (cam != null)
            {
                cam.gameObject.AddComponent<AudioListener>();
                return;
            }
            var go = new GameObject("EmberAudioListener");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<AudioListener>();
        }

        AudioClip LoadClip(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null)
                Debug.LogWarning("[EmberAudio] missing clip Resources.Load(\"" + path + "\")");
            return clip;
        }

        void TryLoadFromResources()
        {
            if (menuAmbient == null) menuAmbient = LoadClip("Audio/Music/menu_ambient");
            if (combatLoop == null) combatLoop = LoadClip("Audio/Music/combat_loop");
            if (uiClick == null) uiClick = LoadClip("Audio/Sfx/ui_click");
            if (uiConfirm == null) uiConfirm = LoadClip("Audio/Sfx/ui_confirm");
            if (fire == null) fire = LoadClip("Audio/Sfx/fire");
            if (hit == null) hit = LoadClip("Audio/Sfx/hit");
            if (hurt == null) hurt = LoadClip("Audio/Sfx/hurt");
            if (pickupHeal == null) pickupHeal = LoadClip("Audio/Sfx/pickup_heal");
            if (cardOpen == null) cardOpen = LoadClip("Audio/Sfx/card_open");
            if (cardPick == null) cardPick = LoadClip("Audio/Sfx/card_pick");
            if (meteorImpact == null) meteorImpact = LoadClip("Audio/Sfx/meteor_impact");
            if (weaponFireball == null) weaponFireball = LoadWeaponClip("weapon_fireball");
            if (weaponOrbitIgnite == null) weaponOrbitIgnite = LoadWeaponClip("weapon_orbit_ignite");
            if (weaponBurnGround == null) weaponBurnGround = LoadWeaponClip("weapon_burn_ground");
            if (weaponPierceArrow == null) weaponPierceArrow = LoadWeaponClip("weapon_pierce_arrow");
            if (weaponBoomerang == null) weaponBoomerang = LoadWeaponClip("weapon_boomerang");
            if (weaponMeteor == null) weaponMeteor = LoadWeaponClip("weapon_meteor");
        }

        /// <summary>优先取动作对齐的短 clip，缺失时回落原始 clip。</summary>
        AudioClip LoadWeaponClip(string fileNameNoExt)
        {
            var clip = Resources.Load<AudioClip>("Audio/Sfx/Generated/" + fileNameNoExt);
            if (clip != null) return clip;
            return LoadClip("Audio/Sfx/" + fileNameNoExt);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (music == null) Bootstrap();
            EnsureListener();
            if (clip == null)
            {
                music.Stop();
                currentMusic = null;
                return;
            }
            if (currentMusic == clip && music.isPlaying) return;
            currentMusic = clip;
            music.clip = clip;
            music.spatialBlend = 0f;
            music.volume = musicVolume;
            music.loop = true;
            music.Play();
        }

        public void PlayMenuMusic()
        {
            if (menuAmbient == null) menuAmbient = LoadClip("Audio/Music/menu_ambient");
            PlayMusic(menuAmbient);
        }

        public void PlayCombatMusic()
        {
            if (combatLoop == null) combatLoop = LoadClip("Audio/Music/combat_loop");
            PlayMusic(combatLoop);
        }

        public void PlaySfx(AudioClip clip, float startTime = 0f, float gain = 1f)
        {
            if (sfx == null) Bootstrap();
            EnsureListener();
            if (clip == null) return;
            sfx.spatialBlend = 0f;
            startTime = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clip.length - 0.02f));
            if (startTime <= 0.001f)
            {
                sfx.PlayOneShot(clip, Mathf.Clamp01(gain));
                return;
            }
            // PlayOneShot 无法跳转起点，另起一次性 2D 源，让起音对齐事件帧。
            var go = new GameObject("EmberSfxOneShot");
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            src.ignoreListenerPause = true;
            src.clip = clip;
            src.volume = sfxVolume * Mathf.Clamp01(gain);
            src.time = startTime;
            src.Play();
            Object.Destroy(go, clip.length - startTime + 0.05f);
        }

        public void PlayUiClick()
        {
            if (uiClick == null) uiClick = LoadClip("Audio/Sfx/ui_click");
            PlaySfx(uiClick);
        }

        public void PlayUiConfirm()
        {
            if (uiConfirm == null) uiConfirm = LoadClip("Audio/Sfx/ui_confirm");
            PlaySfx(uiConfirm);
        }

        public void PlayCardOpen()
        {
            PlayCardPick();
        }

        public void PlayCardPick()
        {
            if (cardPick == null) cardPick = LoadClip("Audio/Sfx/card_pick");
            PlaySfx(cardPick != null ? cardPick : uiConfirm);
        }

        public void PlayHurt()
        {
            if (hurt == null) hurt = LoadClip("Audio/Sfx/hurt");
            PlaySfx(hurt);
        }

        public void PlayPickupHeal()
        {
            if (pickupHeal == null) pickupHeal = LoadClip("Audio/Sfx/pickup_heal");
            PlaySfx(pickupHeal);
        }

        public void PlayFire()
        {
            if (Time.unscaledTime < nextFireTime) return;
            nextFireTime = Time.unscaledTime + FireHitMinInterval;
            if (fire == null) fire = LoadClip("Audio/Sfx/fire");
            PlaySfx(fire);
        }

        public void PlayHit()
        {
            if (Time.unscaledTime < nextHitTime) return;
            nextHitTime = Time.unscaledTime + FireHitMinInterval;
            if (hit == null) hit = LoadClip("Audio/Sfx/hit");
            PlaySfx(hit);
        }

        /// <summary>陨石落地（Kenney lowFrequency_explosion）；多点位连续落地共用同一节流。</summary>
        public void PlayMeteorImpact()
        {
            if (Time.unscaledTime < nextMeteorImpactTime) return;
            nextMeteorImpactTime = Time.unscaledTime + MeteorImpactMinInterval;
            if (weaponMeteor == null) weaponMeteor = LoadWeaponClip("weapon_meteor");
            if (weaponMeteor != null)
            {
                // 落地音从采样 0 起播，与伤害同帧。
                PlaySfx(weaponMeteor, 0f, .75f);
                return;
            }
            if (meteorImpact == null) meteorImpact = LoadClip("Audio/Sfx/meteor_impact");
            if (meteorImpact != null) { PlaySfx(meteorImpact); return; }
            if (hit == null) hit = LoadClip("Audio/Sfx/hit");
            PlaySfx(hit);
        }

        /// <summary>专用短促发射音，增益低于落地音。</summary>
        public void PlayWeaponFireball()
        {
            if (Time.unscaledTime < nextWeaponShotTime) return;
            nextWeaponShotTime = Time.unscaledTime + FireHitMinInterval;
            if (weaponFireball == null) weaponFireball = LoadWeaponClip("weapon_fireball");
            if (weaponFireball != null) { PlaySfx(weaponFireball, 0f, .45f); return; }
            PlayFire();
        }

        public void PlayWeaponPierceArrow()
        {
            if (Time.unscaledTime < nextWeaponArrowTime) return;
            nextWeaponArrowTime = Time.unscaledTime + FireHitMinInterval;
            if (weaponPierceArrow == null) weaponPierceArrow = LoadWeaponClip("weapon_pierce_arrow");
            // 短促发射音与弹丸生成同帧起播。
            if (weaponPierceArrow != null) { PlaySfx(weaponPierceArrow, 0f, .55f); return; }
            PlayFire();
        }

        public void PlayWeaponBoomerang()
        {
            if (Time.unscaledTime < nextWeaponBoomTime) return;
            nextWeaponBoomTime = Time.unscaledTime + FireHitMinInterval;
            if (weaponBoomerang == null) weaponBoomerang = LoadWeaponClip("weapon_boomerang");
            if (weaponBoomerang != null) { PlaySfx(weaponBoomerang, 0f, .45f); return; }
            PlayFire();
        }

        /// <summary>仅在火环接触真正扣血之后调用。</summary>
        public void PlayWeaponOrbitContact()
        {
            if (Time.unscaledTime < nextWeaponOrbitTime) return;
            nextWeaponOrbitTime = Time.unscaledTime + 0.35f;
            if (weaponOrbitIgnite == null) weaponOrbitIgnite = LoadWeaponClip("weapon_orbit_ignite");
            if (weaponOrbitIgnite != null) { PlaySfx(weaponOrbitIgnite, 0f, .28f); return; }
            PlayFire();
        }

        /// <summary>仅在地面火焰造成伤害之后调用；重叠火区共用同一节流。</summary>
        public void PlayWeaponBurnGround()
        {
            if (Time.unscaledTime < nextWeaponBurnTime) return;
            nextWeaponBurnTime = Time.unscaledTime + .7f;
            if (weaponBurnGround == null) weaponBurnGround = LoadWeaponClip("weapon_burn_ground");
            if (weaponBurnGround != null) { PlaySfx(weaponBurnGround, 0f, .35f); return; }
            PlayFire();
        }
    }
}
