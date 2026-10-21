using UnityEngine;

namespace ArcheryGame.Archery
{
    [RequireComponent(typeof(BowController))]
    public class BowVisualController : MonoBehaviour
    {
        [SerializeField] private float halfHeight = 0.55f;
        [SerializeField] private float bowDepth = 0.12f;
        [SerializeField] private float lineWidth = 0.025f;

        private LineRenderer bow;
        private LineRenderer grip;
        private Material lineMaterial;

        private void Awake()
        {
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            bow = CreateLine("BowLimbs", 9);
            grip = CreateLine("BowGrip", 2);
        }

        private void LateUpdate()
        {
            Vector3[] points =
            {
                new Vector3(0f, halfHeight, 0f),
                new Vector3(-bowDepth * 0.65f, halfHeight * 0.75f, 0f),
                new Vector3(-bowDepth, halfHeight * 0.35f, 0f),
                new Vector3(-bowDepth * 0.8f, 0f, 0f),
                new Vector3(-bowDepth, -halfHeight * 0.35f, 0f),
                new Vector3(-bowDepth * 0.65f, -halfHeight * 0.75f, 0f),
                new Vector3(0f, -halfHeight, 0f),
                new Vector3(-bowDepth * 0.65f, -halfHeight * 0.75f, 0f),
                new Vector3(0f, halfHeight, 0f)
            };

            bow.SetPositions(points);
            grip.SetPosition(0, new Vector3(-bowDepth, 0.12f, 0f));
            grip.SetPosition(1, new Vector3(-bowDepth, -0.12f, 0f));
        }

        private LineRenderer CreateLine(string name, int positionCount)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = positionCount;
            line.useWorldSpace = false;
            line.widthMultiplier = lineWidth;
            line.startColor = new Color(0.35f, 0.12f, 0.03f);
            line.endColor = new Color(0.35f, 0.12f, 0.03f);
            line.material = lineMaterial;
            return line;
        }

        private void OnDestroy()
        {
            if (lineMaterial != null)
                Destroy(lineMaterial);
        }
    }
}
