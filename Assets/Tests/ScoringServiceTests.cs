using NUnit.Framework;
using UnityEngine;
using ArcheryGame.Scoring;

namespace ArcheryGame.Tests
{
    /// <summary>
    /// Unit tests for the ScoringService class.
    /// These tests run in Edit Mode without requiring Play Mode or the full Unity runtime,
    /// making them suitable for CI/CD pipelines.
    /// </summary>
    [TestFixture]
    public class ScoringServiceTests
    {
        private ScoringService _service;
        private TargetRingConfig[] _standardRings;

        [SetUp]
        public void SetUp()
        {
            _service = new ScoringService();
            _standardRings = DefaultTargetConfig.StandardRings;
        }

        // =====================================================================
        // CalculateScore — Simple 3D distance scoring
        // =====================================================================

        [Test]
        public void CalculateScore_ExactBullseye_Returns10Points()
        {
            Vector3 center = Vector3.zero;
            Vector3 hit = Vector3.zero;

            int score = _service.CalculateScore(center, hit, _standardRings);

            Assert.AreEqual(10, score, "A direct hit on the center should score 10 points.");
        }

        [Test]
        public void CalculateScore_NearBullseye_Returns10Points()
        {
            Vector3 center = Vector3.zero;
            Vector3 hit = new Vector3(0.01f, 0f, 0f); // 1cm from center, within 2cm bullseye

            int score = _service.CalculateScore(center, hit, _standardRings);

            Assert.AreEqual(10, score, "A hit 1cm from center should be in bullseye (radius 2cm).");
        }

        [Test]
        public void CalculateScore_InnerRing_Returns8Points()
        {
            Vector3 center = Vector3.zero;
            Vector3 hit = new Vector3(0.03f, 0f, 0f); // 3cm from center

            int score = _service.CalculateScore(center, hit, _standardRings);

            Assert.AreEqual(8, score, "A hit 3cm from center should be in the inner ring (2cm-5cm).");
        }

        [Test]
        public void CalculateScore_MiddleRing_Returns5Points()
        {
            Vector3 center = Vector3.zero;
            Vector3 hit = new Vector3(0.08f, 0f, 0f); // 8cm from center

            int score = _service.CalculateScore(center, hit, _standardRings);

            Assert.AreEqual(5, score, "A hit 8cm from center should be in the middle ring (5cm-10cm).");
        }

        [Test]
        public void CalculateScore_OuterRing_Returns2Points()
        {
            Vector3 center = Vector3.zero;
            Vector3 hit = new Vector3(0.15f, 0f, 0f); // 15cm from center

            int score = _service.CalculateScore(center, hit, _standardRings);

            Assert.AreEqual(2, score, "A hit 15cm from center should be in the outer ring (10cm-18cm).");
        }

        [Test]
        public void CalculateScore_EdgeRing_Returns1Point()
        {
            Vector3 center = Vector3.zero;
            Vector3 hit = new Vector3(0.22f, 0f, 0f); // 22cm from center

            int score = _service.CalculateScore(center, hit, _standardRings);

            Assert.AreEqual(1, score, "A hit 22cm from center should be in the edge ring (18cm-25cm).");
        }

        [Test]
        public void CalculateScore_Miss_Returns0Points()
        {
            Vector3 center = Vector3.zero;
            Vector3 hit = new Vector3(1f, 0f, 0f); // 1m from center — way outside

            int score = _service.CalculateScore(center, hit, _standardRings);

            Assert.AreEqual(0, score, "A hit 1m from center should be a miss (outside all rings).");
        }

        [Test]
        public void CalculateScore_ExactBoundary_ReturnsHigherRingScore()
        {
            Vector3 center = Vector3.zero;
            // Exactly at the bullseye boundary (0.02m)
            Vector3 hit = new Vector3(0.02f, 0f, 0f);

            int score = _service.CalculateScore(center, hit, _standardRings);

            Assert.AreEqual(10, score, "A hit exactly on a ring boundary should score the higher ring's points.");
        }

        [Test]
        public void CalculateScore_EmptyRings_Returns0()
        {
            Vector3 center = Vector3.zero;
            Vector3 hit = Vector3.zero;
            TargetRingConfig[] emptyRings = new TargetRingConfig[0];

            int score = _service.CalculateScore(center, hit, emptyRings);

            Assert.AreEqual(0, score, "Empty ring configuration should always return 0.");
        }

        [Test]
        public void CalculateScore_NullRings_Returns0()
        {
            Vector3 center = Vector3.zero;
            Vector3 hit = Vector3.zero;

            int score = _service.CalculateScore(center, hit, null);

            Assert.AreEqual(0, score, "Null ring configuration should always return 0.");
        }

        [Test]
        public void CalculateScore_OffsetTargetCenter_CorrectScore()
        {
            // Target center is not at the origin
            Vector3 center = new Vector3(5f, 3f, -2f);
            Vector3 hit = center + new Vector3(0.03f, 0f, 0f); // 3cm from offset center

            int score = _service.CalculateScore(center, hit, _standardRings);

            Assert.AreEqual(8, score, "Scoring should work correctly regardless of target world position.");
        }

        [Test]
        public void CalculateScore_NegativeCoordinates_CorrectScore()
        {
            Vector3 center = new Vector3(-10f, -5f, -20f);
            Vector3 hit = center + new Vector3(-0.08f, 0f, 0f); // 8cm from center, negative dir

            int score = _service.CalculateScore(center, hit, _standardRings);

            Assert.AreEqual(5, score, "Scoring should work correctly with negative coordinates.");
        }

        [Test]
        public void CalculateScore_DiagonalHit_CorrectScore()
        {
            Vector3 center = Vector3.zero;
            // ~7.07cm from center (diagonal in XY plane), within middle ring
            Vector3 hit = new Vector3(0.05f, 0.05f, 0f);

            int score = _service.CalculateScore(center, hit, _standardRings);

            Assert.AreEqual(5, score, "Diagonal hits should calculate distance correctly (~7cm → middle ring).");
        }

        // =====================================================================
        // CalculateScorePlanar — Planar projection scoring
        // =====================================================================

        [Test]
        public void CalculateScorePlanar_HitOnPlane_SameAsSimple()
        {
            Vector3 center = Vector3.zero;
            Vector3 normal = Vector3.forward; // Target faces Z+
            Vector3 hit = new Vector3(0.03f, 0f, 0f); // On the plane (z=0)

            int score = _service.CalculateScorePlanar(center, normal, hit, _standardRings);

            Assert.AreEqual(8, score, "Planar scoring of a hit on the plane should match simple scoring.");
        }

        [Test]
        public void CalculateScorePlanar_HitOffPlane_ProjectsCorrectly()
        {
            Vector3 center = Vector3.zero;
            Vector3 normal = Vector3.forward; // Target faces Z+
            // Hit is 3cm along X but also 5cm along Z (off the plane)
            // After projection onto XY plane, only X component matters → 3cm → inner ring
            Vector3 hit = new Vector3(0.03f, 0f, 0.05f);

            int score = _service.CalculateScorePlanar(center, normal, hit, _standardRings);

            Assert.AreEqual(8, score, "Planar scoring should project off-plane hit onto target face.");
        }

        [Test]
        public void CalculateScorePlanar_AngledTarget_CorrectScore()
        {
            // Target is rotated: normal points up (Y+)
            Vector3 center = new Vector3(0f, 2f, 0f);
            Vector3 normal = Vector3.up;
            // Hit is at Y=2.1 (slightly above center plane), X=0.03
            // Planar distance: only X and Z matter → 3cm → inner ring
            Vector3 hit = new Vector3(0.03f, 2.1f, 0f);

            int score = _service.CalculateScorePlanar(center, normal, hit, _standardRings);

            Assert.AreEqual(8, score, "Scoring should work with non-standard target orientations.");
        }
    }
}
