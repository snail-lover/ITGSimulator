using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    private Dictionary<string, TaskObject> _taskRegistry = new Dictionary<string, TaskObject>();

    private void Awake()
    {
        // Standard Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Called by TaskObject instances to add themselves to the registry.
    /// </summary>
    public void RegisterTask(TaskObject task)
    {
        if (task == null || string.IsNullOrEmpty(task.taskID))
        {
            Debug.LogError("Attempted to register a task with a null or empty ID.", task);
            return;
        }

        if (_taskRegistry.ContainsKey(task.taskID))
        {
            Debug.LogWarning($"A task with ID '{task.taskID}' is already registered. Overwriting with new instance. Ensure task IDs are unique.", task);
            _taskRegistry[task.taskID] = task;
        }
        else
        {
            _taskRegistry.Add(task.taskID, task);
            // Debug.Log($"Task registered: {task.taskID}");
        }
    }

    /// <summary>
    /// Retrieves a task object from the scene by its unique ID.
    /// </summary>
    public TaskObject GetTaskByID(string id)
    {
        if (string.IsNullOrEmpty(id) || !_taskRegistry.ContainsKey(id))
        {
            Debug.LogError($"TaskManager: No task found with ID '{id}'.");
            return null;
        }
        return _taskRegistry[id];
    }
}