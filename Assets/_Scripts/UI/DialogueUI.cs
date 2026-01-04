using Game.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The DialogueUI is now responsible for its own animations.
[RequireComponent(typeof(Animator), typeof(AudioSource))]
public class DialogueUI : MonoBehaviour, IDialogueUIRoot
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
    public void ShowDialogue(DialogueViewNode view)
    {
        isShowingGreetingDialogue = view.isGreeting;
        UpdateDialogueContent(view);
        AnimateIn(); // you already have this
    }

    /// <summary>
    /// Updates the content of the dialogue UI. Does NOT handle showing/hiding the panel.
    /// </summary>
    public void UpdateDialogueContent(DialogueViewNode view)
    {
        if (view == null) return;

        // Header & text
        if (speakerNameText) speakerNameText.text = view.speakerName;
        if (sizingDialogueText) sizingDialogueText.text = view.text;
        if (visibleDialogueText) visibleDialogueText.text = "";

        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(Typewriter(view.text));

        // Portrait
        if (characterPortraitImage)
        {
            characterPortraitImage.sprite = view.portrait;
            characterPortraitImage.enabled = (view.portrait != null);
        }

        // Rebuild choices
        foreach (Transform child in choicesContainer) Destroy(child.gameObject);
        choicesContainer.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        StartCoroutine(PopulateChoices(view));
    }

    private IEnumerator PopulateChoices(DialogueViewNode view)
    {
        if (choicesScrollRect) choicesScrollRect.gameObject.SetActive(false);

        // Clean up (second pass to be safe)
        foreach (Transform child in choicesContainer) Destroy(child.gameObject);
        yield return null;

        if (view.choices == null || view.choices.Count == 0)
            yield break;

        var newButtons = new List<DynamicButton>();

        foreach (var choice in view.choices)
        {
            if (choice == null) continue;

            var btn = Instantiate(choiceButtonPrefab, choicesContainer);
            var button = btn.GetComponent<UnityEngine.UI.Button>();
            if (btn == null || button == null) continue;

            btn.SetText(choice.text);
            btn.SetIcon(choice.icon); // safe if null
            button.interactable = true;

            // Wire the callback provided by DialogueManager
            button.onClick.AddListener(() => choice.onClick?.Invoke());

            newButtons.Add(btn);
        }

        // Give layout a frame or two, then enable scroll
        yield return new WaitForEndOfFrame();
        if (choicesScrollRect) choicesScrollRect.gameObject.SetActive(true);
    }

    public void SetStatsButtonVisibility(bool isVisible)
    {
        if (statsButton != null)
        {
            statsButton.gameObject.SetActive(isVisible);
        }
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