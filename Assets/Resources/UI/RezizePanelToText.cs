using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(LayoutElement))]
public class ResizePanelToText : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("The TextMeshPro component to measure.")]
    [SerializeField] private TextMeshProUGUI textComponent;

    [Tooltip("The LayoutElement of the root panel to resize.")]
    [SerializeField] private LayoutElement layoutElement;

    [Header("Sizing Options")]
    [Tooltip("Extra vertical padding to add to the text's preferred height.")]
    [SerializeField] private float topBottomPadding = 20f;
    [Tooltip("Extra horizontal padding for text wrapping calculations.")]
    [SerializeField] private float leftRightPadding = 20f;


    void OnValidate()
    {
        if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();
        if (textComponent == null) textComponent = GetComponentInChildren<TextMeshProUGUI>();
    }

    // VVV ADD THIS ATTRIBUTE BELOW VVV
    [ContextMenu("Update Panel Size Now")]
    public void UpdatePanelSize()
    {
        if (textComponent == null || layoutElement == null)
        {
            Debug.LogError("ResizePanelToText is missing required component references.", this);
            return;
        }

        // This calculation now assumes it's being controlled by a parent Layout Group
        // So we get the parent's width.
        RectTransform parentRect = transform.parent.GetComponent<RectTransform>();
        if (parentRect == null)
        {
            Debug.LogWarning("Cannot update panel size without a parent RectTransform.", this);
            return;
        }

        float availableWidth = parentRect.rect.width - leftRightPadding;

        float preferredHeight = textComponent.GetPreferredValues(textComponent.text, availableWidth, 0).y;

        layoutElement.preferredHeight = preferredHeight + topBottomPadding;

        // In the editor, we might need to nudge the layout group to refresh.
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }
#endif
    }
}