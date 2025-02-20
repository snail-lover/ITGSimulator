using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BaseNPC : MonoBehaviour, IClickable
{
    [Header("Interaction Settings")]
    public float interactRange = 2.0f;    // Interaction range in Unity units
    private Transform player;             // Reference to the player's transform
    private NavMeshAgent playerAgent;     // Reference to the player's NavMeshAgent
    public bool isInteracting = false;   // Prevent multiple interactions
    public bool isTalking = false;       // Tracks if the player is talking to the NPC
    public static BaseNPC currentTarget; // Track the currently targeted NPC

    [Header("Task Management")]
    public List<TaskObject> taskPool;      // List of tasks the NPC can perform
    public float taskDelay = 2.0f;         // Delay between tasks (seconds)

    [Header("NPC Movement")]
    private NavMeshAgent agent;            // Reference to the NPC's NavMeshAgent
    private TaskObject currentTask;        // The NPC's current task
    private Coroutine taskCoroutine;       // Reference to the task coroutine

    [Header("NPC Info")]
    public string npcName = "NPC Name";
    public TextAsset dialogueFile;
    public int currentLove;
    public int npcAge;
    public string npcGender;
    public string npcBloodType;
    public string npcZodiacSign;
    public int npcCockLength;
    public string npcLikes;
    public string npcDislikes;
    private DialogueData dialogueData;
    private Inventory inventory;
    
    public DialogueData GetDialogueData() => dialogueData;

    void Awake() 
    {
        Debug.Log("Loaded JSON: " + dialogueFile.text);
        dialogueData = JsonUtility.FromJson<DialogueData>(dialogueFile.text);
        dialogueData.nodeDictionary = new Dictionary<string, DialogueNode>();
        foreach (var node in dialogueData.nodes) {
            if (node.itemGate != null && node.itemGate.requiredItem == null) {
                node.itemGate = null;
            }
        }
    
        foreach (var node in dialogueData.nodes)
        {
            dialogueData.nodeDictionary.Add(node.nodeID, node);
            Debug.Log($"Added node: {node.nodeID}"); // Verify nodes are added
        }
    
        currentLove = dialogueData.startingLove;
    }

    private void Start()
    {
        // Find the player in the scene
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerAgent = player.GetComponent<NavMeshAgent>();
        }
        else
        {
            Debug.LogError("Player not found. Ensure the player is tagged 'Player'.");
        }

        // Initialize the NPC's NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component missing from NPC.");
        }

        // Start the task loop coroutine
        taskCoroutine = StartCoroutine(TaskLoop());
    }

    private void Update()
    {
    
    }

    public string GetStats()
    {
        return $"Name: {npcName}\n" +
               $"Age: {npcAge}\n" +
               $"Gender: {npcGender}\n" +
               $"Blood Type: {npcBloodType}\n" +
               $"Zodiac Sign: {npcZodiacSign}\n" +
               $"Cock Length: {npcCockLength}\n" +
               $"Likes: {npcLikes}\n" +
               $"Dislikes: {npcDislikes}\n" +
               $"Current Love: {currentLove}";
    }

    public void OnClick()
    {
        if (PointAndClickMovement.currentTarget != null && (object)PointAndClickMovement.currentTarget != this)
        {
            PointAndClickMovement.currentTarget.ResetInteractionState();
        }

        PointAndClickMovement.currentTarget = this;
        Interact();
    }

    public void ResetInteractionState()
    {
        isInteracting = false;
        isTalking = false;
        StopAllCoroutines();
        
        // Resume normal NPC behavior
        if (taskCoroutine == null)
        {
            taskCoroutine = StartCoroutine(TaskLoop());
        }

        // Notify PointAndClickMovement that interaction has ended
        if ((object)PointAndClickMovement.currentTarget == this)
        {
            PointAndClickMovement.currentTarget = null;
            Object.FindAnyObjectByType<PointAndClickMovement>().EndInteraction();
        }
    }

    // <summary>
    /// Initiates interaction with the NPC.
    /// </summary>
    public virtual void Interact()
    {
        if (isInteracting || isTalking) return; // Prevent multiple interactions
        isInteracting = true;
        isTalking = true;

        // Set this NPC as the current target
        currentTarget = this;

        // Start following the NPC until within interactRange
        StartCoroutine(FollowPlayerToNPC());
    }

    /// <summary>
    /// Coroutine that moves the player towards the NPC's current position.
    /// Continuously updates the destination to handle moving NPCs.
    /// </summary>
    private IEnumerator FollowPlayerToNPC()
    {
        while (currentTarget == this && Vector3.Distance(player.position, transform.position) > interactRange)
        {
            playerAgent.SetDestination(transform.position);

            // Wait for a short interval before updating destination again
            yield return new WaitForSeconds(0.5f);
        }

        if (currentTarget != this)
        {
            //Debug.Log($"Interaction with {name} was canceled.");
            isInteracting = false;
            isTalking = false;
            yield break;
        }

        // Stop player movement and start dialogue
        playerAgent.ResetPath(); 
        StopNPC();
        StartDialogue();
    }

    /// <summary>
    /// Stops the NPC's movement and pauses task execution.
    /// </summary>
    private void StopNPC()
    {
        agent.isStopped = true;
        //Debug.Log($"{name} has stopped for conversation.");

        // Pause task execution by stopping the task coroutine
        if (taskCoroutine != null)
        {
            StopCoroutine(taskCoroutine);
            taskCoroutine = null;
        }
    }

    /// <summary>
    /// Initiates the dialogue using the BaseDialogue system.
    /// </summary>
    private void StartDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(this);
        }
        else
        {
            Debug.LogError("DialogueManager instance not found in the scene.");
        }
    }

    /// <summary>
    /// Clears the current NPC target, typically when the player clicks elsewhere.
    /// </summary>
    public static void ClearCurrentTarget()
    {
        if (currentTarget != null)
        {
            Debug.Log($"Clearing target: {currentTarget.name}");
            currentTarget.isInteracting = false;
            currentTarget.isTalking = false;
            currentTarget = null;
        }
    }

    /// <summary>
    /// Resumes the NPC's tasks after dialogue has ended.
    /// </summary>
    public void ResumeNPC()
    {
        isTalking = false;
        agent.isStopped = false;
        Debug.Log($"{name} has resumed tasks.");

        // Restart the task loop coroutine
        if (taskCoroutine == null)
        {
            taskCoroutine = StartCoroutine(TaskLoop());
        }
    }

    #region Task System

    /// <summary>
    /// Main loop for assigning and performing tasks.
    /// </summary>
    private IEnumerator TaskLoop()
    {
        while (true)
        {
            if (isTalking)
            {
                // Wait until not talking before assigning the next task
                yield return null;
                continue;
            }

            AssignNextTask();

            // Wait until the current task is completed before assigning the next
            while (currentTask != null)
            {
                yield return null;
            }

            // Wait for the task delay before the next task
            yield return new WaitForSeconds(taskDelay);
        }
    }

    /// <summary>
    /// Assigns the next task from the task pool.
    /// </summary>
    private void AssignNextTask()
    {
        currentTask = GetRandomTask();

        if (currentTask != null)
        {
            //Debug.Log($"{name} is starting task: {currentTask.taskName}");
            MoveToTask(currentTask);
        }
        else
        {
            Debug.LogWarning($"{name} has no tasks to perform.");
        }
    }

    /// <summary>
    /// Retrieves a random task from the task pool.
    /// </summary>
    private TaskObject GetRandomTask()
    {
        // Ensure there are tasks available
        if (taskPool.Count > 0)
        {
            // Select a random task
            int randomIndex = UnityEngine.Random.Range(0, taskPool.Count);
            return taskPool[randomIndex];
        }

        Debug.LogWarning("Task pool is empty! NPC cannot assign new tasks.");
        return null;
    }

    /// <summary>
    /// Commands the NPC to move to the specified task location.
    /// </summary>
    private void MoveToTask(TaskObject task)
    {
        if (task == null || task.transform == null)
        {
            Debug.LogError("Invalid task or task location.");
            currentTask = null;
            return;
        }

        // Move the NPC to the task's location
        agent.SetDestination(task.transform.position);

        // Start coroutine to wait for the NPC to arrive
        StartCoroutine(PerformTaskWhenArrived(task));
    }

    /// <summary>
    /// Coroutine that waits until the NPC arrives at the task location before performing the task.
    /// </summary>
    private IEnumerator PerformTaskWhenArrived(TaskObject task)
    {
        // Wait until the NPC has arrived at the task location
        while (agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }

        // Confirm arrival
        //Debug.Log($"Arrived at task {task.taskName}.");

        // Perform the task
        PerformTask(task);
    }

    /// <summary>
    /// Executes the specified task.
    /// </summary>
    private void PerformTask(TaskObject task)
    {
        if (task == null)
        {
            Debug.LogError("Cannot perform a null task.");
            currentTask = null;
            return;
        }

        //Debug.Log($"{name} is performing task: {task.taskName} with action: {task.action}");

        // Trigger the task's action
        task.PerformTask();

        // Indicate that the task has been completed
        currentTask = null;
    }
    #endregion

    public virtual void WhenHovered()
    {

    }

    public virtual void HideHover()
    {

    }
}
