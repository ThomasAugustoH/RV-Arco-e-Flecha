using UnityEngine;

namespace ArcheryGame.Arrow
{
    public enum ArrowState
    {
        Nocked,
        Flying,
        Stuck
    }

    /// <summary>
    /// Controls arrow behavior: physics-based flight, rotation toward velocity,
    /// collision handling, and sticking to targets/surfaces.
    /// 
    /// Required components: Rigidbody, Collider (CapsuleCollider recommended).
    /// The arrow model should face the +Z axis (forward).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ArrowController : MonoBehaviour
    {
        [Header("Flight Settings")]
        [Tooltip("Minimum speed (m/s) for the arrow to count as a valid hit.")]
        [SerializeField] private float minHitSpeed = 2f;

        [Tooltip("Time in seconds before the arrow auto-destroys if it hasn't hit anything.")]
        [SerializeField] private float maxLifetime = 10f;

        [Header("Visual Settings")]
        [Tooltip("Comprimento visível da flecha, em metros.")]
        [SerializeField] private float arrowLength = 0.75f;

        [Tooltip("Raio visual da haste, em metros.")]
        [SerializeField] private float shaftRadius = 0.01f;

        [Header("State (Read Only)")]
        [SerializeField] private bool isFlying = false;
        [SerializeField] private bool isStuck = false;
        [SerializeField] private ArrowState state = ArrowState.Nocked;

        private Rigidbody rb;
        private float spawnTime;

        /// <summary>Whether this arrow is currently in flight.</summary>
        public bool IsFlying => isFlying;

        /// <summary>Whether this arrow is stuck in a target or surface.</summary>
        public bool IsStuck => isStuck;
        public ArrowState State => state;

        /// <summary>The arrow's current velocity. Zero if stuck.</summary>
        public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;

        /// <summary>Minimum speed required for a valid scoring hit.</summary>
        public float MinHitSpeed => minHitSpeed;

        private void Awake()
        {
            ConfigureArrowShape();
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true; // Start kinematic until launched
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth VR visuals
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Prevent tunneling through thin targets
            SetColliderEnabled(false);
            spawnTime = Time.time;
        }

        private void ConfigureArrowShape()
        {
            // The original prefab is a uniformly scaled Unity cylinder. Keep the
            // physics root facing +Z and put the cylinder mesh on a rotated child,
            // so aiming and flight rotation use the same forward axis.
            transform.localScale = Vector3.one;

            MeshFilter sourceFilter = GetComponent<MeshFilter>();
            MeshRenderer sourceRenderer = GetComponent<MeshRenderer>();
            if (sourceFilter != null && sourceRenderer != null)
            {
                GameObject shaft = new GameObject("ArrowShaftVisual");
                shaft.transform.SetParent(transform, false);
                shaft.transform.localPosition = new Vector3(0f, 0f, arrowLength * 0.5f);
                shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                shaft.transform.localScale = new Vector3(
                    shaftRadius,
                    arrowLength * 0.5f,
                    shaftRadius);

                MeshFilter shaftFilter = shaft.AddComponent<MeshFilter>();
                shaftFilter.sharedMesh = sourceFilter.sharedMesh;
                MeshRenderer shaftRenderer = shaft.AddComponent<MeshRenderer>();
                shaftRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
                shaftRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                shaftRenderer.receiveShadows = sourceRenderer.receiveShadows;
                sourceRenderer.enabled = false;
            }

            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.direction = 2; // Z axis
                capsule.center = new Vector3(0f, 0f, arrowLength * 0.5f);
                capsule.height = arrowLength;
                capsule.radius = shaftRadius * 1.5f;
            }
        }

        public void Nock(Transform nockingPoint)
        {
            if (nockingPoint == null || isFlying || isStuck) return;

            state = ArrowState.Nocked;
            isFlying = false;
            rb.isKinematic = true;
            rb.useGravity = false;
            SetColliderEnabled(false);
            UpdateNockedPosition(nockingPoint.position, nockingPoint.rotation);
            spawnTime = Time.time;
        }

        public void UpdateNockedPosition(Vector3 position, Quaternion rotation)
        {
            if (state != ArrowState.Nocked) return;
            rb.position = position;
            rb.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
        }

        public void CancelNock()
        {
            if (state != ArrowState.Nocked) return;
            Destroy(gameObject);
        }

        /// <summary>
        /// Launches the arrow with the given velocity. Enables physics and gravity.
        /// </summary>
        /// <param name="velocity">Initial velocity vector (direction * speed).</param>
        public void Launch(Vector3 velocity)
        {
            if (isFlying || isStuck) return;

            isFlying = true;
            state = ArrowState.Flying;
            rb.isKinematic = false;
            rb.useGravity = true;
            SetColliderEnabled(true);
            rb.linearVelocity = velocity;

            spawnTime = Time.time;
        }

        private void FixedUpdate()
        {
            if (!isFlying || isStuck) return;

            // Rotate arrow to face its velocity direction (realistic arc trajectory)
            if (rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(rb.linearVelocity);
                rb.MoveRotation(targetRotation);
            }
        }

        private void Update()
        {
            // Auto-destroy after timeout
            if (!isStuck && Time.time - spawnTime > maxLifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isStuck) return;

            // Attempt to notify the target's collision handler
            var targetHandler = collision.gameObject.GetComponentInParent<Scoring.TargetCollisionHandler>();

            if (targetHandler != null)
            {
                // Hit a target — let the handler process scoring
                Vector3 contactPoint = collision.GetContact(0).point;
                targetHandler.ProcessArrowHit(this, contactPoint);
            }

            // Stick to whatever we hit (target or environment)
            StickToSurface(collision.transform);
        }

        /// <summary>
        /// Freezes the arrow in place and parents it to the given surface transform.
        /// Called when the arrow impacts a target or any other surface.
        /// </summary>
        /// <param name="surface">The transform of the surface to stick to.</param>
        public void StickToSurface(Transform surface)
        {
            if (isStuck) return;

            isStuck = true;
            isFlying = false;
            state = ArrowState.Stuck;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Disable collider to prevent further collision events
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Parent to the surface so the arrow moves with moving targets
            transform.SetParent(surface);
        }

        private void SetColliderEnabled(bool enabled)
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = enabled;
        }
    }
}
