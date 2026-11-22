using TMPro;
using UnityEngine;

public enum WorldMenuAction
{
    None,
    Easy,
    Medium,
    Hard,
    ClearTargets
}

/// <summary>Raycastable surface used by the physical in-world menu.</summary>
public class WorldMenuButton : MonoBehaviour
{
    private static readonly Color HoverColor = new Color(0.92f, 0.58f, 0.12f);

    private WorldMenuController menu;
    private WorldMenuAction action;
    private Renderer buttonRenderer;
    private Material runtimeMaterial;
    private Color normalColor;
    private bool selected;
    private bool hovered;

    public void Initialize(WorldMenuController owner, WorldMenuAction buttonAction, Color color,
        bool isSelected, string label, Vector2 labelSize, float fontSize)
    {
        menu = owner;
        action = buttonAction;
        normalColor = color;
        selected = isSelected;
        buttonRenderer = GetComponent<Renderer>();

        // Trigger colliders remain raycastable but let arrows pass through the menu.
        Collider menuCollider = GetComponent<Collider>();
        if (menuCollider != null)
            menuCollider.isTrigger = true;

        if (buttonRenderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader != null)
            {
                runtimeMaterial = new Material(shader);
                buttonRenderer.sharedMaterial = runtimeMaterial;
            }
        }

        if (!string.IsNullOrEmpty(label))
        {
            GameObject labelObject = new GameObject("Label", typeof(TextMeshPro));
            labelObject.transform.SetParent(transform, false);
            Vector3 buttonScale = transform.localScale;
            // The camera faces +Z, so the visible/front face of the menu points toward -Z.
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.022f / buttonScale.z);
            labelObject.transform.localScale = new Vector3(
                1f / buttonScale.x,
                1f / buttonScale.y,
                1f / buttonScale.z);
            labelObject.transform.localRotation = Quaternion.identity;

            TextMeshPro text = labelObject.GetComponent<TextMeshPro>();
            text.text = label;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.color = Color.white;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color32(0, 0, 0, 220);
            text.rectTransform.sizeDelta = labelSize;
        }

        RefreshColor();
    }

    public void SetHovered(bool value)
    {
        if (hovered == value)
            return;

        hovered = value;
        RefreshColor();
    }

    public void SetSelected(bool value)
    {
        if (selected == value)
            return;

        selected = value;
        RefreshColor();
    }

    public void Activate()
    {
        if (action != WorldMenuAction.None && menu != null)
            menu.Activate(action);
    }

    private void RefreshColor()
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.color = hovered
            ? HoverColor
            : selected
                ? new Color(0.05f, 0.43f, 0.42f)
                : normalColor;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
