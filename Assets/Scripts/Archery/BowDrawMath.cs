using UnityEngine;

namespace ArcheryGame.Archery
{
    /// <summary>
    /// Pure calculations used by the bow. Only the component of the hand
    /// movement along the bow axis contributes to draw strength.
    /// </summary>
    public static class BowDrawMath
    {
        public static float CalculateDrawDistance(
            Vector3 drawHandPosition,
            Vector3 nockingPoint,
            Vector3 bowForward)
        {
            Vector3 axis = bowForward.sqrMagnitude > Mathf.Epsilon
                ? bowForward.normalized
                : Vector3.forward;

            return Vector3.Dot(drawHandPosition - nockingPoint, -axis);
        }

        public static float NormalizeDraw(
            float drawDistance,
            float minDrawDistance,
            float maxDrawDistance)
        {
            if (float.IsNaN(drawDistance) || float.IsInfinity(drawDistance))
                return 0f;

            if (maxDrawDistance <= minDrawDistance)
                return drawDistance >= maxDrawDistance ? 1f : 0f;

            return Mathf.Clamp01(
                (drawDistance - minDrawDistance) /
                (maxDrawDistance - minDrawDistance));
        }

        public static float CalculateNormalizedDraw(
            float drawDistance,
            float minDrawDistance,
            float maxDrawDistance)
        {
            return NormalizeDraw(drawDistance, minDrawDistance, maxDrawDistance);
        }

        public static float CalculateForceFactor(
            float drawDistance,
            float minDrawDistance,
            float maxDrawDistance,
            AnimationCurve drawCurve)
        {
            float normalizedDraw = NormalizeDraw(
                drawDistance, minDrawDistance, maxDrawDistance);

            return drawCurve == null
                ? normalizedDraw
                : Mathf.Clamp01(drawCurve.Evaluate(normalizedDraw));
        }

        public static float CalculateLaunchSpeed(
            float drawDistance,
            float minDrawDistance,
            float maxDrawDistance,
            float minLaunchSpeed,
            float maxLaunchSpeed,
            AnimationCurve drawCurve)
        {
            float forceFactor = CalculateForceFactor(
                drawDistance, minDrawDistance, maxDrawDistance, drawCurve);
            return Mathf.Lerp(minLaunchSpeed, maxLaunchSpeed, forceFactor);
        }

        public static Vector3 CalculateLaunchVelocity(
            Vector3 bowForward,
            float drawDistance,
            float minDrawDistance,
            float maxDrawDistance,
            float minLaunchSpeed,
            float maxLaunchSpeed,
            AnimationCurve drawCurve)
        {
            Vector3 direction = bowForward.sqrMagnitude > Mathf.Epsilon
                ? bowForward.normalized
                : Vector3.forward;

            float speed = CalculateLaunchSpeed(
                drawDistance, minDrawDistance, maxDrawDistance,
                minLaunchSpeed, maxLaunchSpeed, drawCurve);
            return direction * speed;
        }
    }
}
