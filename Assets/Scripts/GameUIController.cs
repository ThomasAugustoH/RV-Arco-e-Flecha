using System.Collections;
using ArcheryGame.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    public GameManager gameManager;
    public TargetSetter targetSetter;

    [SerializeField, Min(1f)] private float resultSeconds = 4f;

    private static readonly Color PanelColor = new Color32(20, 27, 39, 248);
    private static readonly Color ButtonColor = new Color32(36, 59, 76, 255);
    private static readonly Color FocusColor = new Color32(26, 110, 123, 255);

    private Camera view;
    private RectTransform canvas;
    private RectTransform menuCanvas;
    private RectTransform menu;
    private RectTransform result;
    private Button playButton;
    private Button exitButton;
    private TextMeshProUGUI resultTitle;
    private TextMeshProUGUI resultScore;
    private TextMeshProUGUI instructions;

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
        if (targetSetter == null)
            targetSetter = FindFirstObjectByType<TargetSetter>();

        if (gameManager != null)
            gameManager.OnRoundChanged += RefreshInterface;
    }

    private void LateUpdate()
    {
        if (gameManager == null) return;

        if (canvas == null)
        {
            view = Camera.main;
            if (view == null) return;

            CreateInterface();
            RefreshInterface();
        }

        if (gameManager.Phase == RoundPhase.Menu)
        {
            if (!menuCanvas.gameObject.activeSelf)
                OpenMenu();
            UpdateMenuInput();
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnRoundChanged -= RefreshInterface;
        if (canvas != null)
            Destroy(canvas.gameObject);
    }

    private void CreateInterface()
    {
        canvas = CreateCanvas("Interface da partida", view.transform);
        canvas.localPosition = new Vector3(0f, 0f, 1.5f);

        menuCanvas = CreateCanvas("Menu principal", transform);
        menu = CreatePanel("Menu", menuCanvas);
        CreateLabel(menu, "RV Arco e Flecha", new Vector2(0f, 95f), new Vector2(480f, 58f), 44f);
        playButton = CreateButton(menu, "Jogar", 12f, StartRound);
        exitButton = CreateButton(menu, "Sair", -76f, ExitGame);
        menuCanvas.gameObject.SetActive(false);

        result = CreatePanel("Resultado", canvas);
        resultTitle = CreateLabel(result, "", new Vector2(0f, 57f), new Vector2(490f, 74f), 54f);
        resultScore = CreateLabel(result, "", new Vector2(0f, -17f), new Vector2(480f, 52f), 34f);
        CreateLabel(result, "Voltando ao menu…", new Vector2(0f, -88f), new Vector2(480f, 40f), 26f);
        result.gameObject.SetActive(false);

        instructions = CreateLabel(canvas, "", new Vector2(0f, -245f), new Vector2(650f, 48f), 25f);
        instructions.outlineWidth = 0.22f;
        instructions.outlineColor = Color.black;
    }

    private RectTransform CreateCanvas(string name, Transform parent)
    {
        Canvas worldCanvas = new GameObject(name, typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = view;
        worldCanvas.sortingOrder = 20;
        RectTransform rect = worldCanvas.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one * 0.0016f;
        return rect;
    }

    private void OpenMenu()
    {
        Transform head = view.transform;
        Vector3 menuOffset = new Vector3(0f, -0.25f, 1.5f);
        menuCanvas.SetPositionAndRotation(head.position + head.rotation * menuOffset, head.rotation);
        menuCanvas.gameObject.SetActive(true);
    }

    private static RectTransform CreatePanel(string name, RectTransform parent)
    {
        Image panel = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        panel.color = PanelColor;
        SetLayout(panel.rectTransform, parent, Vector2.zero, new Vector2(520f, 300f));
        return panel.rectTransform;
    }

    private static TextMeshProUGUI CreateLabel(
        Transform parent, string text, Vector2 position, Vector2 size, float fontSize)
    {
        TextMeshProUGUI label = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        SetLayout(label.rectTransform, parent, position, size);
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(RectTransform parent, string text, float y, UnityAction action)
    {
        GameObject buttonObject = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
        Image background = buttonObject.GetComponent<Image>();
        SetLayout(background.rectTransform, parent, new Vector2(0f, y), new Vector2(330f, 66f));
        background.color = ButtonColor;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(action);
        CreateLabel(background.rectTransform, text, Vector2.zero, background.rectTransform.sizeDelta, 38f);
        return button;
    }

    private static void SetLayout(RectTransform rect, Transform parent, Vector2 position, Vector2 size)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void UpdateMenuInput()
    {
        Button hovered = null;
        if (targetSetter != null)
        {
            Ray ray = targetSetter.ControllerRay;
            hovered = FindButton(ray, out Vector3 end);
            targetSetter.ShowPointer(ray, end, hovered != null ? Color.white : Color.cyan);
        }

        bool click = OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger);
        if (!Application.isMobilePlatform && Input.GetMouseButtonDown(0))
        {
            hovered = FindButton(view.ScreenPointToRay(Input.mousePosition), out _);
            click = true;
        }

        playButton.image.color = hovered == playButton ? FocusColor : ButtonColor;
        exitButton.image.color = hovered == exitButton ? FocusColor : ButtonColor;
        if (click && hovered != null)
            hovered.onClick.Invoke();
    }

    private Button FindButton(Ray ray, out Vector3 end)
    {
        Plane plane = new Plane(menu.forward, menu.position);
        if (!plane.Raycast(ray, out float distance) || distance > 8f)
        {
            end = ray.GetPoint(3f);
            return null;
        }

        end = ray.GetPoint(distance);
        if (Contains(playButton, end)) return playButton;
        if (Contains(exitButton, end)) return exitButton;
        return null;
    }

    private static bool Contains(Button button, Vector3 worldPoint)
    {
        RectTransform rect = (RectTransform)button.transform;
        return rect.rect.Contains((Vector2)rect.InverseTransformPoint(worldPoint));
    }

    private void RefreshInterface()
    {
        if (canvas == null) return;

        RoundPhase phase = gameManager.Phase;
        if (phase != RoundPhase.Menu)
            menuCanvas.gameObject.SetActive(false);
        if (phase == RoundPhase.Result && !result.gameObject.activeSelf)
        {
            int score = gameManager.TotalScore;
            resultTitle.text = score >= GameManager.WinningScore ? "Você ganhou!" : "Você perdeu!";
            resultScore.text = $"Pontos: {score} / {GameManager.WinningScore}";
            StartCoroutine(ReturnToMenuAfterResult());
        }
        result.gameObject.SetActive(phase == RoundPhase.Result);
        instructions.gameObject.SetActive(phase == RoundPhase.PlacingTargets || phase == RoundPhase.Shooting);

        if (phase == RoundPhase.PlacingTargets)
            instructions.text = $"Alvos: {gameManager.TargetCount}/{GameManager.TargetLimit}   |   A: iniciar os tiros";
        else if (phase == RoundPhase.Shooting)
            instructions.text = $"Flechas: {gameManager.ArrowsRemaining}/{GameManager.ArrowLimit}   |   Meta: {GameManager.WinningScore} pontos";
    }

    private void StartRound()
    {
        GameAudioManager.Instance?.PlayUiClick();
        targetSetter?.BeginPlacement();
        gameManager.StartRound();
    }

    private static void ExitGame()
    {
        GameAudioManager.Instance?.PlayUiClick();
        Application.Quit();
    }

    private IEnumerator ReturnToMenuAfterResult()
    {
        yield return new WaitForSecondsRealtime(resultSeconds);
        gameManager.ReturnToMenu();
    }
}
