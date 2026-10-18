using UnityEngine;
using ArcheryGame.Arrow;

namespace ArcheryGame.Scoring
{
    /// <summary>
    /// Bridge component that sits on the Target prefab.
    /// Receives arrow collision events and delegates scoring to the ScoringService.
    /// 
    /// This thin adapter keeps the MonoBehaviour layer minimal, while all scoring
    /// logic lives in the pure C# ScoringService (unit-testable without Play Mode).
    /// </summary>
    public class TargetCollisionHandler : MonoBehaviour
    {
        [Header("Target Configuration")]
        [Tooltip("The center point of the bullseye. If not set, uses this transform's position.")]
        [SerializeField] private Transform centerTransform;

        [Tooltip("Ring configuration for this target. Must be sorted by radius ascending (smallest first).")]
        [SerializeField] private TargetRingConfig[] rings;

        [Header("Events")]
        [Tooltip("Invoked when an arrow scores on this target. Parameter is the score value.")]
        public System.Action<int, Vector3> OnScored;

        private ScoringService scoringService;

        private void Awake()
        {
            scoringService = new ScoringService();

            // Use default rings if none configured in Inspector
            if (rings == null || rings.Length == 0)
            {
                rings = DefaultTargetConfig.StandardRings;
            }

            // Default center to this transform if not explicitly set
            if (centerTransform == null)
            {
                centerTransform = transform;
            }
        }

        /// <summary>
        /// Called by ArrowController when an arrow collides with this target.
        /// Calculates the score based on hit position and emits the OnScored event.
        /// </summary>
        /// <param name="arrow">The arrow that hit this target.</param>
        /// <param name="hitPoint">The world-space contact point of the collision.</param>
        public void ProcessArrowHit(ArrowController arrow, Vector3 hitPoint)
        {
            if (arrow == null) return;

            // Validate that the arrow was moving fast enough
            // (use velocity from just before impact — arrow may already be stopped)
            // Note: In most cases, OnCollisionEnter fires before velocity is zeroed out
            bool validHit = CollisionMath.IsValidHit(arrow.Velocity, arrow.MinHitSpeed);

            int score;

            if (validHit)
            {
                // Use planar projection for accurate scoring on angled/moving targets
                Vector3 targetNormal = centerTransform.up; // Target disc's normal direction
                score = scoringService.CalculateScorePlanar(
                    centerTransform.position,
                    targetNormal,
                    hitPoint,
                    rings
                );
            }
            else
            {
                score = 0;
                Debug.Log("[TargetCollisionHandler] Arrow too slow — hit not counted.");
            }

            if (score > 0)
            {
                Debug.Log($"[TargetCollisionHandler] HIT! Score: {score} points at position {hitPoint}");
            }
            else
            {
                Debug.Log($"[TargetCollisionHandler] Miss or invalid hit at position {hitPoint}");
            }

            // Notify listeners (GameManager)
            OnScored?.Invoke(score, hitPoint);
        }

        /// <summary>
        /// Returns the ring configuration for this target (useful for debugging/testing).
        /// </summary>
        public TargetRingConfig[] GetRings() => rings;
    }
}
