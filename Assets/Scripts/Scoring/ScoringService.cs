using UnityEngine;

namespace ArcheryGame.Scoring
{
    /// <summary>
    /// Pure C# scoring service with no MonoBehaviour dependency.
    /// Calculates the score for an arrow hit based on distance from the target center.
    /// Fully unit-testable without running the Unity game.
    /// </summary>
    public class ScoringService
    {
        /// <summary>
        /// Calculates the score for an arrow hit using simple 3D distance.
        /// Best for targets that are always facing the player head-on.
        /// </summary>
        /// <param name="targetCenter">World position of the target's bullseye center.</param>
        /// <param name="hitPoint">World position where the arrow made contact.</param>
        /// <param name="rings">Array of ring configs sorted by radius ascending (smallest first).</param>
        /// <returns>The score for the hit, or 0 if outside all rings (miss).</returns>
        public int CalculateScore(Vector3 targetCenter, Vector3 hitPoint, TargetRingConfig[] rings)
        {
            if (rings == null || rings.Length == 0)
                return 0;

            float distance = CollisionMath.DistanceToCenter(targetCenter, hitPoint);

            for (int i = 0; i < rings.Length; i++)
            {
                if (distance <= rings[i].radius)
                    return rings[i].points;
            }

            return 0;
        }

        /// <summary>
        /// Calculates the score for an arrow hit using planar projection.
        /// More accurate for targets hit at an angle, as it projects the hit point
        /// onto the target's face plane before measuring distance.
        /// </summary>
        /// <param name="targetCenter">World position of the target's bullseye center.</param>
        /// <param name="targetNormal">The target's forward normal (perpendicular to face).</param>
        /// <param name="hitPoint">World position where the arrow made contact.</param>
        /// <param name="rings">Array of ring configs sorted by radius ascending (smallest first).</param>
        /// <returns>The score for the hit, or 0 if outside all rings (miss).</returns>
        public int CalculateScorePlanar(Vector3 targetCenter, Vector3 targetNormal, Vector3 hitPoint, TargetRingConfig[] rings)
        {
            if (rings == null || rings.Length == 0)
                return 0;

            float distance = CollisionMath.PlanarDistanceToCenter(hitPoint, targetCenter, targetNormal.normalized);

            for (int i = 0; i < rings.Length; i++)
            {
                if (distance <= rings[i].radius)
                    return rings[i].points;
            }

            return 0;
        }
    }
}
