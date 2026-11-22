using UnityEngine;
using ArcheryGame.Audio;

public enum InteractionMode
{
    PlaceTarget,
    ShootArrow,
    Archery
}

public class TargetSetter : MonoBehaviour
{
    [Header("Referências")]
    public Transform rightController;
    public GameObject targetPrefab;
    public GameManager gameManager;

    [Header("Configurações")]
    public float defaultPlacementDistance = 2.5f; // Distância padrão quando mira no ar/vazio
    public bool canPlaceTargets = true;
    public InteractionMode interactionMode = InteractionMode.PlaceTarget;

    [Header("Cores do Laser")]
    public Color normalLaserColor = Color.cyan;
    public Color targetHoverColor = Color.red;
    public Color menuHoverLaserColor = Color.green;

    private LineRenderer laser;
    private WorldMenuButton hoveredMenuButton;
    private WorldMenuController worldMenuController;
    private float lastActionTime = 0f;
    private const float actionCooldown = 0.2f;

    void Awake()
    {
        canPlaceTargets = true;
    }

    void Start()
    {
        laser = GetComponent<LineRenderer>();
        canPlaceTargets = true; // Inicia garantidamente no modo de edição de alvos

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        worldMenuController = FindFirstObjectByType<WorldMenuController>();
        if (worldMenuController != null)
            worldMenuController.SetVisible(interactionMode == InteractionMode.PlaceTarget);

        if (laser != null)
        {
            laser.startColor = normalLaserColor;
            laser.endColor = normalLaserColor;
        }
    }

    void Update()
    {
        bool alternouModo = OVRInput.GetDown(OVRInput.RawButton.A)
                         || OVRInput.GetDown(OVRInput.Button.One)
                         || Input.GetKeyDown(KeyCode.Tab);

        if (alternouModo)
        {
            interactionMode = interactionMode == InteractionMode.PlaceTarget
                ? InteractionMode.Archery
                : InteractionMode.PlaceTarget;
            Debug.Log("[TargetSetter] Modo alterado para: " + interactionMode);
        }

        bool creationMode = interactionMode == InteractionMode.PlaceTarget;
        if (worldMenuController != null)
            worldMenuController.SetVisible(creationMode);

        if (laser != null)
            laser.enabled = creationMode;

        // Ponto de origem do laser (no controle ou neste objeto)
        Vector3 rayOrigin = rightController != null ? rightController.position : transform.position;
        Vector3 rayDirection = rightController != null ? rightController.forward : transform.forward;

        if (laser != null)
            laser.SetPosition(0, rayOrigin);

        RaycastHit hit;
        bool acertouAlgo = Physics.Raycast(rayOrigin, rayDirection, out hit, 15f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

        GameObject targetAtingido = null;
        WorldMenuButton menuButtonAtingido = null;

        if (acertouAlgo)
        {
            if (laser != null)
                laser.SetPosition(1, hit.point);

            // Botões físicos têm prioridade sobre alvos e superfícies do cenário.
            menuButtonAtingido = hit.collider.GetComponentInParent<WorldMenuButton>();

            // Verifica se o objeto atingido é um alvo existente
            if (menuButtonAtingido == null && gameManager != null && gameManager.IsTarget(hit.collider.gameObject))
            {
                targetAtingido = hit.collider.gameObject;
            }
            else if (menuButtonAtingido == null && hit.collider.GetComponentInParent<TargetMovement>() != null)
            {
                targetAtingido = hit.collider.GetComponentInParent<TargetMovement>().gameObject;
            }
            else if (menuButtonAtingido == null && (hit.collider.name.Contains("Target") || hit.collider.CompareTag("Target")))
            {
                targetAtingido = hit.collider.gameObject;
            }
        }
        else
        {
            // Laser vai no vazio
            if (laser != null)
                laser.SetPosition(1, rayOrigin + (rayDirection * 10f));
        }

        if (hoveredMenuButton != menuButtonAtingido)
        {
            if (hoveredMenuButton != null)
                hoveredMenuButton.SetHovered(false);

            hoveredMenuButton = menuButtonAtingido;
            if (hoveredMenuButton != null)
                hoveredMenuButton.SetHovered(true);
        }

        // Feedback visual do Laser
        if (laser != null && menuButtonAtingido != null)
        {
            laser.startColor = menuHoverLaserColor;
            laser.endColor = menuHoverLaserColor;
        }
        else if (laser != null && targetAtingido != null)
        {
            // Mirando em um alvo existente: Laser fica Vermelho (clicar vai remover)
            laser.startColor = targetHoverColor;
            laser.endColor = targetHoverColor;
        }
        else if (laser != null)
        {
            // Mirando no ambiente ou ar: Laser Ciano normal (clicar vai criar alvo)
            laser.startColor = normalLaserColor;
            laser.endColor = normalLaserColor;
        }

        // Detecção de Input: Gatilho no Quest, Botão A, clique do mouse no PC ou Barra de Espaço
        bool clicou = OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger)
                   || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)
                   || OVRInput.GetDown(OVRInput.RawButton.A)
                   || OVRInput.GetDown(OVRInput.Button.One)
                   || Input.GetMouseButtonDown(0)
                   || Input.GetKeyDown(KeyCode.Space);

        // Permite também o botão B, Grip ou tecla Delete para deletar
        bool clicouDeletar = OVRInput.GetDown(OVRInput.RawButton.B)
                          || OVRInput.GetDown(OVRInput.RawButton.RHandTrigger)
                          || Input.GetKeyDown(KeyCode.Delete);

        if (clicou || clicouDeletar)
        {
            // O fundo também é tratado como botão sem ação, evitando que um clique
            // sobre a placa crie/remova um alvo atrás do menu.
            if (menuButtonAtingido != null)
            {
                if (Time.time - lastActionTime < actionCooldown) return;
                lastActionTime = Time.time;
                menuButtonAtingido.Activate();
                return;
            }

            if (!canPlaceTargets) return;
            if (interactionMode != InteractionMode.PlaceTarget) return;
            if (Time.time - lastActionTime < actionCooldown) return;
            lastActionTime = Time.time;

            if (targetAtingido != null)
            {
                // SE ESTÁ MIRANDO EM UM ALVO EXISTENTE -> REMOVE!
                Debug.Log("[TargetSetter] Alvo atingido pelo laser! Removendo: " + targetAtingido.name);
                if (gameManager != null)
                {
                    gameManager.RemoveTarget(targetAtingido);
                }
                else
                {
                    Destroy(targetAtingido);
                }
            }
            else if (!clicouDeletar)
            {
                // SE NÃO ESTÁ MIRANDO EM ALVO -> CRIA NOVO ALVO!
                if (gameManager != null && !gameManager.CanPlaceTarget())
                {
                    Debug.LogWarning("[TargetSetter] Limite máximo de alvos (" + gameManager.maxTargets + ") atingido! Remova um alvo antes de criar outro.");
                    return;
                }

                Vector3 spawnPos;
                Quaternion spawnRot;

                if (acertouAlgo)
                {
                    // Se atingiu uma superfície física com colisor (parede/chão)
                    spawnPos = hit.point;
                    spawnRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    Debug.Log("[TargetSetter] Criando alvo na superfície física em: " + spawnPos);
                }
                else
                {
                    // Se clicou no ar/vazio (Passthrough sem malha física ou Simulator)
                    spawnPos = rayOrigin + (rayDirection * defaultPlacementDistance);
                    spawnRot = Quaternion.FromToRotation(Vector3.up, -rayDirection);
                    Debug.Log("[TargetSetter] Criando alvo no espaço a " + defaultPlacementDistance + "m em: " + spawnPos);
                }

                if (targetPrefab != null)
                {
                    GameObject newTarget = Instantiate(targetPrefab, spawnPos, spawnRot);
                    if (gameManager != null)
                    {
                        gameManager.RegisterNewTarget(newTarget);
                    }
                    GameAudioManager.Instance?.PlayTargetAction(spawnPos);
                    Debug.Log("[TargetSetter] Alvo criado com sucesso!");
                }
                else
                {
                    Debug.LogError("[TargetSetter] 'targetPrefab' está nulo no Inspector!");
                }
            }
        }
    }
}
