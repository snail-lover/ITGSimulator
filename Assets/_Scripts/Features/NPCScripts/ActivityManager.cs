// --- START OF FILE ActivityManager.cs ---

using System.Collections.Generic;
using UnityEngine;

// The class is now named ActivityManager.
public class ActivityManager : MonoBehaviour
{
    public static ActivityManager Instance { get; private set; }

    // The registry now stores ActivityObjects, not TaskObjects.
    private Dictionary<string, ActivityObject> _activityRegistry = new Dictionary<string, ActivityObject>();

    // NEW & IMPORTANT: A public, read-only property for NPCs to get all available activities.
    // This is the efficient alternative to FindObjectsOfType<ActivityObject>().
    public IEnumerable<ActivityObject> AllRegisteredActivities => _activityRegistry.Values;


    private void Awake()
    {
        // Standard Singleton pattern (no changes needed here)
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
    /// Called by ActivityObject instances to add themselves to the registry.
    /// Renamed from RegisterTask and now accepts an ActivityObject.
    /// </summary>
    public void RegisterActivity(ActivityObject activity) // <-- Changed parameter type
    {
        if (activity == null || string.IsNullOrEmpty(activity.activityID)) // <-- Uses activityID
        {
            Debug.LogError("Attempted to register an activity with a null or empty ID.", activity);
            return;
        }

        if (_activityRegistry.ContainsKey(activity.activityID))
        {
            Debug.LogWarning($"An activity with ID '{activity.activityID}' is already registered. Overwriting with new instance. Ensure activity IDs are unique.", activity);
            _activityRegistry[activity.activityID] = activity;
        }
        else
        {
            _activityRegistry.Add(activity.activityID, activity);
            // Debug.Log($"Activity registered: {activity.activityID}");
        }
    }

    /// <summary>
    /// Retrieves an activity object from the scene by its unique ID.
    /// Renamed from GetTaskByID.
    /// </summary>
    public ActivityObject GetActivityByID(string id) // <-- Changed return type
    {
        if (string.IsNullOrEmpty(id) || !_activityRegistry.ContainsKey(id))
        {
            // This is not necessarily an error anymore. An NPC might look for an activity that doesn't exist.
            // A warning or null return is often sufficient.
            // Debug.LogWarning($"ActivityManager: No activity found with ID '{id}'.");
            return null;
        }
        return _activityRegistry[id];
    }
    /// <summary>
    /// Removes an activity from the registry. Called when an object is destroyed or picked up.
    /// </summary>
    public void UnregisterActivity(ActivityObject activity)
    {
        if (activity == null || string.IsNullOrEmpty(activity.activityID)) return;

        if (_activityRegistry.ContainsKey(activity.activityID))
        {
            _activityRegistry.Remove(activity.activityID);
            // Debug.Log($"Activity unregistered: {activity.activityID}");
        }
    }
}