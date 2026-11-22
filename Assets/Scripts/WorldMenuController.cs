using ArcheryGame.Audio;
using TMPro;
using UnityEngine;

/// <summary>Builds a physical, head-locked menu that can be selected by the controller ray.</summary>
public class WorldMenuController : MonoBehaviour
{
    [Header("Posição na visão")]
    [Tooltip("Posição local à câmera, em metros. O menu acompanha a cabeça do jogador.")]
    public Vector3 viewOffset = new Vector3(0.68f, -0.02f, 1.55f);

    private GameManager gameManager;
    private Transform headTransform;
    private GameObject menuRoot;
    private WorldMenuButton easyButton;
    private WorldMenuButton mediumButton;
    private WorldMenuButton hardButton;

    private static readonly Color PanelColor = new Color(0.035f, 0.055f, 0.08f);
    private static readonly Color ButtonColor = new Color(0.12f, 0.23f, 0.31f);
    private static readonly Color ClearColor = new Color(0.43f, 0.14f, 0.13f);

    private void Start()
    {
        gameManager = GetComponent<GameManager>();
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager != null)
            gameManager.OnDifficultyChanged += RefreshSelection;

        TryCreateMenu();
    }

    private void LateUpdate()
    {
        // The XR camera can be enabled after this component's Start method.
        if (menuRoot == null)
            TryCreateMenu();
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnDifficultyChanged -= RefreshSelection;

        if (menuRoot != null)
            Destroy(menuRoot);
    }

    private void TryCreateMenu()
    {
        if (headTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                headTransform = mainCamera.transform;
        }

        if (headTransform == null || gameManager == null)
            return;

        menuRoot = new GameObject("WorldGameMenu");
        menuRoot.transform.SetParent(headTransform, false);
        menuRoot.transform.localPosition = viewOffset;
        menuRoot.transform.localRotation = Quaternion.identity;

        CreateMenuSurface();
        CreateLabel("MENU", new Vector3(0f, 0.41f, -0.035f), new Vector2(0.62f, 0.09f), 0.42f);
        CreateLabel("ALVOS EM MOVIMENTO", new Vector3(0f, 0.29f, -0.035f), new Vector2(0.62f, 0.07f), 0.27f);

        easyButton = CreateButton("FÁCIL  |  0", WorldMenuAction.Easy,
            new Vector3(0f, 0.10f, -0.05f), new Vector3(0.62f, 0.115f, 0.035f), ButtonColor, 0.4f);
        mediumButton = CreateButton("MÉDIO  |  1", WorldMenuAction.Medium,
            new Vector3(0f, -0.08f, -0.05f), new Vector3(0.62f, 0.115f, 0.035f), ButtonColor, 0.4f);
        hardButton = CreateButton("DIFÍCIL  |  3", WorldMenuAction.Hard,
            new Vector3(0f, -0.26f, -0.05f), new Vector3(0.62f, 0.115f, 0.035f), ButtonColor, 0.34f);
        CreateButton("APAGAR TUDO", WorldMenuAction.ClearTargets,
            new Vector3(0f, -0.44f, -0.05f), new Vector3(0.62f, 0.115f, 0.035f), ClearColor, 0.34f);

        RefreshSelection(gameManager.CurrentDifficulty);
    }

    private void CreateMenuSurface()
    {
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surface.name = "WorldGameMenuSurface";
        surface.transform.SetParent(menuRoot.transform, false);
        surface.transform.localPosition = Vector3.zero;
        surface.transform.localRotation = Quaternion.identity;
        surface.transform.localScale = new Vector3(0.7f, 1.0f, 0.04f);

        WorldMenuButton surfaceButton = surface.AddComponent<WorldMenuButton>();
        surfaceButton.Initialize(this, WorldMenuAction.None, PanelColor, false, null, Vector2.zero, 0f);
        surface.GetComponent<Renderer>().enabled = false;
    }

    private WorldMenuButton CreateButton(string label, WorldMenuAction action, Vector3 position,
        Vector3 size, Color color, float fontSize)
    {
        GameObject buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonObject.name = action + "Button";
        buttonObject.transform.SetParent(menuRoot.transform, false);
        buttonObject.transform.localPosition = position;
        buttonObject.transform.localRotation = Quaternion.identity;
        buttonObject.transform.localScale = size;

        WorldMenuButton button = buttonObject.AddComponent<WorldMenuButton>();
        button.Initialize(this, action, color, false, label,
            new Vector2(size.x - 0.035f, size.y * 0.9f), fontSize);
        return button;
    }

    private void CreateLabel(string value, Vector3 position, Vector2 size, float fontSize)
    {
        GameObject labelObject = new GameObject(value, typeof(TextMeshPro));
        labelObject.transform.SetParent(menuRoot.transform, false);
        labelObject.transform.localPosition = position;
        labelObject.transform.localRotation = Quaternion.identity;

        TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
        label.text = value;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = Color.white;
        label.outlineWidth = 0.2f;
        label.outlineColor = new Color32(0, 0, 0, 220);
        label.rectTransform.sizeDelta = size;
    }

    public void Activate(WorldMenuAction action)
    {
        if (gameManager == null)
            return;

        GameAudioManager.Instance?.PlayUiClick();

        switch (action)
        {
            case WorldMenuAction.Easy:
                gameManager.SetDifficultyEasy();
                break;
            case WorldMenuAction.Medium:
                gameManager.SetDifficultyMedium();
                break;
            case WorldMenuAction.Hard:
                gameManager.SetDifficultyHard();
                break;
            case WorldMenuAction.ClearTargets:
                gameManager.DeleteAllTargets();
                break;
        }
    }

    private void RefreshSelection(TargetDifficulty difficulty)
    {
        if (easyButton == null)
            return;

        easyButton.SetSelected(difficulty == TargetDifficulty.Easy);
        mediumButton.SetSelected(difficulty == TargetDifficulty.Medium);
        hardButton.SetSelected(difficulty == TargetDifficulty.Hard);
    }
}
