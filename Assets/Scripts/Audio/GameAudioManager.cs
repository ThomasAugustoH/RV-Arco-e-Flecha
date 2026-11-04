using UnityEngine;

namespace ArcheryGame.Audio
{
    /// <summary>
    /// Centralizes playback of game sound effects (SFX) for bow actions,
    /// arrow impacts, target placement/removal, and UI interactions.
    /// Automatically loads clips from Resources/Audio/SFX if not assigned in the Inspector.
    /// </summary>
    public class GameAudioManager : MonoBehaviour
    {
        public const string BowDrawResourcePath = "Audio/SFX/bow_draw";
        public const string BowReleaseResourcePath = "Audio/SFX/bow_release";
        public const string ArrowHitTargetResourcePath = "Audio/SFX/arrow_hit_target";
        public const string UiClickResourcePath = "Audio/SFX/ui_click";
        public const string TargetActionOrMissResourcePath = "Audio/SFX/target_action_or_miss";

        private static GameAudioManager instance;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip bowDrawClip;
        [SerializeField] private AudioClip bowReleaseClip;
        [SerializeField] private AudioClip arrowHitTargetClip;
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip targetActionOrMissClip;

        [Header("Volume Settings")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float bowDrawVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float bowReleaseVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float arrowHitVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float uiClickVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float targetActionVolume = 0.9f;

        private AudioSource uiAudioSource;
        private AudioSource drawAudioSource;

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

        public AudioClip BowDrawClip => bowDrawClip;
        public AudioClip BowReleaseClip => bowReleaseClip;
        public AudioClip ArrowHitTargetClip => arrowHitTargetClip;
        public AudioClip UiClickClip => uiClickClip;
        public AudioClip TargetActionOrMissClip => targetActionOrMissClip;
        public bool IsBowDrawPlaying => drawAudioSource != null && drawAudioSource.isPlaying;

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
            if (bowDrawClip == null)
                bowDrawClip = Resources.Load<AudioClip>(BowDrawResourcePath);
            if (bowReleaseClip == null)
                bowReleaseClip = Resources.Load<AudioClip>(BowReleaseResourcePath);
            if (arrowHitTargetClip == null)
                arrowHitTargetClip = Resources.Load<AudioClip>(ArrowHitTargetResourcePath);
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
        /// Plays the target hit sound at the arrow impact position.
        /// Detached from the target GameObject so target destruction does not cut off playback.
        /// </summary>
        public void PlayArrowHitTarget(Vector3 position)
        {
            Initialize();
            if (arrowHitTargetClip == null) return;

            PlaySpatialOneShot(arrowHitTargetClip, position, arrowHitVolume * masterVolume, 1f);
        }

        /// <summary>
        /// Plays the surface/environment impact sound (or miss sound) at the given position.
        /// </summary>
        public void PlaySurfaceHit(Vector3 position)
        {
            Initialize();
            if (targetActionOrMissClip == null) return;

            PlaySpatialOneShot(targetActionOrMissClip, position, targetActionVolume * masterVolume, 1f);
        }

        /// <summary>
        /// Plays the target placement or removal sound at the target's position.
        /// </summary>
        public void PlayTargetAction(Vector3 position)
        {
            Initialize();
            if (targetActionOrMissClip == null) return;

            PlaySpatialOneShot(targetActionOrMissClip, position, targetActionVolume * masterVolume, 1f);
        }

        /// <summary>
        /// Plays the 2D UI click / mode switch sound.
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
