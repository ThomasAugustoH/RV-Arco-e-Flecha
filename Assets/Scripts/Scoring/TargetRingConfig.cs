using System;

namespace ArcheryGame.Scoring
{
    /// <summary>
    /// Represents a single ring (zone) on a target board.
    /// Rings should be sorted from smallest radius (bullseye) to largest (edge).
    /// </summary>
    [Serializable]
    public struct TargetRingConfig
    {
        /// <summary>
        /// The maximum radius of this ring in meters, measured from the target center.
        /// </summary>
        public float radius;

        /// <summary>
        /// The score awarded for hitting within this ring.
        /// </summary>
        public int points;

        public TargetRingConfig(float radius, int points)
        {
            this.radius = radius;
            this.points = points;
        }
    }

    /// <summary>
    /// Provides default ring configurations for standard archery targets.
    /// </summary>
    public static class DefaultTargetConfig
    {
        /// <summary>Radius of the default target face in meters (Target prefab is 0.5m wide).</summary>
        public const float TargetRadius = 0.25f;

        /// <summary>Number of scoring bands on the default target.</summary>
        public const int BandCount = 5;

        /// <summary>Points awarded for the outermost band. Each band closer to the center doubles it.</summary>
        public const int EdgePoints = 1;

        /// <summary>
        /// Standard 5-band target for a 0.5m diameter target. Bands have equal width (5cm)
        /// and points double on every band towards the center: 16, 8, 4, 2, 1.
        /// Rings are sorted from smallest (bullseye) to largest (edge).
        /// </summary>
        public static TargetRingConfig[] StandardRings => CreateDoublingRings(TargetRadius, BandCount, EdgePoints);

        /// <summary>
        /// Builds <paramref name="bandCount"/> equal-width bands covering <paramref name="targetRadius"/>.
        /// The outermost band is worth <paramref name="edgePoints"/> and every band closer to the
        /// center is worth double the previous one.
        /// </summary>
        /// <returns>Rings sorted by radius ascending (bullseye first). Empty when inputs are invalid.</returns>
        public static TargetRingConfig[] CreateDoublingRings(float targetRadius, int bandCount, int edgePoints)
        {
            if (targetRadius <= 0f || bandCount <= 0 || edgePoints <= 0)
                return new TargetRingConfig[0];

            // Bullseye points would overflow int.
            if (bandCount > 31 || ((long)edgePoints << (bandCount - 1)) > int.MaxValue)
                return new TargetRingConfig[0];

            var rings = new TargetRingConfig[bandCount];
            float bandWidth = targetRadius / bandCount;

            for (int i = 0; i < bandCount; i++)
            {
                // i = 0 is the bullseye; bandsFromEdge = 0 is the outermost band.
                int bandsFromEdge = bandCount - 1 - i;
                float radius = i == bandCount - 1 ? targetRadius : bandWidth * (i + 1);
                rings[i] = new TargetRingConfig(radius, edgePoints << bandsFromEdge);
            }

            return rings;
        }
    }
}
