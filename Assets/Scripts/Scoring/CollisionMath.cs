using UnityEngine;

namespace ArcheryGame.Scoring
{
    /// <summary>
    /// Pure math utilities for arrow-target collision calculations.
    /// All methods are static and have no side effects, making them fully unit-testable.
    /// </summary>
    public static class CollisionMath
    {
        /// <summary>
        /// Calculates the Euclidean distance between two points in 3D space.
        /// </summary>
        /// <param name="center">The center point (e.g., target bullseye).</param>
        /// <param name="point">The point to measure from (e.g., arrow hit position).</param>
        /// <returns>The distance in meters.</returns>
        public static float DistanceToCenter(Vector3 center, Vector3 point)
        {
            return Vector3.Distance(center, point);
        }

        /// <summary>
        /// Projects a 3D hit point onto the target's local plane and returns the
        /// 2D distance from the target center. This ensures accurate scoring even
        /// if the arrow hits at an angle and the contact point is slightly off-plane.
        /// </summary>
        /// <param name="point">The 3D hit point from the collision.</param>
        /// <param name="targetCenter">The world-space center of the target (bullseye).</param>
        /// <param name="targetNormal">The target's forward/normal direction (perpendicular to face).</param>
        /// <returns>The 2D distance from the center on the target plane, in meters.</returns>
        public static float PlanarDistanceToCenter(Vector3 point, Vector3 targetCenter, Vector3 targetNormal)
        {
            Vector3 toPoint = point - targetCenter;

            // Remove the component along the target normal (depth)
            Vector3 projectedOffset = toPoint - Vector3.Dot(toPoint, targetNormal) * targetNormal;

            return projectedOffset.magnitude;
        }

        /// <summary>
        /// Projects a 3D point onto a plane defined by a point and normal.
        /// </summary>
        /// <param name="point">The point to project.</param>
        /// <param name="planePoint">A point on the plane.</param>
        /// <param name="planeNormal">The normal of the plane (must be normalized).</param>
        /// <returns>The projected point on the plane.</returns>
        public static Vector3 ProjectOntoPlane(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
        {
            Vector3 toPoint = point - planePoint;
            float distanceAlongNormal = Vector3.Dot(toPoint, planeNormal);
            return point - distanceAlongNormal * planeNormal;
        }

        /// <summary>
        /// Checks whether an arrow's velocity is sufficient to count as a valid hit.
        /// Prevents slow-moving or stationary arrows from scoring.
        /// </summary>
        /// <param name="arrowVelocity">The arrow's velocity vector at the moment of impact.</param>
        /// <param name="minSpeed">The minimum speed threshold in m/s.</param>
        /// <returns>True if the arrow speed meets or exceeds the minimum threshold.</returns>
        public static bool IsValidHit(Vector3 arrowVelocity, float minSpeed)
        {
            return arrowVelocity.sqrMagnitude >= minSpeed * minSpeed;
        }
    }
}
