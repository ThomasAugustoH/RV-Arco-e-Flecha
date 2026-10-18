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
        /// <summary>
        /// Standard 5-ring target configuration calibrated for a 0.5m diameter target.
        /// Rings are sorted from smallest (bullseye) to largest (edge).
        /// </summary>
        public static TargetRingConfig[] StandardRings => new[]
        {
            new TargetRingConfig(0.02f, 10),  // Bullseye
            new TargetRingConfig(0.05f, 8),   // Inner
            new TargetRingConfig(0.10f, 5),   // Middle
            new TargetRingConfig(0.18f, 2),   // Outer
            new TargetRingConfig(0.25f, 1),   // Edge
        };
    }
}
