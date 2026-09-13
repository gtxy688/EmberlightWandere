using UnityEngine;

namespace Emberlight
{
    /// <summary>Audio-v1 minimal bus: one music loop + one-shot SFX. Missing clips are silent no-ops.</summary>
    public sealed class EmberAudio : MonoBehaviour
    {
        public static EmberAudio Instance { get; private set; }

        const float FireHitMinInterval = 0.05f;

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
        AudioClip currentMusic;

        public static EmberAudio Ensure()
        {
            if (Instance != null) return Instance;
            var existing = FindObjectOfType<EmberAudio>();
            if (existing != null)
            {
                Instance = existing;
                existing.Bootstrap();
                return existing;
            }
            var go = new GameObject("EmberAudio");
            DontDestroyOnLoad(go);
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
            music.volume = musicVolume;
            sfx.volume = sfxVolume;
            TryLoadFromResources();
        }

        void TryLoadFromResources()
        {
            // Prefer Assets/Resources/Audio/... (mirrors Docs/Audio-v1 names).
            if (menuAmbient == null) menuAmbient = Resources.Load<AudioClip>("Audio/Music/menu_ambient");
            if (combatLoop == null) combatLoop = Resources.Load<AudioClip>("Audio/Music/combat_loop");
            if (uiClick == null) uiClick = Resources.Load<AudioClip>("Audio/Sfx/ui_click");
            if (uiConfirm == null) uiConfirm = Resources.Load<AudioClip>("Audio/Sfx/ui_confirm");
            if (fire == null) fire = Resources.Load<AudioClip>("Audio/Sfx/fire");
            if (hit == null) hit = Resources.Load<AudioClip>("Audio/Sfx/hit");
            if (hurt == null) hurt = Resources.Load<AudioClip>("Audio/Sfx/hurt");
            if (pickupHeal == null) pickupHeal = Resources.Load<AudioClip>("Audio/Sfx/pickup_heal");
            if (cardOpen == null) cardOpen = Resources.Load<AudioClip>("Audio/Sfx/card_open");
            if (cardPick == null) cardPick = Resources.Load<AudioClip>("Audio/Sfx/card_pick");
        }

        public void PlayMusic(AudioClip clip)
        {
            if (music == null) Bootstrap();
            if (clip == null)
            {
                music.Stop();
                currentMusic = null;
                return;
            }
            if (currentMusic == clip && music.isPlaying) return;
            currentMusic = clip;
            music.clip = clip;
            music.volume = musicVolume;
            music.loop = true;
            music.Play();
        }

        public void PlayMenuMusic() { PlayMusic(menuAmbient); }
        public void PlayCombatMusic() { PlayMusic(combatLoop); }

        public void StopMusic()
        {
            if (music != null) music.Stop();
            currentMusic = null;
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfx == null) return;
            sfx.PlayOneShot(clip, sfxVolume);
        }

        public void PlayUiClick() { PlaySfx(uiClick); }
        public void PlayUiConfirm() { PlaySfx(uiConfirm); }
        public void PlayCardOpen() { PlaySfx(cardOpen); }
        public void PlayCardPick() { PlaySfx(cardPick != null ? cardPick : uiConfirm); }
        public void PlayHurt() { PlaySfx(hurt); }
        public void PlayPickupHeal() { PlaySfx(pickupHeal); }

        public void PlayFire()
        {
            if (Time.unscaledTime < nextFireTime) return;
            nextFireTime = Time.unscaledTime + FireHitMinInterval;
            PlaySfx(fire);
        }

        public void PlayHit()
        {
            if (Time.unscaledTime < nextHitTime) return;
            nextHitTime = Time.unscaledTime + FireHitMinInterval;
            PlaySfx(hit);
        }
    }
}
