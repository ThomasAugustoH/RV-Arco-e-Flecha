using ArcheryGame.Audio;
using TMPro;
using UnityEngine;

/// <summary>Builds a physical, head-locked menu that can be selected by the controller ray.</summary>
public class WorldMenuController : MonoBehaviour
{
    [Header("Posição na visão")]
    [Tooltip("Posição local à câmera, em metros. O menu acompanha a cabeça do jogador.")]
    public Vector3 viewOffset = new Vector3(0.68f, -0.18f, 1.55f);

    private GameManager gameManager;
    private Transform headTransform;
    private GameObject menuRoot;
    private WorldMenuButton difficultyButton;
    private bool menuVisible = true;

    private static readonly Color ButtonColor = new Color(0.12f, 0.23f, 0.31f);
    private static readonly Color ClearColor = new Color(0.43f, 0.14f, 0.13f);

    private void Start()
    {
        gameManager = GetComponent<GameManager>();
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        TargetSetter targetSetter = FindFirstObjectByType<TargetSetter>();
        if (targetSetter != null)
            menuVisible = targetSetter.interactionMode == InteractionMode.PlaceTarget;

        if (gameManager != null)
            gameManager.OnDifficultyChanged += RefreshDifficultyLabel;

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
            gameManager.OnDifficultyChanged -= RefreshDifficultyLabel;

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
        CreateLabel("DIFICULDADE", new Vector3(0f, 0.25f, -0.035f), new Vector2(0.68f, 0.08f), 0.36f);
        difficultyButton = CreateButton(GetDifficultyLabel(gameManager.CurrentDifficulty), WorldMenuAction.CycleDifficulty,
            new Vector3(0f, 0.08f, -0.05f), new Vector3(0.78f, 0.14f, 0.035f), ButtonColor, 0.47f);
        CreateLabel("CLIQUE PARA MUDAR", new Vector3(0f, -0.035f, -0.035f), new Vector2(0.78f, 0.06f), 0.3f);
        CreateButton("APAGAR TUDO", WorldMenuAction.ClearTargets,
            new Vector3(0f, -0.21f, -0.05f), new Vector3(0.78f, 0.14f, 0.035f), ClearColor, 0.42f);

        RefreshDifficultyLabel(gameManager.CurrentDifficulty);
        menuRoot.SetActive(menuVisible);
    }

    private void CreateMenuSurface()
    {
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surface.name = "WorldGameMenuSurface";
        surface.transform.SetParent(menuRoot.transform, false);
        surface.transform.localPosition = Vector3.zero;
        surface.transform.localRotation = Quaternion.identity;
        surface.transform.localScale = new Vector3(0.86f, 0.70f, 0.04f);

        WorldMenuButton surfaceButton = surface.AddComponent<WorldMenuButton>();
        surfaceButton.Initialize(this, WorldMenuAction.None, Color.black, false, null, Vector2.zero, 0f);
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
            case WorldMenuAction.CycleDifficulty:
                gameManager.CycleDifficulty();
                gameManager.DeleteAllTargets();
                break;
            case WorldMenuAction.ClearTargets:
                gameManager.DeleteAllTargets();
                break;
        }
    }

    public void SetVisible(bool visible)
    {
        menuVisible = visible;
        if (menuRoot != null && menuRoot.activeSelf != visible)
            menuRoot.SetActive(visible);
    }

    private void RefreshDifficultyLabel(TargetDifficulty difficulty)
    {
        if (difficultyButton == null)
            return;

        difficultyButton.SetLabel(GetDifficultyLabel(difficulty));
        difficultyButton.SetSelected(true);
    }

    private static string GetDifficultyLabel(TargetDifficulty difficulty)
    {
        switch (difficulty)
        {
            case TargetDifficulty.Medium:
                return "MÉDIO · 1";
            case TargetDifficulty.Hard:
                return "DIFÍCIL · 3";
            default:
                return "FÁCIL · 0";
        }
    }
}
