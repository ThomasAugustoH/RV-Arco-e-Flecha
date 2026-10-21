using UnityEngine;
using ArcheryGame.Bow;
using ArcheryGame.Arrow;

public enum InteractionMode
{
    PlaceTarget,
    ShootArrow
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
    public Color shootModeLaserColor = Color.yellow;

    private LineRenderer laser;
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

        // Garante que exista um sistema de arco/flecha ativo na cena
        if (FindFirstObjectByType<BowController>() == null && FindFirstObjectByType<ArrowShooter>() == null)
        {
            gameObject.AddComponent<BowController>();
            Debug.Log("[TargetSetter] BowController adicionado automaticamente para arqueria.");
        }

        if (laser != null)
        {
            laser.startColor = normalLaserColor;
            laser.endColor = normalLaserColor;
        }
    }

    void Update()
    {
        if (laser == null) return;

        // Ponto de origem do laser (no controle ou neste objeto)
        Vector3 rayOrigin = rightController != null ? rightController.position : transform.position;
        Vector3 rayDirection = rightController != null ? rightController.forward : transform.forward;

        laser.SetPosition(0, rayOrigin);

        RaycastHit hit;
        bool acertouAlgo = Physics.Raycast(rayOrigin, rayDirection, out hit, 15f);

        GameObject targetAtingido = null;

        if (acertouAlgo)
        {
            laser.SetPosition(1, hit.point);

            // Verifica se o objeto atingido é um alvo existente
            if (gameManager != null && gameManager.IsTarget(hit.collider.gameObject))
            {
                targetAtingido = hit.collider.gameObject;
            }
            else if (hit.collider.GetComponentInParent<TargetMovement>() != null)
            {
                targetAtingido = hit.collider.GetComponentInParent<TargetMovement>().gameObject;
            }
            else if (hit.collider.name.Contains("Target") || hit.collider.CompareTag("Target"))
            {
                targetAtingido = hit.collider.gameObject;
            }
        }
        else
        {
            // Laser vai no vazio
            laser.SetPosition(1, rayOrigin + (rayDirection * 10f));
        }

        // Feedback visual do Laser
        if (interactionMode == InteractionMode.ShootArrow)
        {
            // No modo tiro, o laser fica Amarelo
            laser.startColor = shootModeLaserColor;
            laser.endColor = shootModeLaserColor;
        }
        else if (targetAtingido != null)
        {
            // Mirando em um alvo existente: Laser fica Vermelho (clicar vai remover)
            laser.startColor = targetHoverColor;
            laser.endColor = targetHoverColor;
        }
        else
        {
            // Mirando no ambiente ou ar: Laser Ciano normal (clicar vai criar alvo)
            laser.startColor = normalLaserColor;
            laser.endColor = normalLaserColor;
        }

        // Alternar modo com teclas 1 e 2 no PC
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetInteractionMode(InteractionMode.PlaceTarget);
            return;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetInteractionMode(InteractionMode.ShootArrow);
            return;
        }

        // Alternar modo: Botão A no VR, ou teclas F, M no PC (ou Tab como fallback)
        bool alternouModo = OVRInput.GetDown(OVRInput.RawButton.A)
                         || OVRInput.GetDown(OVRInput.Button.One)
                         || Input.GetKeyDown(KeyCode.F)
                         || Input.GetKeyDown(KeyCode.M)
                         || Input.GetKeyDown(KeyCode.Tab);

        if (alternouModo)
        {
            ToggleMode();
            return;
        }

        // Detecção de Input para colocar/remover alvos (somente no modo PlaceTarget)
        bool clicou = OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger)
                   || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)
                   || Input.GetMouseButtonDown(0)
                   || Input.GetKeyDown(KeyCode.Space);

        // Permite também o botão B, Grip ou tecla Delete para deletar
        bool clicouDeletar = OVRInput.GetDown(OVRInput.RawButton.B)
                          || OVRInput.GetDown(OVRInput.RawButton.RHandTrigger)
                          || Input.GetKeyDown(KeyCode.Delete);

        if (clicou || clicouDeletar)
        {
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
                    Debug.Log("[TargetSetter] Alvo criado com sucesso!");
                }
                else
                {
                    Debug.LogError("[TargetSetter] 'targetPrefab' está nulo no Inspector!");
                }
            }
        }
    }

    /// <summary>
    /// Alterna entre modo de posicionar alvos e atirar com arco e flecha.
    /// </summary>
    public void ToggleMode()
    {
        SetInteractionMode(interactionMode == InteractionMode.PlaceTarget
            ? InteractionMode.ShootArrow
            : InteractionMode.PlaceTarget);
    }

    /// <summary>
    /// Define diretamente o modo de interação.
    /// </summary>
    public void SetInteractionMode(InteractionMode mode)
    {
        interactionMode = mode;
        string info = mode == InteractionMode.ShootArrow 
            ? "🏹 MODO ARCO E FLECHA (Laser Amarelo | Segure T / Clique para puxar a corda, solte para atirar)" 
            : "🎯 MODO ALVOS (Laser Ciano/Vermelho | Clique para criar/remover alvos)";
        Debug.Log($"[TargetSetter] {info}");
    }

    private void OnGUI()
    {
        // Botão visual no canto superior da tela para alternar de modo facilmente com 1 clique de mouse
        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 14;
        btnStyle.fontStyle = FontStyle.Bold;
        btnStyle.alignment = TextAnchor.MiddleCenter;

        Color originalColor = GUI.backgroundColor;
        bool isShoot = interactionMode == InteractionMode.ShootArrow;
        GUI.backgroundColor = isShoot ? new Color(1f, 0.85f, 0.15f) : new Color(0.2f, 0.85f, 1f);

        string label = isShoot
            ? "🏹 MODO: ARCO E FLECHA (Laser Amarelo)\n[Clique aqui para MODO ALVOS]"
            : "🎯 MODO: CRIAR ALVOS (Laser Ciano)\n[Clique aqui para MODO ARCO]";

        if (GUI.Button(new Rect(15, 15, 340, 52), label, btnStyle))
        {
            ToggleMode();
        }

        GUI.backgroundColor = originalColor;
    }
}
