using UnityEngine;

namespace ArcheryGame.Arrow
{
    /// <summary>
    /// Spawns and launches arrows from the VR controller.
    /// Uses OVRInput for Meta Quest trigger detection.
    /// 
    /// Attach this to a GameObject in the scene (e.g., the right controller anchor).
    /// Assign the arrowPrefab and shootOrigin in the Inspector.
    /// </summary>
    public class ArrowShooter : MonoBehaviour
    {
        [Header("Arrow Settings")]
        [Tooltip("The arrow prefab to instantiate. Must have ArrowController and Rigidbody.")]
        [SerializeField] private GameObject arrowPrefab;

        [Tooltip("The transform from which arrows are spawned (position and forward direction).")]
        [SerializeField] private Transform shootOrigin;

        [Tooltip("Game manager that receives shot statistics.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Optional target setter used to share the placement/shooting mode.")]
        [SerializeField] private TargetSetter targetSetter;

        [Tooltip("Force multiplier applied to the arrow on launch (m/s).")]
        [SerializeField] private float shootForce = 20f;

        [Header("Cooldown")]
        [Tooltip("Minimum time in seconds between shots.")]
        [SerializeField] private float shootCooldown = 0.5f;

        [Header("State")]
        [Tooltip("Set to false to prevent shooting (e.g., during target placement mode).")]
        public bool canShoot = true;

        private float lastShotTime = -Mathf.Infinity;

        private void Update()
        {
            if (!canShoot) return;
            if (targetSetter != null && targetSetter.interactionMode != InteractionMode.ShootArrow) return;
            if (Time.time - lastShotTime < shootCooldown) return;

            if (GetShootInput())
            {
                ShootArrow();
            }
        }

        private void Start()
        {
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();

            if (targetSetter == null)
                targetSetter = FindFirstObjectByType<TargetSetter>();
        }

        /// <summary>
        /// Checks for shoot input from VR controllers and desktop fallback.
        /// Uses Meta Quest right-hand index trigger as primary input.
        /// </summary>
        private bool GetShootInput()
        {
            // Meta Quest: Right index trigger
            bool vrTrigger = false;
            try
            {
                vrTrigger = OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger)
                         || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);
            }
            catch
            {
                // OVRInput may not be available in simulator/editor without Meta XR plugin
            }

            // Desktop fallback: Left mouse click or Space key
            bool desktopInput = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);

            return vrTrigger || desktopInput;
        }

        /// <summary>
        /// Instantiates an arrow at the shoot origin and launches it forward.
        /// </summary>
        private void ShootArrow()
        {
            if (arrowPrefab == null)
            {
                Debug.LogWarning("[ArrowShooter] Arrow prefab is not assigned!");
                return;
            }

            Transform origin = shootOrigin != null ? shootOrigin : transform;

            // Instantiate the arrow at the controller position/rotation
            GameObject arrowObj = Instantiate(arrowPrefab, origin.position, origin.rotation);

            // Launch it forward
            ArrowController arrow = arrowObj.GetComponent<ArrowController>();
            if (arrow != null)
            {
                arrow.Launch(origin.forward * shootForce);
                gameManager?.RegisterShot();
            }
            else
            {
                Debug.LogError("[ArrowShooter] Arrow prefab is missing ArrowController component!");
                // Still apply velocity via Rigidbody as fallback
                Rigidbody rb = arrowObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.linearVelocity = origin.forward * shootForce;
                }
            }

            lastShotTime = Time.time;
            Debug.Log($"[ArrowShooter] Arrow launched with force {shootForce} m/s");
        }
    }
}
