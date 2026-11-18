using UnityEngine;

namespace ArcheryGame.Audio
{
    /// <summary>
    /// Centralizes playback of game sound effects (SFX) and looping background music.
    /// Automatically initializes on scene load and loads clips from Resources/Audio/SFX
    /// if not assigned in the Inspector.
    /// </summary>
    public class GameAudioManager : MonoBehaviour
    {
        public const string BowDrawResourcePath = "Audio/SFX/bow_draw";
        public const string BowReleaseResourcePath = "Audio/SFX/bow_release";
        public const string ArrowHitTargetResourcePath = "Audio/SFX/arrow_hit_target";
        public const string TargetScoreDingResourcePath = "Audio/SFX/target_score_ding";
        public const string UiClickResourcePath = "Audio/SFX/ui_click";
        public const string TargetActionOrMissResourcePath = "Audio/SFX/target_action_or_miss";
        public const string BackgroundMusicResourcePath = "Audio/SFX/background_music";

        private static GameAudioManager instance;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip backgroundMusicClip;
        [SerializeField] private AudioClip bowDrawClip;
        [SerializeField] private AudioClip bowReleaseClip;
        [SerializeField] private AudioClip arrowHitTargetClip;
        [SerializeField] private AudioClip targetScoreClip;
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip targetActionOrMissClip;

        [Header("Volume Settings")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.25f;
        [SerializeField, Range(0f, 1f)] private float bowDrawVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float bowReleaseVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float arrowHitVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float targetScoreVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float uiClickVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float targetActionVolume = 0.9f;

        private AudioSource uiAudioSource;
        private AudioSource drawAudioSource;
        private AudioSource musicAudioSource;

        public static GameAudioManager Instance
        {
            get
            {
                if (instance != null) return instance;

                instance = FindFirstObjectByType<GameAudioManager>();
                if (instance == null && Application.isPlaying)
                {
                    GameObject audioObject = new GameObject("GameAudioManager");
                    instance = audioObject.AddComponent<GameAudioManager>();
                }

                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapOnSceneLoad()
        {
            if (Instance != null)
            {
                Instance.StartBackgroundMusic();
            }
        }

        public AudioClip BackgroundMusicClip => backgroundMusicClip;
        public AudioClip BowDrawClip => bowDrawClip;
        public AudioClip BowReleaseClip => bowReleaseClip;
        public AudioClip ArrowHitTargetClip => arrowHitTargetClip;
        public AudioClip TargetScoreClip => targetScoreClip;
        public AudioClip UiClickClip => uiClickClip;
        public AudioClip TargetActionOrMissClip => targetActionOrMissClip;
        public bool IsBowDrawPlaying => drawAudioSource != null && drawAudioSource.isPlaying;
        public bool IsBackgroundMusicPlaying => musicAudioSource != null && musicAudioSource.isPlaying;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                return;
            }

            instance = this;
            if (Application.isPlaying && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            Initialize();
            if (Application.isPlaying)
            {
                StartBackgroundMusic();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void Initialize()
        {
            LoadMissingClipsFromResources();
            EnsureAudioSources();
        }

        public void LoadMissingClipsFromResources()
        {
            if (backgroundMusicClip == null)
                backgroundMusicClip = Resources.Load<AudioClip>(BackgroundMusicResourcePath);
            if (bowDrawClip == null)
                bowDrawClip = Resources.Load<AudioClip>(BowDrawResourcePath);
            if (bowReleaseClip == null)
                bowReleaseClip = Resources.Load<AudioClip>(BowReleaseResourcePath);
            if (arrowHitTargetClip == null)
                arrowHitTargetClip = Resources.Load<AudioClip>(ArrowHitTargetResourcePath);
            if (targetScoreClip == null)
                targetScoreClip = Resources.Load<AudioClip>(TargetScoreDingResourcePath);
            if (uiClickClip == null)
                uiClickClip = Resources.Load<AudioClip>(UiClickResourcePath);
            if (targetActionOrMissClip == null)
                targetActionOrMissClip = Resources.Load<AudioClip>(TargetActionOrMissResourcePath);
        }

        private void EnsureAudioSources()
        {
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.GetComponent<AudioSource>();
                if (uiAudioSource == null)
                    uiAudioSource = gameObject.AddComponent<AudioSource>();

                uiAudioSource.playOnAwake = false;
                uiAudioSource.loop = false;
                uiAudioSource.spatialBlend = 0f; // 2D UI sound
            }

            if (musicAudioSource == null)
            {
                Transform existingMusicTransform = transform.Find("BackgroundMusicAudioSource");
                GameObject musicObj = existingMusicTransform != null
                    ? existingMusicTransform.gameObject
                    : new GameObject("BackgroundMusicAudioSource");

                musicObj.transform.SetParent(transform, false);
                musicAudioSource = musicObj.GetComponent<AudioSource>();
                if (musicAudioSource == null)
                    musicAudioSource = musicObj.AddComponent<AudioSource>();

                musicAudioSource.playOnAwake = false;
                musicAudioSource.loop = true;
                musicAudioSource.spatialBlend = 0f; // 2D ambient background music
            }

            if (drawAudioSource == null)
            {
                Transform existingDrawTransform = transform.Find("BowDrawAudioSource");
                GameObject drawObj = existingDrawTransform != null
                    ? existingDrawTransform.gameObject
                    : new GameObject("BowDrawAudioSource");

                drawObj.transform.SetParent(transform, false);
                drawAudioSource = drawObj.GetComponent<AudioSource>();
                if (drawAudioSource == null)
                    drawAudioSource = drawObj.AddComponent<AudioSource>();

                drawAudioSource.playOnAwake = false;
                drawAudioSource.loop = false;
                drawAudioSource.spatialBlend = 1f; // 3D spatial sound
                drawAudioSource.minDistance = 0.5f;
                drawAudioSource.maxDistance = 15f;
                drawAudioSource.rolloffMode = AudioRolloffMode.Linear;
            }
        }

        /// <summary>
        /// Starts playing the background music in loop if not already playing.
        /// </summary>
        public void StartBackgroundMusic()
        {
            Initialize();
            if (backgroundMusicClip == null || musicAudioSource == null) return;

            musicAudioSource.loop = true;
            musicAudioSource.volume = backgroundMusicVolume * masterVolume;

            if (musicAudioSource.isPlaying && musicAudioSource.clip == backgroundMusicClip)
                return;

            musicAudioSource.clip = backgroundMusicClip;
            musicAudioSource.Play();
        }

        /// <summary>
        /// Stops the background music.
        /// </summary>
        public void StopBackgroundMusic()
        {
            if (musicAudioSource != null && musicAudioSource.isPlaying)
            {
                musicAudioSource.Stop();
            }
        }

        /// <summary>
        /// Starts playing the bow draw tension sound at the specified world position.
        /// </summary>
        public void StartBowDraw(Vector3 position)
        {
            Initialize();
            if (bowDrawClip == null || drawAudioSource == null) return;

            drawAudioSource.transform.position = position;
            drawAudioSource.Stop();
            drawAudioSource.clip = bowDrawClip;
            drawAudioSource.volume = bowDrawVolume * masterVolume;
            drawAudioSource.pitch = 1f;
            drawAudioSource.Play();
        }

        /// <summary>
        /// Updates the 3D position of the active bow draw sound.
        /// </summary>
        public void UpdateBowDrawPosition(Vector3 position)
        {
            if (drawAudioSource != null)
            {
                drawAudioSource.transform.position = position;
            }
        }

        /// <summary>
        /// Stops the bow draw sound immediately (e.g., on release or cancel).
        /// </summary>
        public void StopBowDraw()
        {
            if (drawAudioSource != null && drawAudioSource.isPlaying)
            {
                drawAudioSource.Stop();
            }
        }

        /// <summary>
        /// Stops the bow draw sound and plays the arrow release/launch sound.
        /// </summary>
        public void PlayBowRelease(Vector3 position, float forceFactor = 1f)
        {
            StopBowDraw();
            Initialize();
            if (bowReleaseClip == null) return;

            float clampedForce = Mathf.Clamp01(forceFactor);
            float volume = Mathf.Lerp(0.75f, 1f, clampedForce) * bowReleaseVolume * masterVolume;
            float pitch = Mathf.Lerp(0.95f, 1.06f, clampedForce);
            PlaySpatialOneShot(bowReleaseClip, position, volume, pitch);
        }

        /// <summary>
        /// Plays both the arrow impact sound and the lowered-volume scoring chime when hitting a target.
        /// Detached from the target GameObject so target destruction does not cut off playback.
        /// </summary>
        public void PlayArrowHitTarget(Vector3 position)
        {
            Initialize();
            if (arrowHitTargetClip != null)
            {
                PlaySpatialOneShot(arrowHitTargetClip, position, arrowHitVolume * masterVolume, 1f);
            }

            if (targetScoreClip != null)
            {
                PlaySpatialOneShot(targetScoreClip, position, targetScoreVolume * masterVolume, 1f);
            }
        }

        /// <summary>
        /// Plays the arrow impact sound when hitting a wall or surface in the environment.
        /// </summary>
        public void PlaySurfaceHit(Vector3 position)
        {
            Initialize();
            if (arrowHitTargetClip == null) return;

            PlaySpatialOneShot(arrowHitTargetClip, position, arrowHitVolume * masterVolume, 1f);
        }

        /// <summary>
        /// Plays the target placement sound at the target's position in space.
        /// </summary>
        public void PlayTargetAction(Vector3 position)
        {
            Initialize();
            if (targetActionOrMissClip == null) return;

            PlaySpatialOneShot(targetActionOrMissClip, position, targetActionVolume * masterVolume, 1f);
        }

        /// <summary>
        /// Plays the 2D UI click sound.
        /// </summary>
        public void PlayUiClick()
        {
            Initialize();
            if (uiClickClip == null || uiAudioSource == null) return;

            uiAudioSource.PlayOneShot(uiClickClip, uiClickVolume * masterVolume);
        }

        private static void PlaySpatialOneShot(AudioClip clip, Vector3 position, float volume, float pitch)
        {
            if (clip == null || volume <= 0f) return;

            if (!Application.isPlaying)
                return;

            GameObject tempAudioObj = new GameObject($"OneShotAudio_{clip.name}");
            tempAudioObj.transform.position = position;

            AudioSource source = tempAudioObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 1f;
            source.minDistance = 1f;
            source.maxDistance = 25f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();

            float lifetime = clip.length / Mathf.Max(0.1f, Mathf.Abs(pitch)) + 0.1f;
            Destroy(tempAudioObj, lifetime);
        }
    }
}
