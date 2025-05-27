using UnityEngine;
using UnityEngine.UI; // For legacy UI Text
using TMPro; // For TextMeshPro, if you use it

public class ShowHoverText : MonoBehaviour, IHoverable
{
    [Tooltip("Prefab for the hover text UI element. Must have a Text or TextMeshProUGUI component.")]
    public GameObject hoverTextPrefab;

    [Tooltip("Optional custom text. If empty, GameObject's name will be used.")]
    public string customHoverText = "";

    [Tooltip("Offset from the mouse cursor.")]
    public Vector2 offset = new Vector2(0, 20);

    private GameObject currentHoverTextInstance;
    private RectTransform hoverTextRect;
    private Text legacyTextComponent; // For Unity UI Text
    private TextMeshProUGUI tmProTextComponent; // For TextMeshPro

    private Canvas mainCanvas; // Cache the canvas

    void Start()
    {
        // Find the main canvas in the scene. You might want a more robust way to get this
        // e.g., assign it via Inspector, or have a CanvasLocator singleton.
        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("ShowHoverText: No Canvas found in the scene! Hover text will not work.", gameObject);
        }
    }

    public void WhenHovered()
    {
        if (currentHoverTextInstance == null && hoverTextPrefab != null && mainCanvas != null)
        {
            currentHoverTextInstance = Instantiate(hoverTextPrefab, mainCanvas.transform);
            hoverTextRect = currentHoverTextInstance.GetComponent<RectTransform>();

            // Try to get Text or TextMeshProUGUI component
            legacyTextComponent = currentHoverTextInstance.GetComponent<Text>();
            tmProTextComponent = currentHoverTextInstance.GetComponent<TextMeshProUGUI>();

            if (legacyTextComponent == null && tmProTextComponent == null)
            {
                Debug.LogError("Hover text prefab is missing a Text or TextMeshProUGUI component!", hoverTextPrefab);
                Destroy(currentHoverTextInstance);
                currentHoverTextInstance = null;
                return;
            }

            string textToShow = string.IsNullOrEmpty(customHoverText) ? gameObject.name : customHoverText;

            if (tmProTextComponent != null)
            {
                tmProTextComponent.text = textToShow;
            }
            else if (legacyTextComponent != null)
            {
                legacyTextComponent.text = textToShow;
            }

            // Initial position update
            UpdateTextPosition();
        }
    }

    public void HideHover()
    {
        if (currentHoverTextInstance != null)
        {
            Destroy(currentHoverTextInstance);
            currentHoverTextInstance = null;
            hoverTextRect = null;
            legacyTextComponent = null;
            tmProTextComponent = null;
        }
    }

    void Update()
    {
        // Continuously update position if text is active
        if (currentHoverTextInstance != null && hoverTextRect != null)
        {
            UpdateTextPosition();
        }
    }

    void UpdateTextPosition()
    {
        // Convert mouse position to canvas space
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.transform as RectTransform,
            Input.mousePosition,
            mainCanvas.worldCamera, // Use canvas's camera, or Camera.main if ScreenSpaceOverlay
            out localPoint
        );
        hoverTextRect.localPosition = localPoint + offset;
    }

    // Ensure text is hidden if this component is disabled or destroyed
    void OnDisable()
    {
        HideHover();
    }

    void OnDestroy()
    {
        HideHover();
    }
}