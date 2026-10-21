using UnityEngine;
using ArcheryGame.Arrow;

namespace ArcheryGame.Archery
{
    public class BowController : MonoBehaviour
    {
        public enum BowState
        {
            Idle,
            Drawing,
            Ready
        }

        [Header("References")]
        [SerializeField] private Transform bowHand;
        [SerializeField] private Transform drawHand;
        [SerializeField] private Transform nockingPoint;
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private BowStringController bowString;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TargetSetter targetSetter;

        [Header("Draw Settings")]
        [SerializeField] private float minDrawDistance = 0.05f;
        [SerializeField] private float maxDrawDistance = 0.45f;
        [SerializeField] private float minLaunchSpeed = 5f;
        [SerializeField] private float maxLaunchSpeed = 25f;
        [SerializeField] private AnimationCurve drawCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Input")]
        [SerializeField] private bool useDesktopInput = true;
        [SerializeField] private bool enabledInArcheryMode = true;
        [SerializeField] private float desktopDrawDistance = 0.3f;
        [SerializeField] private float simulatedFullDrawTime = 1.5f;
        [SerializeField] private float desktopBowMoveSensitivity = 0.0025f;
        [SerializeField] private float vrTriggerThreshold = 0.1f;
        [SerializeField] private bool enableInputDiagnostics = true;
        [SerializeField] private float diagnosticsInterval = 0.25f;

        [Header("State")]
        [SerializeField] private BowState state = BowState.Idle;
        [SerializeField] private float currentDrawDistance;

        private ArrowController currentArrow;
        private bool wasDrawing;
        private float nextDiagnosticsTime;
        private BowState lastLoggedState;
        private bool simulatorFallbackLogged;
        private float drawStartedAt = -1f;

        public BowState State => state;
        public float CurrentDrawDistance => currentDrawDistance;
        public ArrowController CurrentArrow => currentArrow;

        private void Start()
        {
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            if (targetSetter == null) targetSetter = FindFirstObjectByType<TargetSetter>();
            ResolveHandReferences();
            if (nockingPoint == null) nockingPoint = transform;

            if (transform.parent != null &&
                transform.parent.name == "LeftHandAnchor" &&
                transform.localPosition.sqrMagnitude > 1f)
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }

            lastLoggedState = state;
            LogDiagnostic("Initialized. " + DescribeReferences());
        }

        private void Update()
        {
            if (!enabledInArcheryMode ||
                (targetSetter != null && targetSetter.interactionMode != InteractionMode.Archery))
            {
                CancelDraw();
                return;
            }

            bool drawing = GetDrawInput();
            UpdateDesktopBowMovement();
            LogInputDiagnostic(drawing);
            if (drawing)
            {
                if (!wasDrawing) BeginDraw();
                UpdateDraw();
            }
            else if (wasDrawing)
            {
                LogDiagnostic("Input released. Calling ReleaseDraw().");
                ReleaseDraw();
            }

            wasDrawing = drawing;
        }

        private void OnDisable()
        {
            CancelDraw();
        }

        public void BeginDraw()
        {
            if (currentArrow != null)
                return;
            if (arrowPrefab == null)
            {
                Debug.LogWarning("[BowController] Arrow prefab is not assigned.");
                return;
            }
            if (nockingPoint == null)
            {
                Debug.LogError("[BowController] Nocking point is not assigned.");
                return;
            }

            GameObject arrowObject = Instantiate(
                arrowPrefab, nockingPoint.position, nockingPoint.rotation);
            currentArrow = arrowObject.GetComponent<ArrowController>();
            if (currentArrow == null)
            {
                Debug.LogError("[BowController] Arrow prefab is missing ArrowController.");
                Destroy(arrowObject);
                return;
            }

            currentArrow.Nock(nockingPoint);
            drawStartedAt = Time.time;
            state = BowState.Drawing;
            LogDiagnostic("Arrow nocked. State=Drawing.");
        }

        public void UpdateDraw()
        {
            if (currentArrow == null) return;

            Vector3 drawHandPosition = GetDrawHandPosition();
            currentDrawDistance = BowDrawMath.CalculateDrawDistance(
                drawHandPosition, nockingPoint.position, GetBowForward());
            state = currentDrawDistance >= minDrawDistance
                ? BowState.Ready
                : BowState.Drawing;

            Vector3 arrowPosition = Vector3.Lerp(
                nockingPoint.position,
                drawHandPosition,
                Mathf.Clamp01(currentDrawDistance / Mathf.Max(maxDrawDistance, Mathf.Epsilon)));
            currentArrow.UpdateNockedPosition(arrowPosition, nockingPoint.rotation);
            bowString?.SetNockingPosition(arrowPosition);
            LogStateChange();
        }

        public void ReleaseDraw()
        {
            if (currentArrow == null) return;

            if (currentDrawDistance < minDrawDistance)
            {
                LogDiagnostic($"Release cancelled: draw distance {currentDrawDistance:F3} < minimum {minDrawDistance:F3}.");
                CancelDraw();
                return;
            }

            Vector3 velocity = BowDrawMath.CalculateLaunchVelocity(
                GetBowForward(), currentDrawDistance, minDrawDistance,
                maxDrawDistance, minLaunchSpeed, maxLaunchSpeed, drawCurve);
            currentArrow.Launch(velocity);
            gameManager?.RegisterShot();
            LogDiagnostic($"Arrow launched. Distance={currentDrawDistance:F3}, speed={velocity.magnitude:F2}, velocity={velocity}.");
            currentArrow = null;
            currentDrawDistance = 0f;
            drawStartedAt = -1f;
            state = BowState.Idle;
            LogStateChange();
        }

        public void CancelDraw()
        {
            bool hadDraw = currentArrow != null ||
                           state != BowState.Idle ||
                           currentDrawDistance > Mathf.Epsilon;

            if (currentArrow != null)
            {
                currentArrow.CancelNock();
                currentArrow = null;
            }

            currentDrawDistance = 0f;
            drawStartedAt = -1f;
            state = BowState.Idle;
            if (hadDraw)
            {
                LogDiagnostic("Draw cancelled and nocked arrow destroyed.");
                LogStateChange();
            }
            bowString?.SetNockingPosition(nockingPoint != null
                ? nockingPoint.position
                : transform.position);
        }

        private bool GetDrawInput()
        {
            bool desktop = useDesktopInput &&
                (Input.GetMouseButton(0) ||
                 Input.GetMouseButton(1) ||
                 Input.GetKey(KeyCode.Space));

            return desktop || IsVrTriggerPressed();
        }

        private Vector3 GetDrawHandPosition()
        {
            if (IsDesktopDrawInput())
            {
                float drawDistance = GetSimulatedDrawDistance();
                return nockingPoint.position - bowHand.forward * drawDistance;
            }

            if (Application.isEditor)
            {
                float triggerDraw = GetVrTriggerValue();
                Vector3 trackedDrawOffset = drawHand.position - nockingPoint.position;
                float trackedDistance = Vector3.Dot(trackedDrawOffset, -GetBowForward());

                if (triggerDraw > vrTriggerThreshold && trackedDistance < minDrawDistance)
                {
                    float simulatedDistance = GetSimulatedDrawDistance();
                    if (!simulatorFallbackLogged)
                    {
                        LogDiagnostic(
                            $"Simulator fallback draw: trigger={triggerDraw:F2}, " +
                            $"distance={simulatedDistance:F3}.");
                        simulatorFallbackLogged = true;
                    }
                    return nockingPoint.position - GetBowForward() * simulatedDistance;
                }
            }

            simulatorFallbackLogged = false;
            return drawHand.position;
        }

        private bool IsDesktopDrawInput()
        {
            return useDesktopInput &&
                (Input.GetMouseButton(0) ||
                 Input.GetMouseButton(1) ||
                 Input.GetKey(KeyCode.Space));
        }

        private float GetSimulatedDrawDistance()
        {
            float elapsed = drawStartedAt >= 0f
                ? Mathf.Max(0f, Time.time - drawStartedAt)
                : 0f;
            float duration = Mathf.Max(simulatedFullDrawTime, 0.01f);
            float drawFactor = Mathf.Clamp01(elapsed / duration);
            return Mathf.Lerp(minDrawDistance, maxDrawDistance, drawFactor);
        }

        private bool IsVrTriggerPressed()
        {
            try
            {
                float rightTrigger = GetVrTriggerValue(OVRInput.Axis1D.SecondaryIndexTrigger);
                float leftTrigger = GetVrTriggerValue(OVRInput.Axis1D.PrimaryIndexTrigger);
                return rightTrigger >= vrTriggerThreshold ||
                       leftTrigger >= vrTriggerThreshold ||
                       OVRInput.Get(OVRInput.RawButton.RIndexTrigger) ||
                       OVRInput.Get(OVRInput.RawButton.LIndexTrigger);
            }
            catch (System.InvalidOperationException)
            {
                return false;
            }
        }

        private float GetVrTriggerValue()
        {
            return Mathf.Max(
                GetVrTriggerValue(OVRInput.Axis1D.PrimaryIndexTrigger),
                GetVrTriggerValue(OVRInput.Axis1D.SecondaryIndexTrigger));
        }

        private float GetVrTriggerValue(OVRInput.Axis1D axis)
        {
            try
            {
                return OVRInput.Get(axis);
            }
            catch (System.InvalidOperationException)
            {
                return 0f;
            }
        }

        private void LogInputDiagnostic(bool drawing)
        {
            if (!enableInputDiagnostics || Time.unscaledTime < nextDiagnosticsTime)
                return;

            nextDiagnosticsTime = Time.unscaledTime + diagnosticsInterval;
            LogDiagnostic(
                $"Input={drawing}, mouseL={Input.GetMouseButton(0)}, " +
                $"mouseR={Input.GetMouseButton(1)}, space={Input.GetKey(KeyCode.Space)}, " +
                $"triggerL={GetVrTriggerValue(OVRInput.Axis1D.PrimaryIndexTrigger):F2}, " +
                $"triggerR={GetVrTriggerValue(OVRInput.Axis1D.SecondaryIndexTrigger):F2}, " +
                $"wasDrawing={wasDrawing}, state={state}, distance={currentDrawDistance:F3}");
        }

        private void LogStateChange()
        {
            if (state == lastLoggedState)
                return;

            LogDiagnostic($"State changed: {lastLoggedState} -> {state}.");
            lastLoggedState = state;
        }

        private string DescribeReferences()
        {
            return $"bowHand={GetReferenceName(bowHand)}, drawHand={GetReferenceName(drawHand)}, " +
                   $"nockingPoint={GetReferenceName(nockingPoint)}, arrowPrefab={GetReferenceName(arrowPrefab)}";
        }

        private static string GetReferenceName(Object reference)
        {
            return reference == null ? "NULL" : reference.name;
        }

        private void LogDiagnostic(string message)
        {
            if (enableInputDiagnostics)
                Debug.Log("[BowController] " + message, this);
        }

        private Vector3 GetBowForward()
        {
            return transform.forward.sqrMagnitude > Mathf.Epsilon
                ? transform.forward
                : bowHand.forward;
        }

        private void UpdateDesktopBowMovement()
        {
            if (!useDesktopInput || IsVrTriggerPressed() || !Input.GetMouseButton(2))
                return;

            Vector3 movement = new Vector3(
                Input.GetAxis("Mouse X"),
                Input.GetAxis("Mouse Y"),
                0f) * desktopBowMoveSensitivity;
            transform.localPosition += movement;
        }

        private void ResolveHandReferences()
        {
            if (bowHand == null || bowHand == transform)
            {
                Transform leftHand = FindSceneTransform("LeftHandAnchor");
                bowHand = leftHand != null ? leftHand : transform;
            }

            if (drawHand == null || drawHand == transform)
            {
                Transform rightHand = FindSceneTransform("RightHandAnchor");
                drawHand = rightHand != null ? rightHand : transform;
            }

            if (bowHand == transform || drawHand == transform)
            {
                Debug.LogWarning(
                    "[BowController] Hand references are not configured. " +
                    "Assign LeftHandAnchor to Bow Hand and RightHandAnchor to Draw Hand.");
            }
        }

        private static Transform FindSceneTransform(string objectName)
        {
            GameObject hand = GameObject.Find(objectName);
            return hand != null ? hand.transform : null;
        }
    }
}
