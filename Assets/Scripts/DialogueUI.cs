using UnityEngine;
using UnityEngine.UI; // For Text, Button
// using TMPro; // If using TextMeshPro

public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;   // The parent panel for the dialogue UI
    public Text dialogueText;          // If using TMPro, change to TMP_Text
    public Transform choicesContainer; // Where choice buttons go
    public Button choiceButtonPrefab;  // Prefab of a button for each choice

    // Current references
    private DialogueManager dialogueManager;
    
    private void Start()
    {
        // Hide on scene start if you like
        HideDialogue();
    }

    public void ShowDialogue(DialogueManager manager, DialogueNode node)
    {
        dialogueManager = manager;
        dialoguePanel.SetActive(true);

    // Clear previous choices
    foreach (Transform child in choicesContainer)
    {
        Destroy(child.gameObject);
    }

    dialogueText.text = node.text;

    foreach (var choice in node.choices)
    {
        Button choiceButton = Instantiate(choiceButtonPrefab, choicesContainer);
        Text buttonText = choiceButton.GetComponentInChildren<Text>();
        buttonText.text = choice.choiceText;

        bool isAvailable = dialogueManager.IsChoiceAvailable(choice);
        choiceButton.interactable = isAvailable; // Disable button if choice is locked

        choiceButton.onClick.AddListener(() =>
        {
            if (isAvailable)
            {
                dialogueManager.HandleChoiceSelected(choice);
            }
        });
    }
}


    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
    }
}
