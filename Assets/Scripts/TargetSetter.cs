using ArcheryGame.Audio;
using UnityEngine;

public class TargetSetter : MonoBehaviour
{
    [Header("Referências")]
    public Transform rightController;
    public GameObject targetPrefab;
    public GameManager gameManager;

    [Header("Configurações")]
    public float defaultPlacementDistance = 2.5f;

    [Header("Cores do Laser")]
    public Color normalLaserColor = Color.cyan;
    public Color targetHoverColor = Color.red;

    private LineRenderer laser;
    private float lastActionTime = float.NegativeInfinity;
    private const float ActionCooldown = 0.2f;
    private const float PointerRange = 10f;

    public Ray ControllerRay
    {
        get
        {
            Transform origin = rightController != null ? rightController : transform;
            return new Ray(origin.position, origin.forward);
        }
    }

    private void Awake()
    {
        laser = GetComponent<LineRenderer>();
        if (laser != null)
        {
            laser.positionCount = 2;
            laser.useWorldSpace = true;
        }
    }

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Update()
    {
        if (gameManager == null || gameManager.Phase != RoundPhase.PlacingTargets)
        {
            HidePointer();
            return;
        }

        if (OVRInput.GetDown(OVRInput.RawButton.A) || Input.GetKeyDown(KeyCode.Tab))
        {
            gameManager.StartShooting();
            if (gameManager.Phase == RoundPhase.Shooting)
            {
                HidePointer();
                return;
            }
        }

        Ray ray = ControllerRay;
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, 15f);
        GameObject aimedTarget = hasHit && gameManager.IsTarget(hit.collider.gameObject)
            ? hit.collider.gameObject
            : null;

        ShowPointer(ray, hasHit ? hit.point : ray.GetPoint(PointerRange),
            aimedTarget != null ? targetHoverColor : normalLaserColor);

        bool place = OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger)
                     || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        bool remove = OVRInput.GetDown(OVRInput.RawButton.B)
                      || OVRInput.GetDown(OVRInput.RawButton.RHandTrigger)
                      || Input.GetKeyDown(KeyCode.Delete);

        if ((!place && !remove) || Time.time - lastActionTime < ActionCooldown)
            return;

        lastActionTime = Time.time;
        if (aimedTarget != null)
        {
            gameManager.RemoveTarget(aimedTarget);
            return;
        }

        if (!place || remove || targetPrefab == null || !gameManager.CanPlaceTarget())
            return;

        Vector3 position = hasHit ? hit.point : ray.GetPoint(defaultPlacementDistance);
        Vector3 normal = hasHit ? hit.normal : -ray.direction;
        GameObject target = Instantiate(targetPrefab, position, Quaternion.FromToRotation(Vector3.up, normal));
        if (gameManager.RegisterNewTarget(target))
            GameAudioManager.Instance?.PlayTargetAction(position);
        else
            Destroy(target);
    }

    public void ShowPointer(Ray ray, Vector3 end, Color color)
    {
        if (laser == null) return;

        laser.enabled = true;
        laser.startColor = color;
        laser.endColor = color;
        laser.SetPosition(0, ray.origin);
        laser.SetPosition(1, end);
    }

    public void HidePointer()
    {
        if (laser != null) laser.enabled = false;
    }

    public void BeginPlacement()
    {
        lastActionTime = Time.time;
        HidePointer();
    }
}
