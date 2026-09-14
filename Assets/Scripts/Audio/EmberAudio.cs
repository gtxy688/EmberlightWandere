using UnityEngine;

namespace Emberlight
{
    /// <summary>Audio-v1 bus: 2D music + SFX, PlayerPrefs volumes, silent-with-warn missing clips.</summary>
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

        AudioSource music;
        AudioSource sfx;
        float nextFireTime;
        float nextHitTime;
        float nextMeteorImpactTime;
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

        public void StopMusic()
        {
            if (music != null) music.Stop();
            currentMusic = null;
        }

        public void PlaySfx(AudioClip clip)
        {
            if (sfx == null) Bootstrap();
            EnsureListener();
            if (clip == null) return;
            sfx.spatialBlend = 0f;
            sfx.PlayOneShot(clip, sfxVolume);
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
            if (cardOpen == null) cardOpen = LoadClip("Audio/Sfx/card_open");
            PlaySfx(cardOpen);
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

        /// <summary>Meteor ground impact — reuses hit clip with looser throttle so multi-drops stay audible.</summary>
        public void PlayMeteorImpact()
        {
            if (Time.unscaledTime < nextMeteorImpactTime) return;
            nextMeteorImpactTime = Time.unscaledTime + MeteorImpactMinInterval;
            if (hit == null) hit = LoadClip("Audio/Sfx/hit");
            PlaySfx(hit);
        }
    }
}
