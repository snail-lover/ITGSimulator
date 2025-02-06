using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour 
{
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Button exitButton;
    public GameObject dialoguePanel; 

    private BaseNPC currentNPC;
    private DialogueData currentData;
    private string currentNodeKey;
    private Inventory inventory;
    public static bool IsDialogueActive { get; private set; } = false;



    void Start()
    {
        dialoguePanel.SetActive(false);
        inventory = Object.FindFirstObjectByType<Inventory>(); // Initialize inventory reference
        exitButton.onClick.AddListener(EndDialogue);
        
    }

    public void StartDialogue(BaseNPC npc) 
    {
        Debug.Log("Starting dialogue with " + npc.name);
        dialoguePanel.SetActive(true);
        currentNPC = npc;
        currentData = npc.GetDialogueData();
        currentNodeKey = "start"; // Set first
        Debug.Log($"Current node: {currentNodeKey}"); // Log after assignment
        RefreshUI();
        IsDialogueActive = true;
    }

    void RefreshUI() {

        DialogueNode node = currentData.nodeDictionary[currentNodeKey];
        dialogueText.text = node.text;

        Debug.Log($"Checking itemGate for node {currentNodeKey}. Is null? {node.itemGate == null}");
        // Clear old choices
        foreach (Transform child in choicesContainer) {
            Destroy(child.gameObject);
        }

        // Handle item gates first
       if (node.itemGate != null)  
        {
        Debug.Log("Item gate detected! Skipping choices.");
        HandleItemGate(node.itemGate);
        return;
        }

         // Create choice buttons
        Debug.Log($"Creating {node.choices.Count} choice buttons...");
        foreach (var choice in node.choices) {
        GameObject button = Instantiate(choiceButtonPrefab, choicesContainer);
        Debug.Log($"Instantiated button for choice: {choice.text}");
        button.GetComponentInChildren<TextMeshProUGUI>().text = choice.text;
        button.GetComponent<Button>().onClick.AddListener(() => OnChoiceMade(choice));
    }
    }

    void HandleItemGate(ItemGate gate)
    {
        // Fixes Errors 2 & 4
        bool hasItem = inventory.items.Exists(item => item.id == gate.requiredItem);
        bool hasLove = currentNPC.currentLove >= gate.requiredLove;

        if (hasItem && hasLove)
        {
            currentNPC.currentLove += gate.onSuccess.loveChange;
            // Fixes Errors 3 & 5
            inventory.RemoveItemByID(gate.onSuccess.removeItem);
            currentNodeKey = gate.successNode;
        }
        else
        {
            currentNodeKey = gate.failNode;
        }
    }

    void OnChoiceMade(DialogueChoice choice) {
        currentNPC.currentLove += choice.loveChange;
        currentNPC.currentLove = Mathf.Clamp(currentNPC.currentLove, 0, 10);
        currentNodeKey = choice.targetNode;
        RefreshUI();
    }

       public void EndDialogue()
    {
        IsDialogueActive = false; // Clear when dialogue ends
         dialoguePanel.SetActive(false);
        currentNPC.ResumeNPC();
        
        // Add your cleanup logic here
    }


}
