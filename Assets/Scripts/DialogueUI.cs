using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System; 

public class DialogueUI : MonoBehaviour
{
    [Header("Core UI References")]
    public CanvasGroup dialogueSystemCanvasGroup;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public Image characterPortraitImage;

    [Header("Choice References")]
    public Transform choicesContainer;
    public Button choiceButtonPrefab;

    [Header("Special Action Buttons")] 
    [SerializeField] private Button finalCutsceneButton; // Assign this in the Inspector

    [Header("Animation/Fading")]
    public float fadeDuration = 0.3f;
    private Coroutine fadeCoroutine;

    private DialogueManager dialogueManager;

    void Awake()
    {
        // Ensure UI is hidden initially via the Canvas Group
        if (dialogueSystemCanvasGroup != null)
        {
            dialogueSystemCanvasGroup.alpha = 0f;
            dialogueSystemCanvasGroup.interactable = false;
            dialogueSystemCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            Debug.LogError("[DialogueUI] DialogueSystem_Container (with CanvasGroup) reference not set in Inspector!");
        }

        // Validate prefab has TMP component
        if (choiceButtonPrefab == null || choiceButtonPrefab.GetComponentInChildren<TextMeshProUGUI>() == null)
        {
            Debug.LogError("[DialogueUI] Choice Button Prefab is missing or does not have a TextMeshProUGUI child!");
        }

        // Validate other essential references
        if (speakerNameText == null) Debug.LogError("[DialogueUI] SpeakerName_TMP reference not set!");
        if (dialogueText == null) Debug.LogError("[DialogueUI] DialogueText_TMP reference not set!");
        if (characterPortraitImage == null) Debug.LogError("[DialogueUI] CharacterPortrait_Image reference not set!");
        if (choicesContainer == null) Debug.LogError("[DialogueUI] Choices_Container reference not set!");

        // Validate and initialize Final Cutscene Button
        if (finalCutsceneButton == null)
        {
            Debug.LogWarning("[DialogueUI] Final Cutscene Button reference not set in Inspector! This feature will be disabled.");
        }
        else
        {
            finalCutsceneButton.gameObject.SetActive(false); // Hide it by default
        }
    }

    public void ShowDialogue(DialogueManager manager, DialogueNode node, string speakerName, Sprite portraitSprite)
    {
        dialogueManager = manager;

        if (dialogueSystemCanvasGroup == null || node == null)
        {
            Debug.LogError("[DialogueUI] Cannot show dialogue - CanvasGroup or Node is null.");
            return; 
        }

        Debug.Log($"[DialogueUI] Showing Node: {node.nodeID}");


        speakerNameText.text = speakerName;
        dialogueText.text = node.text;

        // Update Portrait
        if (characterPortraitImage != null)
        {
            if (portraitSprite != null)
            {
                characterPortraitImage.sprite = portraitSprite;
                characterPortraitImage.enabled = true;
                Debug.Log($"[DialogueUI] Set portrait for {speakerName}");
            }
            else
            {
                characterPortraitImage.enabled = false;
                Debug.LogWarning($"[DialogueUI] No portrait sprite provided for {speakerName}");
            }
        }

        foreach (Transform child in choicesContainer)
        {
            Destroy(child.gameObject);
        }

        Debug.Log($"[DialogueUI] Populating {node.choices.Length} choices.");
        if (node.choices != null)
        {
            foreach (var choice in node.choices)
            {
                if (choice == null)
                {
                    Debug.LogWarning($"[DialogueUI] Encountered a null choice in node {node.nodeID}");
                    continue;
                }

                Button choiceButtonInstance = Instantiate(choiceButtonPrefab, choicesContainer);
                TextMeshProUGUI buttonText = choiceButtonInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = choice.choiceText;
                }
                else
                {
                    Debug.LogError($"[DialogueUI] Choice button instance created from prefab '{choiceButtonPrefab.name}' is missing TextMeshProUGUI child!", choiceButtonInstance);
                    buttonText.text = "MISSING TEXT";
                }

                bool isAvailable = dialogueManager.IsChoiceAvailable(choice);
                choiceButtonInstance.interactable = isAvailable;

                DialogueChoice currentChoice = choice;
                choiceButtonInstance.onClick.AddListener(() =>
                {
                    Debug.Log($"[DialogueUI] Choice clicked: {currentChoice.choiceText}");
                    dialogueManager.HandleChoiceSelected(currentChoice);
                });
            }
        }

        StartCoroutine(RebuildChoiceLayoutAfterFrame());
        ShowPanel();
    }

    private IEnumerator RebuildChoiceLayoutAfterFrame()
    {
        yield return null; 

        if (choicesContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(choicesContainer as RectTransform);
            Debug.Log("[DialogueUI] Forced layout rebuild on Choices Container (after frame delay).");
        }
    }

    public void HideDialogue()
    {
        Debug.Log("[DialogueUI] HideDialogue called.");
        if (dialogueSystemCanvasGroup == null) return;
        SetFinalCutsceneButtonVisibility(false, null); // Ensure it's hidden when dialogue UI hides
        HidePanel();
    }

    public void SetFinalCutsceneButtonVisibility(bool isVisible, Action onClickCallback)
    {
        if (finalCutsceneButton == null) return; // Not set up

        finalCutsceneButton.gameObject.SetActive(isVisible);
        if (isVisible)
        {
            finalCutsceneButton.onClick.RemoveAllListeners(); // Clear previous listeners
            if (onClickCallback != null)
            {
                finalCutsceneButton.onClick.AddListener(() => onClickCallback());
            }
            Debug.Log("[DialogueUI] Final Cutscene Button is now VISIBLE.");
        }
        else
        {
            finalCutsceneButton.onClick.RemoveAllListeners();
            Debug.Log("[DialogueUI] Final Cutscene Button is now HIDDEN.");
        }
    }


    private void ShowPanel()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        dialogueSystemCanvasGroup.interactable = true;
        dialogueSystemCanvasGroup.blocksRaycasts = true;
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(dialogueSystemCanvasGroup, dialogueSystemCanvasGroup.alpha, 1f, fadeDuration));
        Debug.Log("[DialogueUI] Starting fade IN.");
    }

    private void HidePanel()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        dialogueSystemCanvasGroup.interactable = false;
        dialogueSystemCanvasGroup.blocksRaycasts = false;
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(dialogueSystemCanvasGroup, dialogueSystemCanvasGroup.alpha, 0f, fadeDuration));
        Debug.Log("[DialogueUI] Starting fade OUT.");
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float targetAlpha, float duration)
    {
        float time = 0f;
        if (duration <= 0) duration = 0.01f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }
        cg.alpha = targetAlpha;
        fadeCoroutine = null;
        Debug.Log($"[DialogueUI] Fade finished. Alpha: {cg.alpha}");
    }
}