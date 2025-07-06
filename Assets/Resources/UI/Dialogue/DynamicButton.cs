using UnityEngine;
using UnityEngine.UI;
using TMPro;

// This attribute allows the script to run in the editor, so you can see changes without pressing Play.
[ExecuteAlways]
// This ensures the necessary components are on the GameObject when you add this script.
[RequireComponent(typeof(RectTransform), typeof(Button))]
public class DynamicButton : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("The TextMeshProUGUI component that displays the button's text.")]
    [SerializeField] private TextMeshProUGUI textComponent;

    [Tooltip("The Image component for the button's background.")]
    [SerializeField] private Image buttonImage;

    [Tooltip("The Image component for the button's icon (optional).")]
    [SerializeField] private Image iconImage;

    [Header("Sizing & Layout")]
    [Tooltip("The maximum width the button can be before text wraps.")]
    [SerializeField] private float maxWidth = 400f;

    [Tooltip("Padding between the text and the edge of the button image.")]
    [SerializeField] private Padding padding;

    [Tooltip("The vertical space between this button and the one above it. Assumes a top-down vertical layout.")]
    [SerializeField] private float spacing = 10f;

    [Header("Icon Settings")]
    [Tooltip("The size of the icon (width and height).")]
    [SerializeField] private float iconSize = 24f;

    // --- NEW ---
    [Tooltip("The horizontal offset of the icon from the button's left edge. Positive values push the icon to the left, outside the button.")]
    [SerializeField] private float iconXOffset = 10f;
    // --- END NEW ---

    // The iconTextSpacing is no longer needed as the icon and text are decoupled.
    // [SerializeField] private float iconTextSpacing = 8f;

    // A helper struct to make padding settings clearer in the Inspector.
    [System.Serializable]
    public struct Padding
    {
        public float left, right, top, bottom;
    }

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        EnsureIconIsBehindButton();
    }

    // This is called in the editor whenever a value is changed in the inspector.
    private void OnValidate()
    {
        if (textComponent != null && buttonImage != null)
        {
            EnsureRectTransform();
            EnsureIconIsBehindButton();
            ResizeButton();
        }
    }

    /// <summary>
    /// Ensures rectTransform is assigned. This is needed because SetText might be called before Awake.
    /// </summary>
    private void EnsureRectTransform()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
    }

    private void EnsureIconIsBehindButton()
    {
        if (iconImage != null && iconImage.transform.parent == this.transform)
        {
            // In a UI Canvas, the first sibling is rendered first (i.e., at the bottom).
            iconImage.transform.SetAsFirstSibling();
        }
    }

    /// <summary>
    /// Sets the button's text and automatically resizes and repositions it.
    /// This is the main method you should call from other scripts.
    /// </summary>
    /// <param name="newText">The text to display on the button.</param>
    public void SetText(string newText)
    {
        if (textComponent == null)
        {
            Debug.LogError("Text Component is not assigned.", this);
            return;
        }
        textComponent.text = newText;
        ResizeButton();
    }


    /// <summary>
    /// Sets the button's icon sprite. Pass null to hide the icon.
    /// </summary>
    /// <param name="iconSprite">The sprite to display as an icon, or null to hide the icon.</param>
    public void SetIcon(Sprite iconSprite)
    {
        if (iconImage == null)
        {
            return;
        }

        if (iconSprite == null)
        {
            iconImage.gameObject.SetActive(false);
        }
        else
        {
            iconImage.sprite = iconSprite;
            iconImage.gameObject.SetActive(true);

            RectTransform iconRect = iconImage.rectTransform;
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        }

        ResizeButton();
    }

    /// <summary>
    /// A helper method to easily trigger a resize from the Inspector context menu.
    /// </summary>
    [ContextMenu("Update Button Visuals")]
    private void ForceResize()
    {
        ResizeButton();
    }

    /// <summary>
    /// Core logic to resize the button and position elements.
    /// The icon is now positioned independently and does not affect button or text layout.
    /// </summary>
    private void ResizeButton()
    {
        // --- MODIFIED LOGIC ---

        EnsureRectTransform();

        if (textComponent == null || buttonImage == null || rectTransform == null)
        {
            return;
        }

        // --- 1. Calculate available width for text ---
        // The icon no longer affects the available text width.
        float availableTextWidth = maxWidth - padding.left - padding.right;

        // --- 2. Calculate Text Size ---
        Vector2 preferredTextSize = textComponent.GetPreferredValues(textComponent.text, availableTextWidth, 0);

        // --- 3. Calculate Button Size (based ONLY on text and padding) ---
        float newWidth = preferredTextSize.x + padding.left + padding.right;
        float newHeight = preferredTextSize.y + padding.top + padding.bottom;

        // Ensure the button's final width doesn't exceed the max width.
        newWidth = Mathf.Min(newWidth, maxWidth);

        // --- 4. Apply Button Size ---
        rectTransform.sizeDelta = new Vector2(newWidth, newHeight);

        // --- 5. Position Text ---
        // The text is always positioned simply within the padding, as if there is no icon.
        RectTransform textRect = textComponent.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(padding.left, padding.bottom);
        textRect.offsetMax = new Vector2(-padding.right, -padding.top);

        // --- 6. Position Icon (if active) ---
        // The icon is now positioned relative to the button's final state, but does not influence it.
        bool hasActiveIcon = iconImage != null && iconImage.gameObject.activeInHierarchy;
        if (hasActiveIcon)
        {
            RectTransform iconRect = iconImage.rectTransform;
            // Anchor to the middle-left of the button's RectTransform.
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);

            // Position it. The X position is determined by the new offset.
            // A negative X moves it to the left. We use iconSize * 0.5 to center the icon on the edge,
            // then subtract the offset to push it further out.
            iconRect.anchoredPosition = new Vector2(-(iconSize * 0.5f) - iconXOffset, 0);
        }
    }

    /// <summary>
    /// Positions this button vertically based on its sibling above it.
    /// This mimics a vertical layout group without needing the component.
    /// NOTE: This assumes the parent container has its pivot and anchor at the top-center.
    /// </summary>
    public void ApplyLayoutSpacing(RectTransform previousSiblingRect = null)
    {
        EnsureRectTransform();

        if (rectTransform == null)
        {
            Debug.LogError("Failed to get RectTransform component", this);
            return;
        }

        if (previousSiblingRect == null)
        {
            rectTransform.anchoredPosition = new Vector2(0, 0);
        }
        else
        {
            float newY = previousSiblingRect.anchoredPosition.y - previousSiblingRect.rect.height - spacing;
            rectTransform.anchoredPosition = new Vector2(0, newY);
        }
    }
}