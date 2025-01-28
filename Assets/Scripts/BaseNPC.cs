using System;
using System.Collections;                // For IEnumerator and Coroutines
using System.Collections.Generic;        // For List<>
using System.Threading.Tasks;
using UnityEngine;                       // For Unity-specific functionality
using UnityEngine.AI;                    // For NavMeshAgent

public class BaseNPC : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 2.0f;    // Interaction range in Unity units
    private Transform player;            // Reference to the player's transform
    private NavMeshAgent playerAgent;    // Reference to the player's NavMeshAgent
    private bool isInteracting = false;  // Prevent multiple interactions
    private static BaseNPC currentTarget; // Track the currently targeted NPC

    [Header("Task Management")]
    public List<TaskObject> taskPool;    // List of tasks the NPC can perform
    public float taskDelay = 2.0f;       // Delay between tasks (seconds)

    [Header("NPC Movement")]
    private NavMeshAgent agent;          // Reference to the NPC's NavMeshAgent
    private TaskObject currentTask;      // The NPC's current task

    private void Start()
    {
        // Find the player in the scene (ensure the player is tagged "Player")
        player = GameObject.FindWithTag("Player").transform;
        playerAgent = player.GetComponent<NavMeshAgent>();

        // Initialize the NPC's NavMeshAgent
        agent = GetComponent<NavMeshAgent>();

        // Assign the first task
        AssignNextTask();
   
    }

    public void Update()
    {
        //Debug.Log("BaseNPC: " + taskDelay);
    }

    public virtual void Interact()
    {
        if (isInteracting) return; // Prevent multiple interactions
        isInteracting = true;

        // Set this NPC as the current target
        currentTarget = this;

        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= interactRange)
        {
            // Player is already in range, start dialogue
            StartDialogue();
        }
        else
        {
            // Move the player closer
            Debug.Log($"Moving player towards {name}...");
            playerAgent.SetDestination(transform.position);
            StartCoroutine(WaitForPlayerArrival());
        }
    }

    private IEnumerator WaitForPlayerArrival()
    {
        // Wait until the player is within interaction range or the target is cleared
        while (currentTarget == this && Vector3.Distance(player.position, transform.position) > interactRange)
        {
            yield return null;
        }

        // If the current target is no longer this NPC, cancel interaction
        if (currentTarget != this)
        {
            Debug.Log($"Interaction with {name} was canceled.");
            isInteracting = false;
            yield break;
        }

        // Stop player movement and start dialogue
        playerAgent.ResetPath(); // Stops the player from moving
        StartDialogue();
    }

    private void StartDialogue()
    {
        Debug.Log($"Starting dialogue with {name}");
        BaseDialogue.Instance.StartDialoguePlaceholder(this);
        isInteracting = false;

        // Clear the current target after interaction starts
        currentTarget = null;
    }

    // Static method to clear the current target (e.g., when clicking elsewhere)
    public static void ClearCurrentTarget()
    {
        if (currentTarget != null)
        {
            Debug.Log($"Clearing target: {currentTarget.name}");
            currentTarget.isInteracting = false;
            currentTarget = null;
        }
    }

    private void AssignNextTask()
    {
    // Select a random task from the task pool
    currentTask = GetRandomTask();

      if (currentTask != null)
      {
        Debug.Log($"{name} is starting task: {currentTask.taskName}");
        MoveToTask(currentTask);
      }
      else
      {
               Debug.LogWarning($"{name} has no tasks to perform.");
      }
    }

    private TaskObject GetRandomTask()
    {
        // Ensure there are always tasks to choose from
     if (taskPool.Count > 0)
        {
         // Pick a random task from the pool
         int randomIndex = UnityEngine.Random.Range(0, taskPool.Count);
         return taskPool[randomIndex];
      }

      Debug.LogWarning("Task pool is empty! NPC cannot assign new tasks.");
      return null;
    }

    private void MoveToTask(TaskObject task)
    {
        // Move the NPC to the task's location
        agent.SetDestination(task.transform.position);

        // Start coroutine to wait for the NPC to arrive
        StartCoroutine(PerformTaskWhenArrived(task));
    }

    private IEnumerator PerformTaskWhenArrived(TaskObject task)
    {
        // Wait until the NPC has "arrived" at the task location
        while (agent.remainingDistance > agent.stoppingDistance)
     {
          yield return null;
      }

      // Debugging: Confirm arrival
      Debug.Log($"Arrived at task {task.taskName}");

       // Perform the task
       PerformTask(task);
    }

    private void PerformTask(TaskObject task)
    {
        Debug.Log($"{name} is performing task: {task.taskName} with action: {task.action}");

        // Trigger the task's action
        task.PerformTask();

        // Wait for the task's duration before moving to the next task
        StartCoroutine(WaitAndAssignNextTask(task.duration + taskDelay));
    }

    private IEnumerator WaitAndAssignNextTask(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        AssignNextTask();
    }
}
