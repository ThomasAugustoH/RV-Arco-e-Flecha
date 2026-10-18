using NUnit.Framework;
using UnityEngine;
using ArcheryGame.Scoring;

namespace ArcheryGame.Tests
{
    /// <summary>
    /// Unit tests for the CollisionMath utility class.
    /// These tests run in Edit Mode without requiring Play Mode or the full Unity runtime,
    /// making them suitable for CI/CD pipelines.
    /// </summary>
    [TestFixture]
    public class CollisionMathTests
    {
        private const float Epsilon = 0.0001f;

        // =====================================================================
        // DistanceToCenter
        // =====================================================================

        [Test]
        public void DistanceToCenter_SamePoint_ReturnsZero()
        {
            Vector3 center = new Vector3(1f, 2f, 3f);

            float distance = CollisionMath.DistanceToCenter(center, center);

            Assert.AreEqual(0f, distance, Epsilon, "Distance from a point to itself should be 0.");
        }

        [Test]
        public void DistanceToCenter_KnownDistance_ReturnsCorrectValue()
        {
            Vector3 center = Vector3.zero;
            Vector3 point = new Vector3(3f, 4f, 0f); // 3-4-5 triangle

            float distance = CollisionMath.DistanceToCenter(center, point);

            Assert.AreEqual(5f, distance, Epsilon, "Distance should follow Euclidean formula (3-4-5 triangle = 5).");
        }

        [Test]
        public void DistanceToCenter_NegativeCoords_ReturnsPositiveDistance()
        {
            Vector3 center = new Vector3(-1f, -1f, -1f);
            Vector3 point = new Vector3(-4f, -5f, -1f); // 3-4 offset in XY

            float distance = CollisionMath.DistanceToCenter(center, point);

            Assert.AreEqual(5f, distance, Epsilon, "Distance should be positive regardless of coordinate signs.");
        }

        // =====================================================================
        // PlanarDistanceToCenter
        // =====================================================================

        [Test]
        public void PlanarDistanceToCenter_PointOnPlane_ReturnsSameAsDistance()
        {
            Vector3 center = Vector3.zero;
            Vector3 normal = Vector3.forward;
            Vector3 point = new Vector3(0.1f, 0.05f, 0f); // On the XY plane (z=0)

            float planarDist = CollisionMath.PlanarDistanceToCenter(point, center, normal);
            float directDist = CollisionMath.DistanceToCenter(center, point);

            Assert.AreEqual(directDist, planarDist, Epsilon,
                "For a point already on the plane, planar distance should equal 3D distance.");
        }

        [Test]
        public void PlanarDistanceToCenter_PointOffPlane_IgnoresDepth()
        {
            Vector3 center = Vector3.zero;
            Vector3 normal = Vector3.forward; // Z is depth
            // Point is at (0.1, 0, 0.5) — 0.1m along X, 0.5m depth
            Vector3 point = new Vector3(0.1f, 0f, 0.5f);

            float planarDist = CollisionMath.PlanarDistanceToCenter(point, center, normal);

            Assert.AreEqual(0.1f, planarDist, Epsilon,
                "Planar distance should ignore the depth component (Z along normal).");
        }

        [Test]
        public void PlanarDistanceToCenter_UpNormal_IgnoresYComponent()
        {
            Vector3 center = new Vector3(0f, 5f, 0f);
            Vector3 normal = Vector3.up; // Y is depth for this target
            // Point is at (0.08, 5.3, 0) — Y offset should be ignored
            Vector3 point = new Vector3(0.08f, 5.3f, 0f);

            float planarDist = CollisionMath.PlanarDistanceToCenter(point, center, normal);

            Assert.AreEqual(0.08f, planarDist, Epsilon,
                "For an upward-facing target, Y offset should be ignored in planar distance.");
        }

        // =====================================================================
        // ProjectOntoPlane
        // =====================================================================

        [Test]
        public void ProjectOntoPlane_PointOnPlane_ReturnsSamePoint()
        {
            Vector3 point = new Vector3(1f, 2f, 0f);
            Vector3 planePoint = Vector3.zero;
            Vector3 planeNormal = Vector3.forward;

            Vector3 projected = CollisionMath.ProjectOntoPlane(point, planePoint, planeNormal);

            Assert.AreEqual(point.x, projected.x, Epsilon);
            Assert.AreEqual(point.y, projected.y, Epsilon);
            Assert.AreEqual(point.z, projected.z, Epsilon);
        }

        [Test]
        public void ProjectOntoPlane_PointAbovePlane_ProjectsDown()
        {
            Vector3 point = new Vector3(1f, 2f, 3f);
            Vector3 planePoint = Vector3.zero;
            Vector3 planeNormal = Vector3.forward; // XY plane at z=0

            Vector3 projected = CollisionMath.ProjectOntoPlane(point, planePoint, planeNormal);

            Assert.AreEqual(1f, projected.x, Epsilon);
            Assert.AreEqual(2f, projected.y, Epsilon);
            Assert.AreEqual(0f, projected.z, Epsilon, "Z should be projected to 0 (the plane).");
        }

        // =====================================================================
        // IsValidHit
        // =====================================================================

        [Test]
        public void IsValidHit_AboveMinSpeed_ReturnsTrue()
        {
            Vector3 velocity = new Vector3(0f, 0f, 10f); // 10 m/s
            float minSpeed = 2f;

            bool valid = CollisionMath.IsValidHit(velocity, minSpeed);

            Assert.IsTrue(valid, "Arrow moving at 10 m/s should be a valid hit (min 2 m/s).");
        }

        [Test]
        public void IsValidHit_ExactlyMinSpeed_ReturnsTrue()
        {
            Vector3 velocity = new Vector3(2f, 0f, 0f); // exactly 2 m/s
            float minSpeed = 2f;

            bool valid = CollisionMath.IsValidHit(velocity, minSpeed);

            Assert.IsTrue(valid, "Arrow moving at exactly min speed should be a valid hit.");
        }

        [Test]
        public void IsValidHit_BelowMinSpeed_ReturnsFalse()
        {
            Vector3 velocity = new Vector3(0.5f, 0f, 0f); // 0.5 m/s
            float minSpeed = 2f;

            bool valid = CollisionMath.IsValidHit(velocity, minSpeed);

            Assert.IsFalse(valid, "Arrow moving at 0.5 m/s should NOT be a valid hit (min 2 m/s).");
        }

        [Test]
        public void IsValidHit_ZeroVelocity_ReturnsFalse()
        {
            Vector3 velocity = Vector3.zero;
            float minSpeed = 0.1f;

            bool valid = CollisionMath.IsValidHit(velocity, minSpeed);

            Assert.IsFalse(valid, "A stationary arrow should NOT be a valid hit.");
        }

        [Test]
        public void IsValidHit_DiagonalVelocity_UseMagnitude()
        {
            // velocity = (1,1,1), magnitude = sqrt(3) ≈ 1.732
            Vector3 velocity = new Vector3(1f, 1f, 1f);
            float minSpeed = 1.7f;

            bool valid = CollisionMath.IsValidHit(velocity, minSpeed);

            Assert.IsTrue(valid, "Speed check should use vector magnitude, not individual components.");
        }
    }
}
