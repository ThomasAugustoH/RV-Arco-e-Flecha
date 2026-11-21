using NUnit.Framework;
using ArcheryGame.Scoring;

namespace ArcheryGame.Tests
{
    /// <summary>
    /// Unit tests for the default target band layout: equal-width bands where every
    /// band closer to the center is worth double the previous one.
    /// </summary>
    [TestFixture]
    public class DefaultTargetConfigTests
    {
        private const float Epsilon = 0.0001f;

        [Test]
        public void StandardRings_HasFiveBandsCoveringTheTarget()
        {
            TargetRingConfig[] rings = DefaultTargetConfig.StandardRings;

            Assert.AreEqual(DefaultTargetConfig.BandCount, rings.Length);
            Assert.AreEqual(DefaultTargetConfig.TargetRadius, rings[rings.Length - 1].radius, Epsilon,
                "The outermost band must end at the target edge.");
        }

        [Test]
        public void StandardRings_PointsFromCenterToEdge_Are16_8_4_2_1()
        {
            TargetRingConfig[] rings = DefaultTargetConfig.StandardRings;

            CollectionAssert.AreEqual(new[] { 16, 8, 4, 2, 1 }, System.Array.ConvertAll(rings, r => r.points));
        }

        [Test]
        public void StandardRings_BandsHaveEqualWidth()
        {
            TargetRingConfig[] rings = DefaultTargetConfig.StandardRings;
            float expectedWidth = DefaultTargetConfig.TargetRadius / DefaultTargetConfig.BandCount;

            float previousRadius = 0f;
            foreach (TargetRingConfig ring in rings)
            {
                Assert.AreEqual(expectedWidth, ring.radius - previousRadius, Epsilon);
                previousRadius = ring.radius;
            }
        }

        [Test]
        public void CreateDoublingRings_EachInnerBandDoublesOuterBand()
        {
            TargetRingConfig[] rings = DefaultTargetConfig.CreateDoublingRings(1f, 4, 3);

            CollectionAssert.AreEqual(new[] { 24, 12, 6, 3 }, System.Array.ConvertAll(rings, r => r.points));
            Assert.AreEqual(0.25f, rings[0].radius, Epsilon);
            Assert.AreEqual(1f, rings[3].radius, Epsilon);
        }

        [Test]
        public void CreateDoublingRings_SingleBand_IsWorthEdgePoints()
        {
            TargetRingConfig[] rings = DefaultTargetConfig.CreateDoublingRings(0.3f, 1, 5);

            Assert.AreEqual(1, rings.Length);
            Assert.AreEqual(5, rings[0].points);
            Assert.AreEqual(0.3f, rings[0].radius, Epsilon);
        }

        [TestCase(0f, 5, 1)]
        [TestCase(-1f, 5, 1)]
        [TestCase(0.25f, 0, 1)]
        [TestCase(0.25f, 5, 0)]
        [TestCase(0.25f, 32, 1)]
        [TestCase(0.25f, 5, int.MaxValue)]
        public void CreateDoublingRings_InvalidOrOverflowingInput_ReturnsEmpty(float radius, int bands, int edgePoints)
        {
            TargetRingConfig[] rings = DefaultTargetConfig.CreateDoublingRings(radius, bands, edgePoints);

            Assert.IsNotNull(rings);
            Assert.AreEqual(0, rings.Length);
        }
    }
}
