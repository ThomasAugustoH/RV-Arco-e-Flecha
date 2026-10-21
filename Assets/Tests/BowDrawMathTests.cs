using NUnit.Framework;
using UnityEngine;
using ArcheryGame.Archery;

namespace ArcheryGame.Tests
{
    [TestFixture]
    public class BowDrawMathTests
    {
        private const float Epsilon = 0.0001f;
        private readonly AnimationCurve linear = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Test]
        public void ZeroDraw_ReturnsMinimumFactor()
        {
            Assert.AreEqual(0f, BowDrawMath.NormalizeDraw(0f, 0.05f, 0.45f), Epsilon);
        }

        [Test]
        public void DrawBelowMinimum_IsClamped()
        {
            Assert.AreEqual(0f, BowDrawMath.NormalizeDraw(-1f, 0.05f, 0.45f), Epsilon);
        }

        [Test]
        public void DrawAboveMaximum_IsClamped()
        {
            Assert.AreEqual(1f, BowDrawMath.NormalizeDraw(2f, 0.05f, 0.45f), Epsilon);
        }

        [Test]
        public void MaximumDraw_ReturnsMaximumFactor()
        {
            Assert.AreEqual(1f, BowDrawMath.CalculateForceFactor(0.45f, 0.05f, 0.45f, linear), Epsilon);
        }

        [Test]
        public void LateralMovement_DoesNotIncreaseDraw()
        {
            float distance = BowDrawMath.CalculateDrawDistance(
                new Vector3(2f, 3f, -0.2f), Vector3.zero, Vector3.forward);
            Assert.AreEqual(0.2f, distance, Epsilon);
        }

        [Test]
        public void VerticalMovement_DoesNotIncreaseDraw()
        {
            float distance = BowDrawMath.CalculateDrawDistance(
                new Vector3(0f, 3f, -0.2f), Vector3.zero, Vector3.forward);
            Assert.AreEqual(0.2f, distance, Epsilon);
        }

        [Test]
        public void LaunchDirection_IsNormalized()
        {
            Vector3 velocity = BowDrawMath.CalculateLaunchVelocity(
                new Vector3(0f, 0f, 5f), 0.45f, 0.05f, 0.45f, 5f, 25f, linear);
            Assert.AreEqual(1f, velocity.normalized.magnitude, Epsilon);
        }

        [Test]
        public void LaunchSpeed_RespectsConfiguredLimits()
        {
            Assert.AreEqual(5f, BowDrawMath.CalculateLaunchSpeed(0f, 0.05f, 0.45f, 5f, 25f, linear), Epsilon);
            Assert.AreEqual(25f, BowDrawMath.CalculateLaunchSpeed(1f, 0.05f, 0.45f, 5f, 25f, linear), Epsilon);
        }

        [Test]
        public void InvalidDistance_DoesNotReturnNaN()
        {
            float result = BowDrawMath.NormalizeDraw(float.NaN, 0.05f, 0.45f);
            Assert.IsFalse(float.IsNaN(result));
        }

        [Test]
        public void ForceCurve_IsApplied()
        {
            AnimationCurve quadratic = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            float result = BowDrawMath.CalculateForceFactor(0.25f, 0.05f, 0.45f, quadratic);
            Assert.That(result, Is.GreaterThan(0f).And.LessThan(1f));
        }

        [Test]
        public void InvertedBowAxis_ProducesPredictableDistance()
        {
            float distance = BowDrawMath.CalculateDrawDistance(
                new Vector3(0f, 0f, 0.2f), Vector3.zero, Vector3.back);
            Assert.AreEqual(0.2f, distance, Epsilon);
        }

        [Test]
        public void EqualDrawLimits_DoNotDivideByZero()
        {
            float result = BowDrawMath.NormalizeDraw(0.1f, 0.1f, 0.1f);
            Assert.IsFalse(float.IsNaN(result));
            Assert.AreEqual(1f, result, Epsilon);
        }
    }
}
