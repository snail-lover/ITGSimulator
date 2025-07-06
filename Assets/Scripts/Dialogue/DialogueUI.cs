using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The DialogueUI is now responsible for its own animations.
[RequireComponent(typeof(Animator), typeof(AudioSource))]
public class DialogueUI : MonoBehaviour
{
    [Header("Core UI References")]
    public CanvasGroup dialogueSystemCanvasGroup; // Still useful for enabling/disabling interaction
    public TextMeshProUGUI speakerNameText;
    [Tooltip("The text element that is actually visible to the player.")]
    public TextMeshProUGUI visibleDialogueText;
    [Tooltip("An invisible copy of the text used to correctly size the dialogue box before the text has finished typing out.")]
    public TextMeshProUGUI sizingDialogueText;
    public Image characterPortraitImage;

    [Header("Typewriter Effect")]
    [SerializeField] private float charactersPerSecond = 50f;
    [SerializeField] private AudioClip typingSound;
    [Tooltip("Play sound every N characters. Set to 1 for every character, 2 for every other, etc.")]
    [SerializeField][Range(1, 5)] private int playSoundEveryNChars = 2;

    [Header("Choice References")]
    [Tooltip("The parent object for choice buttons (the Content of a ScrollView).")]
    public Transform choicesContainer;
    [Tooltip("The prefab for a single choice button. Must have the DynamicButton script on its root.")]
    public DynamicButton choiceButtonPrefab;
    [Tooltip("The ScrollRect containing the choices.")]
    public ScrollRect choicesScrollRect;

    [Header("Category Icons")]
    [Tooltip("Icon for Quest dialogue options")]
    public Sprite questIcon;
    [Tooltip("Icon for Love/Romance dialogue options")]
    public Sprite loveIcon;
    [Tooltip("Icon for Contextual dialogue options")]
    public Sprite contextualIcon;
    [Tooltip("Icon for General dialogue options")]
    public Sprite generalIcon;

    [Header("Special Action Buttons")]
    [SerializeField] private Button exitButton;
    [SerializeField] private Button statsButton;
    [SerializeField] private Button finalCutsceneButton;

    [Header("Sound Effects")]
    [Tooltip("The sound that plays when the dialogue panel animates in.")]
    [SerializeField] private AudioClip showSound;

    public Button StatsButton => statsButton;
    public NPCStatPage NpcStatPage { get; private set; }

    private Animator panelAnimator;
    private AudioSource audioSource;
    private Action onHideCompleteCallback;
    private Coroutine typewriterCoroutine;
    public bool isShowingGreetingDialogue = false; // Track if we're showing the initial greeting

    private void Awake()
    {
        // Get the animator component we added to the prefab.
        panelAnimator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        NpcStatPage = GetComponentInChildren<NPCStatPage>(true); // 'true' to find it even if it's inactive
        if (NpcStatPage == null)
        {
            Debug.LogError("[DialogueUI] NPCStatPage component was NOT found as a child of the DialogueUI prefab. The stats page will not function.", this);
        }

        if (dialogueSystemCanvasGroup != null)
        {
            // Start with UI non-interactive. The animator will handle visibility.
            dialogueSystemCanvasGroup.interactable = false;
            dialogueSystemCanvasGroup.blocksRaycasts = false;
        }

        // Ensure buttons are hidden initially
        if (exitButton != null) exitButton.gameObject.SetActive(false);
        if (statsButton != null) statsButton.gameObject.SetActive(false);
        if (finalCutsceneButton != null) finalCutsceneButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Public entry point to update content and show the dialogue panel.
    /// This combines the new separated logic into one simple call.
    /// </summary>
    public void ShowDialogue(DialogueManager manager, DialogueNode node, string speakerName, Sprite portraitSprite, bool isGreeting = false)
    {
        isShowingGreetingDialogue = isGreeting;
        UpdateDialogueContent(manager, node, speakerName, portraitSprite);
        AnimateIn();
    }

    /// <summary>
    /// Updates the content of the dialogue UI. Does NOT handle showing/hiding the panel.
    /// </summary>
    public void UpdateDialogueContent(DialogueManager manager, DialogueNode node, string speakerName, Sprite portraitSprite)
    {
        if (node == null) return;

        speakerNameText.text = speakerName;
        sizingDialogueText.text = node.text;
        visibleDialogueText.text = "";

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        typewriterCoroutine = StartCoroutine(Typewriter(node.text));

        if (characterPortraitImage != null)
        {
            characterPortraitImage.sprite = portraitSprite;
            characterPortraitImage.enabled = (portraitSprite != null);
        }

        // Clear old choices and reset scroll position
        foreach (Transform child in choicesContainer) Destroy(child.gameObject);
        choicesContainer.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        // Populate new choices in a coroutine to avoid layout issues
        StartCoroutine(PopulateChoices(manager, node));

        // Handle Exit Button visibility (preserved from original script)
        if (exitButton != null)
        {
            // The exit button is only available in normal dialogue, not in cutscenes.
            bool canExit = !manager.IsDialogueActiveForCutscene();
            exitButton.gameObject.SetActive(canExit);

            if (canExit)
            {
                // Hook up the button to end the dialogue.
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(() => manager.EndDialogue());
            }
        }
    }

    public void SetStatsButtonVisibility(bool isVisible)
    {
        if (statsButton != null)
        {
            statsButton.gameObject.SetActive(isVisible);
        }
    }

    /// <summary>
    /// Determines the appropriate icon sprite based on the dialogue choice category
    /// </summary>
    private Sprite GetIconForChoice(DialogueChoice choice, DialogueManager manager)
    {
        if (!isShowingGreetingDialogue) return null; // No icons for non-greeting dialogue

        // Check if this is a grouped choice (contextual or general)
        if (choice.nextNodeID == "__CONTEXTUAL_GROUP__" || choice.nextNodeID == "__GENERAL_GROUP__")
        {
            return null;
        }

        // For individual choices, we need to determine their category
        // We'll check against the manager's categorized lists
        var questTopics = manager.GetQuestTopics();
        var loveTopics = manager.GetLoveTopics();
        var contextualTopics = manager.GetContextualTopics();

        // Check if this choice matches any in our categorized lists
        if (questTopics.Any(t => t.choiceText == choice.choiceText && t.nextNodeID == choice.nextNodeID))
            return questIcon;

        if (loveTopics.Any(t => t.choiceText == choice.choiceText && t.nextNodeID == choice.nextNodeID))
            return loveIcon;

        if (contextualTopics.Any(t => t.choiceText == choice.choiceText && t.nextNodeID == choice.nextNodeID))
            return contextualIcon;

        // Default to general icon
        return generalIcon;
    }

    private IEnumerator PopulateChoices(DialogueManager manager, DialogueNode node)
    {
        //Debug.Log("=== PopulateChoices START ===");

        choicesScrollRect.gameObject.SetActive(false);
        foreach (Transform child in choicesContainer)
        {
            Destroy(child.gameObject);
        }
        yield return null;

        if (node.choices == null || !node.choices.Any())
        {
            Debug.Log("No choices available, exiting PopulateChoices");
            yield break;
        }

        // STEP 1: CREATE & SIZE
        var availableChoices = node.choices.Where(c => c != null && manager.IsChoiceAvailable(c)).ToList();
        List<DynamicButton> newButtonInstances = new List<DynamicButton>();

        //Debug.Log($"Available choices count: {availableChoices.Count}");

        if (choiceButtonPrefab == null)
        {
            Debug.LogError("FATAL: The 'Choice Button Prefab' is NOT ASSIGNED in the DialogueUI inspector!", this);
            yield break;
        }

        foreach (var choice in availableChoices)
        {
            //Debug.Log($"Creating button for choice: '{choice.choiceText}'");

            DynamicButton choiceInstance = Instantiate(choiceButtonPrefab, choicesContainer);
            if (choiceInstance == null)
            {
                Debug.LogError("FATAL: Instantiate(choiceButtonPrefab) returned NULL. Is the prefab valid?", this);
                continue;
            }

            Debug.Log($"Button instantiated successfully: {choiceInstance.name}");

            // Check if the button has required components
            var rectTransform = choiceInstance.GetComponent<RectTransform>();
            var buttonComponent = choiceInstance.GetComponent<Button>();

            if (rectTransform == null)
            {
                //Debug.LogError($"Button {choiceInstance.name} is missing RectTransform!", choiceInstance);
                continue;
            }

            if (buttonComponent == null)
            {
                //Debug.LogError($"Button {choiceInstance.name} is missing Button component!", choiceInstance);
                continue;
            }

            //Debug.Log($"Before SetText - Button size: {rectTransform.sizeDelta}");

            // Get the appropriate icon for this choice
            Sprite iconSprite = GetIconForChoice(choice, manager);

            // Set text and icon
            choiceInstance.SetText(choice.choiceText);
            choiceInstance.SetIcon(iconSprite); // This method needs to be added to DynamicButton

            //Debug.Log($"After SetText - Button size: {rectTransform.sizeDelta}");

            buttonComponent.interactable = true;
            DialogueChoice currentChoice = choice;
            buttonComponent.onClick.AddListener(() => {
                manager.HandleChoiceSelected(currentChoice);
            });

            newButtonInstances.Add(choiceInstance);
        }

        if (newButtonInstances.Count == 0)
        {
            //Debug.Log("No valid button instances created, exiting");
            yield break;
        }

        Debug.Log($"Created {newButtonInstances.Count} button instances");

        // STEP 2: WAIT - Let's wait longer to ensure layout is complete
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); // Extra frame for safety

        // STEP 3: POSITION (with heavy debugging)
        Debug.Log("--- Starting Button Positioning ---");
        RectTransform previousButtonRect = null;

        for (int i = 0; i < newButtonInstances.Count; i++)
        {
            DynamicButton currentButton = newButtonInstances[i];

            if (currentButton == null)
            {
                Debug.LogError($"Button at index {i} is NULL in the list. Skipping.");
                continue;
            }

            RectTransform currentButtonRect = currentButton.GetComponent<RectTransform>();
            if (currentButtonRect == null)
            {
                Debug.LogError($"Button {i} ('{currentButton.name}') has no RectTransform!", currentButton);
                continue;
            }

            Debug.Log($"Button {i} BEFORE positioning:");
            Debug.Log($"  - Name: {currentButton.name}");
            Debug.Log($"  - Size: {currentButtonRect.sizeDelta}");
            Debug.Log($"  - Position: {currentButtonRect.anchoredPosition}");
            Debug.Log($"  - Previous button is: {(previousButtonRect == null ? "NULL (first button)" : previousButtonRect.name)}");

            if (previousButtonRect != null)
            {
                Debug.Log($"  - Previous button size: {previousButtonRect.sizeDelta}");
                Debug.Log($"  - Previous button position: {previousButtonRect.anchoredPosition}");
                Debug.Log($"  - Previous button rect height: {previousButtonRect.rect.height}");
            }

            // THIS IS THE CRITICAL CALL
            currentButton.ApplyLayoutSpacing(previousButtonRect);

            Debug.Log($"Button {i} AFTER positioning:");
            Debug.Log($"  - Position: {currentButtonRect.anchoredPosition}");
            Debug.Log($"  - Size: {currentButtonRect.sizeDelta}");

            // This is the CRITICAL ASSIGNMENT for the next loop
            previousButtonRect = currentButtonRect;

            if (previousButtonRect == null)
            {
                Debug.LogError($"CRITICAL FAILURE: RectTransform assignment failed for button {i}", currentButton.gameObject);
            }
        }
        Debug.Log("--- Finished Button Positioning ---");

        // STEP 4: FINALIZE
        choicesScrollRect.gameObject.SetActive(true);

        DynamicButton lastButton = newButtonInstances.Last();
        RectTransform containerRect = choicesContainer.GetComponent<RectTransform>();
        RectTransform lastButtonRect = lastButton.GetComponent<RectTransform>();

        float totalHeight = -lastButtonRect.anchoredPosition.y + lastButtonRect.rect.height;

        //Debug.Log($"Container sizing:");
        //Debug.Log($"  - Last button position Y: {lastButtonRect.anchoredPosition.y}");
        //Debug.Log($"  - Last button height: {lastButtonRect.rect.height}");
        //Debug.Log($"  - Calculated total height: {totalHeight}");
        //Debug.Log($"  - Container size before: {containerRect.sizeDelta}");

        containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, totalHeight);

        //Debug.Log($"  - Container size after: {containerRect.sizeDelta}");

        containerRect.anchoredPosition = Vector2.zero;

        //Debug.Log("=== PopulateChoices END ===");
    }

    /// <summary>
    /// Triggers the animation to show the dialogue panel.
    /// </summary>
    public void AnimateIn()
    {
        panelAnimator.SetTrigger("Show");
        dialogueSystemCanvasGroup.interactable = true;
        dialogueSystemCanvasGroup.blocksRaycasts = true;
    }

    public void PlayShowSound()
    {
        if (showSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(showSound);
        }
    }

    /// <summary>
    /// Triggers the animation to hide the dialogue panel.
    /// </summary>
    public void AnimateOut(Action onHidden = null)
    {
        onHideCompleteCallback = onHidden;
        panelAnimator.SetTrigger("Hide");
        dialogueSystemCanvasGroup.interactable = false;
        dialogueSystemCanvasGroup.blocksRaycasts = false;
    }

    // This method is called by an Animation Event at the end of the "DialogueUI_Hide" animation clip.
    public void OnHideAnimationComplete()
    {
        onHideCompleteCallback?.Invoke();
        onHideCompleteCallback = null; // Clear the callback to prevent it from being called again
    }

    private IEnumerator Typewriter(string textToType)
    {
        // Give the layout a frame to update with the new sizing text.
        yield return new WaitForEndOfFrame();

        int charIndex = 0;
        foreach (char c in textToType)
        {
            // Allow the player to skip the effect by clicking.
            if (Input.GetMouseButtonDown(0))
            {
                visibleDialogueText.text = textToType;
                break; // Exit the loop
            }

            visibleDialogueText.text += c;

            // Play sound effect periodically
            if (typingSound != null && charIndex % playSoundEveryNChars == 0)
            {
                audioSource.PlayOneShot(typingSound);
            }
            charIndex++;

            yield return new WaitForSeconds(1f / charactersPerSecond);
        }

        typewriterCoroutine = null; // Mark as finished
    }

    /// <summary>
    /// Hides all dialogue UI elements and triggers the hide animation.
    /// </summary>
    public void HideDialogue(Action onHidden = null)
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        if (exitButton != null) exitButton.gameObject.SetActive(false);
        SetFinalCutsceneButtonVisibility(false, null);
        AnimateOut(onHidden);
    }

    public void SetFinalCutsceneButtonVisibility(bool isVisible, Action onClickCallback)
    {
        if (finalCutsceneButton == null) return;
        finalCutsceneButton.gameObject.SetActive(isVisible);
        finalCutsceneButton.onClick.RemoveAllListeners();
        if (isVisible && onClickCallback != null)
        {
            finalCutsceneButton.onClick.AddListener(() => onClickCallback());
        }
    }
}