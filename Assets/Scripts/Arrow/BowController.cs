using System.Collections;
using UnityEngine;
using ArcheryGame.Arrow;

namespace ArcheryGame.Bow
{
    /// <summary>
    /// Implements VR Archery mechanics:
    /// - Two-handed physical bow pulling in Meta Quest VR.
    /// - Simulator/Desktop support: hold trigger (Key T / Left Click / Space) to charge tension, release to shoot.
    /// - Dynamic 3-point bowstring rendering via LineRenderer.
    /// - Proportional arrow velocity based on draw tension.
    /// - Integrated with GameManager scoring and TargetSetter modes.
    /// </summary>
    public class BowController : MonoBehaviour
    {
        [Header("Prefabs & Visuals")]
        [Tooltip("The arrow prefab instantiated on release (must have ArrowController).")]
        [SerializeField] private GameObject arrowPrefab;

        [Tooltip("Visual arrow shown resting on the bow while pulling (hidden when idle).")]
        [SerializeField] private GameObject nockedArrowVisual;

        [Tooltip("LineRenderer used to render the dynamic bowstring.")]
        [SerializeField] private LineRenderer stringLineRenderer;

        [Header("Bow String Anchors (Local or World)")]
        [Tooltip("Top tip of the bow where the string attaches.")]
        [SerializeField] private Transform topTip;

        [Tooltip("Bottom tip of the bow where the string attaches.")]
        [SerializeField] private Transform bottomTip;

        [Tooltip("Rest position of the string (at the bow grip/nock point).")]
        [SerializeField] private Transform stringRestTransform;

        [Header("Hands & VR Anchors")]
        [Tooltip("Hand holding the bow (default: Left Controller or this object).")]
        [SerializeField] private Transform bowHand;

        [Tooltip("Hand pulling the string (default: Right Controller).")]
        [SerializeField] private Transform pullHand;

        [Header("Tension & Physics")]
        [Tooltip("Maximum draw distance in meters (typical real bow is 0.5 - 0.7m).")]
        [SerializeField] private float maxDrawDistance = 0.55f;

        [Tooltip("Minimum draw distance required to release an arrow (prevents accidental taps).")]
        [SerializeField] private float minDrawDistance = 0.08f;

        [Tooltip("Arrow launch speed at minimum draw (m/s).")]
        [SerializeField] private float minLaunchSpeed = 8f;

        [Tooltip("Arrow launch speed at full 100% draw (m/s).")]
        [SerializeField] private float maxLaunchSpeed = 35f;

        [Tooltip("Seconds required to reach 100% draw tension in Simulator / Charge mode.")]
        [SerializeField] private float simulatorChargeDuration = 0.85f;

        [Header("References")]
        [SerializeField] private TargetSetter targetSetter;
        [SerializeField] private GameManager gameManager;

        // Current internal state
        private bool isDrawing = false;
        private float currentDrawDistance = 0f;
        private float currentPullRatio = 0f;
        private Vector3 stringPullPosition;
        private Vector3 stringRestPositionLocal = new Vector3(0f, 0f, -0.1f);
        private Vector3 topTipLocal = new Vector3(0f, 0.6f, 0f);
        private Vector3 bottomTipLocal = new Vector3(0f, -0.6f, 0f);

        public bool IsDrawing => isDrawing;
        public float PullRatio => currentPullRatio;

        private void Awake()
        {
            SetupComponentsIfNeeded();
        }

        private void Start()
        {
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();

            if (targetSetter == null)
                targetSetter = FindFirstObjectByType<TargetSetter>();

            AutoFindHands();
            ResetStringPositions();
        }

        private void Update()
        {
            // Only allow archery when in ShootArrow mode (or if no TargetSetter present)
            if (targetSetter != null && targetSetter.interactionMode != InteractionMode.ShootArrow)
            {
                if (isDrawing) CancelDraw();
                UpdateStringVisuals();
                return;
            }

            HandleInput();
            UpdateStringVisuals();
        }

        /// <summary>
        /// Handles both Two-Handed VR drawing and Simulator/Desktop charge drawing.
        /// </summary>
        private void HandleInput()
        {
            bool inputHeld = GetDrawInputHeld();
            bool inputDown = GetDrawInputDown();
            bool inputUp = GetDrawInputUp();

            // Check if two-handed physical pulling is active in VR
            bool hasActivePullHand = pullHand != null && pullHand.gameObject.activeInHierarchy;

            if (inputDown)
            {
                StartDrawing();
            }

            if (isDrawing)
            {
                if (inputHeld)
                {
                    // 1. Two-Handed VR: calculate draw from physical distance between hands
                    if (hasActivePullHand && !IsSimulatorChargeOnly())
                    {
                        Vector3 restWorld = GetRestPointWorld();
                        Vector3 pullWorld = pullHand.position;
                        // Project pull hand onto bow's backward axis
                        Vector3 bowBackward = -transform.forward;
                        float dist = Vector3.Dot(pullWorld - restWorld, bowBackward);
                        currentDrawDistance = Mathf.Clamp(dist, 0f, maxDrawDistance);
                    }
                    else
                    {
                        // 2. Simulator / Desktop: charge tension smoothly over time while holding trigger/click
                        currentDrawDistance += (maxDrawDistance / simulatorChargeDuration) * Time.deltaTime;
                        currentDrawDistance = Mathf.Clamp(currentDrawDistance, 0f, maxDrawDistance);
                    }

                    currentPullRatio = Mathf.Clamp01(currentDrawDistance / maxDrawDistance);

                    // VR Haptic pulse while drawing (Meta Quest)
                    SendHapticPulse(currentPullRatio * 0.4f);
                }

                if (inputUp)
                {
                    // Released the trigger / mouse button
                    if (currentDrawDistance >= minDrawDistance)
                    {
                        ReleaseArrow();
                    }
                    else
                    {
                        CancelDraw();
                    }
                }
            }
        }

        private bool GetDrawInputDown()
        {
            bool vrTrigger = false;
            try
            {
                vrTrigger = OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger)
                         || OVRInput.GetDown(OVRInput.RawButton.RHandTrigger)
                         || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);
            }
            catch { }

            bool desktop = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.T);
            return vrTrigger || desktop;
        }

        private bool GetDrawInputHeld()
        {
            bool vrTrigger = false;
            try
            {
                vrTrigger = OVRInput.Get(OVRInput.RawButton.RIndexTrigger)
                         || OVRInput.Get(OVRInput.RawButton.RHandTrigger)
                         || OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger);
            }
            catch { }

            bool desktop = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.T);
            return vrTrigger || desktop;
        }

        private bool GetDrawInputUp()
        {
            bool vrTrigger = false;
            try
            {
                vrTrigger = OVRInput.GetUp(OVRInput.RawButton.RIndexTrigger)
                         || OVRInput.GetUp(OVRInput.RawButton.RHandTrigger)
                         || OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger);
            }
            catch { }

            bool desktop = Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.T);
            return vrTrigger || desktop;
        }

        private bool IsSimulatorChargeOnly()
        {
            // If running without Meta XR active controllers moving in 3D, use charge mode
            if (pullHand == null) return true;
            // Check if pull hand is stationary at origin
            if (pullHand.localPosition.sqrMagnitude < 0.001f) return true;
            return false;
        }

        private void StartDrawing()
        {
            isDrawing = true;
            currentDrawDistance = 0f;
            currentPullRatio = 0f;

            if (nockedArrowVisual != null)
                nockedArrowVisual.SetActive(true);
        }

        private void ReleaseArrow()
        {
            isDrawing = false;
            float launchSpeed = Mathf.Lerp(minLaunchSpeed, maxLaunchSpeed, currentPullRatio);
            Vector3 shootOrigin = GetRestPointWorld();
            Vector3 shootDirection = transform.forward;

            // Spawn and launch flying arrow
            GameObject arrowObj;
            if (arrowPrefab != null)
            {
                arrowObj = Instantiate(arrowPrefab, shootOrigin, Quaternion.LookRotation(shootDirection));
            }
            else
            {
                arrowObj = CreateDefaultArrow(shootOrigin, Quaternion.LookRotation(shootDirection));
            }

            ArrowController arrowController = arrowObj.GetComponent<ArrowController>();
            if (arrowController != null)
            {
                arrowController.Launch(shootDirection * launchSpeed);
                gameManager?.RegisterShot();
            }
            else
            {
                Rigidbody rb = arrowObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.linearVelocity = shootDirection * launchSpeed;
                    gameManager?.RegisterShot();
                }
            }

            Debug.Log($"[BowController] Flecha disparada! Força: {launchSpeed:F1} m/s (Tensão: {currentPullRatio * 100:F0}%)");

            // Strong release haptic feedback
            SendHapticPulse(1.0f);

            // Hide visual nocked arrow
            if (nockedArrowVisual != null)
                nockedArrowVisual.SetActive(false);

            // Animate string return
            StartCoroutine(SpringBackString());
        }

        private void CancelDraw()
        {
            isDrawing = false;
            currentDrawDistance = 0f;
            currentPullRatio = 0f;

            if (nockedArrowVisual != null)
                nockedArrowVisual.SetActive(false);

            StartCoroutine(SpringBackString());
        }

        private IEnumerator SpringBackString()
        {
            float elapsed = 0f;
            float duration = 0.08f;
            Vector3 startPos = stringPullPosition;
            Vector3 targetPos = GetRestPointWorld();

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                stringPullPosition = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                yield return null;
            }

            ResetStringPositions();
        }

        private void ResetStringPositions()
        {
            stringPullPosition = GetRestPointWorld();
            currentDrawDistance = 0f;
            currentPullRatio = 0f;
        }

        private void UpdateStringVisuals()
        {
            if (stringLineRenderer == null) return;

            Vector3 top = topTip != null ? topTip.position : transform.TransformPoint(topTipLocal);
            Vector3 bot = bottomTip != null ? bottomTip.position : transform.TransformPoint(bottomTipLocal);
            Vector3 rest = GetRestPointWorld();

            if (isDrawing)
            {
                // Pull string backward along bow's -forward vector
                stringPullPosition = rest - (transform.forward * currentDrawDistance);

                // Position the nocked visual arrow
                if (nockedArrowVisual != null)
                {
                    nockedArrowVisual.transform.position = stringPullPosition;
                    nockedArrowVisual.transform.rotation = Quaternion.LookRotation(transform.forward);
                }
            }
            else if (!isDrawing && (stringPullPosition == Vector3.zero || Vector3.Distance(stringPullPosition, rest) > 0.001f))
            {
                stringPullPosition = rest;
            }

            // Update 3-point LineRenderer: Top -> Pull Point -> Bottom
            stringLineRenderer.positionCount = 3;
            stringLineRenderer.SetPosition(0, top);
            stringLineRenderer.SetPosition(1, stringPullPosition);
            stringLineRenderer.SetPosition(2, bot);
        }

        private Vector3 GetRestPointWorld()
        {
            return stringRestTransform != null 
                ? stringRestTransform.position 
                : transform.TransformPoint(stringRestPositionLocal);
        }

        private void SendHapticPulse(float amplitude)
        {
            try
            {
                OVRInput.SetControllerVibration(amplitude * 0.5f, amplitude, OVRInput.Controller.RTouch);
            }
            catch { }
        }

        private void AutoFindHands()
        {
            if (bowHand == null)
            {
                // Try to find LeftControllerAnchor
                GameObject leftAnchor = GameObject.Find("LeftControllerAnchor");
                if (leftAnchor != null) bowHand = leftAnchor.transform;
            }

            if (pullHand == null)
            {
                // Try to find RightControllerAnchor
                GameObject rightAnchor = GameObject.Find("RightControllerAnchor");
                if (rightAnchor != null) pullHand = rightAnchor.transform;
                else if (targetSetter != null && targetSetter.rightController != null) pullHand = targetSetter.rightController;
            }
        }

        private void SetupComponentsIfNeeded()
        {
            // 1. Setup LineRenderer for the bowstring
            if (stringLineRenderer == null)
            {
                stringLineRenderer = GetComponent<LineRenderer>();
                if (stringLineRenderer == null)
                {
                    stringLineRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            if (stringLineRenderer != null)
            {
                stringLineRenderer.useWorldSpace = true;
                stringLineRenderer.startWidth = 0.004f;
                stringLineRenderer.endWidth = 0.004f;
                stringLineRenderer.positionCount = 3;
                stringLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                stringLineRenderer.receiveShadows = false;

                // Simple durable string material (Lit or Unlit)
                if (stringLineRenderer.sharedMaterial == null)
                {
                    Material stringMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
                    stringMat.color = new Color(0.95f, 0.95f, 0.95f, 1f);
                    stringLineRenderer.material = stringMat;
                }
            }

            // 2. Setup Nocked Arrow Visual
            if (nockedArrowVisual == null)
            {
                Transform existingArrow = transform.Find("NockedArrow_Visual");
                if (existingArrow != null)
                {
                    nockedArrowVisual = existingArrow.gameObject;
                }
                else
                {
                    nockedArrowVisual = CreateVisualArrow();
                }
            }

            if (nockedArrowVisual != null)
                nockedArrowVisual.SetActive(false);
        }

        private GameObject CreateVisualArrow()
        {
            GameObject arrow = new GameObject("NockedArrow_Visual");
            arrow.transform.SetParent(transform, false);

            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft";
            shaft.transform.SetParent(arrow.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0f, 0.35f);
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shaft.transform.localScale = new Vector3(0.015f, 0.35f, 0.015f);

            Collider col = shaft.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer rend = shaft.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.82f, 0.52f, 0.25f);
            }

            return arrow;
        }

        private GameObject CreateDefaultArrow(Vector3 position, Quaternion rotation)
        {
            GameObject arrowObj = new GameObject("Arrow");
            arrowObj.transform.position = position;
            arrowObj.transform.rotation = rotation;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Arrow_Mesh";
            visual.transform.SetParent(arrowObj.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0f, 0.35f);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.transform.localScale = new Vector3(0.018f, 0.35f, 0.018f);

            Collider childCol = visual.GetComponent<Collider>();
            if (childCol != null) Destroy(childCol);

            CapsuleCollider capsule = arrowObj.AddComponent<CapsuleCollider>();
            capsule.radius = 0.02f;
            capsule.height = 0.7f;
            capsule.center = new Vector3(0f, 0f, 0.35f);
            capsule.direction = 2; // Z-axis

            Rigidbody rb = arrowObj.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            arrowObj.AddComponent<ArrowController>();

            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.85f, 0.55f, 0.2f);
            }

            return arrowObj;
        }

        private void OnGUI()
        {
            // If in ShootArrow mode, display tension gauge for Simulator/PC testing
            if (targetSetter != null && targetSetter.interactionMode != InteractionMode.ShootArrow) return;

            if (isDrawing)
            {
                GUIStyle style = new GUIStyle(GUI.skin.box);
                style.fontSize = 15;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;

                int pct = Mathf.RoundToInt(currentPullRatio * 100f);
                int bars = Mathf.RoundToInt(currentPullRatio * 15f);
                string barStr = new string('█', bars) + new string('░', 15 - bars);

                GUI.backgroundColor = Color.Lerp(Color.yellow, Color.red, currentPullRatio);
                GUI.Box(new Rect(15, 75, 340, 45), $"🏹 Tensão: [{barStr}] {pct}%\nSolte [T / Clique / Gatilho] para Atirar", style);
            }
        }
    }
}
