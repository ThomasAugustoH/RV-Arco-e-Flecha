using UnityEngine;

namespace ArcheryGame.Archery
{
    public class BowStringController : MonoBehaviour
    {
        [SerializeField] private LineRenderer upperString;
        [SerializeField] private LineRenderer lowerString;
        [SerializeField] private Transform topStringPoint;
        [SerializeField] private Transform bottomStringPoint;

        private Vector3 nockingPosition;

        private void Awake()
        {
            ConfigureLine(upperString);
            ConfigureLine(lowerString);
            nockingPosition = transform.position;
        }

        public void SetNockingPosition(Vector3 position)
        {
            nockingPosition = position;
        }

        private void LateUpdate()
        {
            UpdateLine(upperString, topStringPoint, nockingPosition);
            UpdateLine(lowerString, bottomStringPoint, nockingPosition);
        }

        private static void ConfigureLine(LineRenderer line)
        {
            if (line == null) return;
            line.positionCount = 2;
            line.useWorldSpace = true;
        }

        private static void UpdateLine(
            LineRenderer line, Transform endpoint, Vector3 nockingPosition)
        {
            if (line == null || endpoint == null) return;
            line.SetPosition(0, endpoint.position);
            line.SetPosition(1, nockingPosition);
        }
    }
}
