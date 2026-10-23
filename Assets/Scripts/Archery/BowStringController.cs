using UnityEngine;

namespace ArcheryGame.Archery
{
    public class BowStringController : MonoBehaviour
    {
        [SerializeField] private LineRenderer upperString;
        [SerializeField] private LineRenderer lowerString;
        [SerializeField] private Transform topStringPoint;
        [SerializeField] private Transform bottomStringPoint;
        [SerializeField] private float stringWidth = 0.008f;
        [SerializeField] private Color stringColor = new Color(0.92f, 0.92f, 0.92f, 1f);

        private Vector3 nockingPosition;
        private Material stringMaterial;

        private void Awake()
        {
            Shader stringShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (stringShader == null) stringShader = Shader.Find("Sprites/Default");
            if (stringShader != null)
            {
                stringMaterial = new Material(stringShader)
                {
                    color = stringColor
                };
            }

            ConfigureLine(upperString);
            ConfigureLine(lowerString);
            nockingPosition = transform.position;
        }

        private void OnDestroy()
        {
            if (stringMaterial != null) Destroy(stringMaterial);
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

        private void ConfigureLine(LineRenderer line)
        {
            if (line == null) return;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = stringWidth;
            line.endWidth = stringWidth;
            line.startColor = stringColor;
            line.endColor = stringColor;
            if (stringMaterial != null) line.sharedMaterial = stringMaterial;
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
