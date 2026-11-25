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

        private Vector3 localNockingPosition;
        private bool isDrawn;
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
        }

        private void OnDestroy()
        {
            if (stringMaterial != null) Destroy(stringMaterial);
        }

        public void SetNockingPosition(Vector3 position)
        {
            localNockingPosition = transform.InverseTransformPoint(position);
            isDrawn = true;
        }

        public void ResetToRest() => isDrawn = false;

        private void LateUpdate()
        {
            if (topStringPoint == null || bottomStringPoint == null) return;

            Vector3 nockingPosition = isDrawn
                ? transform.TransformPoint(localNockingPosition)
                : (topStringPoint.position + bottomStringPoint.position) * 0.5f;
            UpdateLine(upperString, topStringPoint, nockingPosition);
            UpdateLine(lowerString, bottomStringPoint, nockingPosition);
        }

        private void ConfigureLine(LineRenderer line)
        {
            if (line == null) return;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
            line.widthMultiplier = stringWidth;
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
