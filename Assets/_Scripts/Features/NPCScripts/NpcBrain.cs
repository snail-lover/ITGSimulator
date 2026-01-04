// NpcBrain.cs (Refactored as an API for Behavior Designer)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

/// <summary>
/// This class is the "API" or "Toolbox" for an NPC's autonomous behavior,
/// designed to be used by Behavior Designer tasks. It holds references to all
/// necessary NPC components and data, and provides utility methods for tasks to call.
/// It no longer contains a state machine or decision-making logic, as that is
/// now handled by the BehaviorTree component.
/// </summary>
public class NpcBrain : MonoBehaviour
{
    // --- PUBLIC COMPONENT & DATA ACCESSORS ---
    // Behavior Designer tasks will get a reference to this NpcBrain,
    // and then use these public properties to access the rest of the NPC.
    public NpcController Controller { get; private set; }
    public NpcConfig Config { get; private set; }
    public NpcRuntimeData RuntimeData { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    public Animator NpcAnimator { get; private set; }
    public NpcPerception Perception { get; private set; }

    public HashSet<ActivityObject> VisibleActivities { get; private set; } = new HashSet<ActivityObject>();

    // --- AI TUNING (Can be read by custom tasks) ---
    [Header("AI Tuning")]
    public float desperationThreshold = 50f;
    public float searchFailureCooldown = 30f;
    public LayerMask wallLayerMask;
    public float searchMeanderWidth = 5f;
    public float searchMeanderDistance = 8f;
    public float followStoppingDistance = 1.5f;

    private bool isInitialized = false;

    /// <summary>
    /// Called by NpcController to give the Brain all the references it needs.
    /// </summary>
    public void Initialize(NpcController controller)
    {
        // --- Cache all references from the Controller ---
        Controller = controller;
        Config = controller.npcConfig;
        RuntimeData = controller.runtimeData;
        Agent = controller.Agent;
        NpcAnimator = controller.NpcAnimator;
        Perception = controller.Perception;

        // --- Validation ---
        if (Config == null || RuntimeData == null || Agent == null || NpcAnimator == null || Perception == null)
        {
            Debug.LogError($"[{gameObject.name}] NpcBrain initialization failed. One or more critical components are missing. Disabling brain.", this);
            this.enabled = false;
            return;
        }
        if (RuntimeData.needs == null)
        {
            Debug.LogError($"[{gameObject.name}] NpcBrain initialization failed. The provided NpcRuntimeData has a null 'needs' collection. Disabling brain.", this);
            this.enabled = false;
        }

        isInitialized = true;
        Debug.Log($"[{gameObject.name}] NpcBrain initialized successfully.");

    }

    private void Update()
    {

        // If the brain isn't ready, do nothing.
        if (!isInitialized) return;

        // Needs still need to decay over time, regardless of behavior.
        // This is a good place for global, continuous logic.
        foreach (var need in RuntimeData.needs.Values)
        {
            need.UpdateNeed(Time.deltaTime);
        }

#if UNITY_EDITOR
        // --- ADD THIS DEBUGGING LINE ---
        // This will print the name of the dragged object the brain sees, or "null".
        //Debug.Log($"[NpcBrain Monitor] VisibleDraggedObject: {(VisibleDraggedObject != null ? VisibleDraggedObject.name : "null")}");
#endif
    }

    #region Public Methods for External Systems

    // These methods are called by other systems (like Perception or Dialogue)
    // to inform the NPC of world events.

    /// <summary>
    /// Clears the short-term memory of currently visible objects.
    /// Called by NpcPerception at the start of its update loop.
    /// </summary>
    public void ClearCurrentPerception()
    {
        VisibleActivities.Clear();
    }


    /// <summary>
    /// The core learning method. Called by NpcPerception when a new activity is seen.
    /// This logic is preserved as it modifies the NPC's core data.
    /// </summary>
    public void LearnAboutActivity(ActivityObject activity)
    {
        // --- MODIFIED ---
        // Create a new memory entry with the current time.
        var newMemory = new NpcMemoryEntry
        {
            position = activity.GetTargetPosition(),
            lastUpdatedTime = Time.time
        };

        if (RuntimeData.rememberedActivityLocations.ContainsKey(activity.activityID))
        {
            RuntimeData.rememberedActivityLocations[activity.activityID] = newMemory;
        }
        else
        {
            RuntimeData.rememberedActivityLocations.Add(activity.activityID, newMemory);
        }

        SearchableZone zone = GetZoneForPosition(activity.transform.position);
        if (zone != null)
        {
            if (!RuntimeData.learnedZoneContents.ContainsKey(zone.zoneName))
            {
                RuntimeData.learnedZoneContents.Add(zone.zoneName, new HashSet<string>());
            }
            foreach (var effect in activity.needEffects)
            {
                if (effect.effectValue < 0)
                    RuntimeData.learnedZoneContents[zone.zoneName].Add(effect.needName);
            }
            foreach (var tag in activity.activityTags)
            {
                RuntimeData.learnedZoneContents[zone.zoneName].Add(tag);
            }
        }
    }
    #endregion

    #region Public Utility Methods for Behavior Tree Tasks

    // Your custom BT Tasks will call these methods to make decisions or get information.

    public bool DoesActivityMatchGoals(ActivityObject activity)
    {
        if (activity.activityTags == null || activity.activityTags.Count == 0 || Config.personalityGoals == null) return false;

        foreach (var goal in Config.personalityGoals)
        {
            foreach (var goalTag in goal.associatedTags)
            {
                if (activity.activityTags.Contains(goalTag))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public SearchableZone GetZoneForPosition(Vector3 position)
    {
        foreach (var zone in SearchableZone.AllZones)
        {
            if (zone.GetComponent<Collider>().bounds.Contains(position))
            {
                return zone;
            }
        }
        return null;
    }

    public void StopMovement()
    {
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.ResetPath();
        }
    }

    #endregion
}