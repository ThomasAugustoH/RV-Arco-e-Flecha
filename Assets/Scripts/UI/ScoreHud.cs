using TMPro;
using UnityEngine;

namespace ArcheryGame.UI
{
    /// <summary>
    /// Head-locked score display shown in the top-right corner of the player's view.
    ///
    /// The scene has no screen-space canvas (overlay canvases are not rendered inside the
    /// headset), so the HUD is a world-space TextMeshPro parented to the VR camera
    /// (CenterEyeAnchor) with an offset up and to the right of the view center.
    /// GameManager creates it automatically, so it survives scene reloads without
    /// any scene wiring.
    /// </summary>
    public class ScoreHud : MonoBehaviour
    {
        public const string ScoreLabel = "Pontos";

        [Header("Referências")]
        [Tooltip("Source of the score. Defaults to the GameManager on this object or in the scene.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Head/camera transform the HUD follows. Defaults to Camera.main (CenterEyeAnchor).")]
        [SerializeField] private Transform headTransform;

        [Header("Layout")]
        [Tooltip("Position relative to the camera: +X right, +Y up, +Z forward (meters).")]
        [SerializeField] private Vector3 viewOffset = new Vector3(0.5f, 0.36f, 0.7f);

        [Tooltip("TextMeshPro font size (world-space units; 0.4 ≈ 4cm tall glyphs).")]
        [SerializeField] private float fontSize = 0.4f;

        [SerializeField] private Color textColor = Color.white;

        [Tooltip("Render the HUD on top of scene geometry so the bow never hides it.")]
        [SerializeField] private bool renderOnTop = true;

        private TextMeshPro label;
        private string currentText = FormatScore(0);
        private bool subscribed;

        /// <summary>Text currently shown by the HUD.</summary>
        public string CurrentText => currentText;

        /// <summary>Transform the HUD text is attached to (null until a camera is found).</summary>
        public Transform AttachedHead => label != null ? label.transform.parent : null;

        /// <summary>The HUD text transform (null until a camera is found).</summary>
        public Transform LabelTransform => label != null ? label.transform : null;

        public Vector3 ViewOffset => viewOffset;

        /// <summary>Formats the score exactly as shown to the player.</summary>
        public static string FormatScore(int score) => $"{ScoreLabel}: {score}";

        private void Start()
        {
            if (gameManager == null)
                gameManager = GetComponent<GameManager>();
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();

            Bind(gameManager);
            TryAttachToHead();
        }

        private void LateUpdate()
        {
            // The OVR rig may enable its camera after Start; keep trying until attached.
            if (label == null)
                TryAttachToHead();
        }

        private void OnDestroy()
        {
            Unbind();

            if (label != null)
                Destroy(label.gameObject);
        }

        /// <summary>Starts listening to score changes of <paramref name="manager"/>.</summary>
        public void Bind(GameManager manager)
        {
            Unbind();
            gameManager = manager;

            if (gameManager == null)
                return;

            gameManager.OnScoreChanged += SetScore;
            subscribed = true;
            SetScore(gameManager.TotalScore);
        }

        /// <summary>Parents the HUD text to <paramref name="head"/> at the top-right view offset.</summary>
        public void AttachTo(Transform head)
        {
            if (head == null)
                return;

            headTransform = head;

            if (label == null)
                label = CreateLabel();

            Transform labelTransform = label.transform;
            labelTransform.SetParent(head, false);
            labelTransform.localPosition = viewOffset;
            labelTransform.localRotation = Quaternion.identity;
            labelTransform.localScale = Vector3.one;
            label.text = currentText;
        }

        /// <summary>Updates the displayed score.</summary>
        public void SetScore(int score)
        {
            currentText = FormatScore(score);

            if (label != null)
                label.text = currentText;
        }

        private void Unbind()
        {
            if (subscribed && gameManager != null)
                gameManager.OnScoreChanged -= SetScore;

            subscribed = false;
        }

        private void TryAttachToHead()
        {
            if (headTransform == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                    headTransform = mainCamera.transform;
            }

            if (headTransform != null)
                AttachTo(headTransform);
        }

        private TextMeshPro CreateLabel()
        {
            var labelObject = new GameObject("ScoreHudText");
            TextMeshPro text = labelObject.AddComponent<TextMeshPro>();

            text.fontSize = fontSize;
            text.color = textColor;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.TopRight;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color32(0, 0, 0, 200);

            // Pivot at the top-right corner so the text grows left/down from the offset point.
            RectTransform rect = text.rectTransform;
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(0.5f, 0.1f);

            if (renderOnTop)
                ApplyOverlayShader(text);

            return text;
        }

        private static void ApplyOverlayShader(TextMeshPro text)
        {
            Shader overlay = Shader.Find("TextMeshPro/Distance Field Overlay");
            if (overlay == null || text.fontSharedMaterial == null)
                return;

            // Instance material so the shared font material is left untouched.
            var material = new Material(text.fontSharedMaterial) { shader = overlay };
            text.fontMaterial = material;
        }
    }
}
