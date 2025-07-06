// WorldStateObject.cs
using UnityEngine;

/// <summary>
/// A component to be placed on any GameObject in the scene that should change its state
/// based on a saved global flag in the WorldDataManager.
/// It checks the flag on Start() to ensure the object's state is correct when a scene is loaded.
/// </summary>
public class WorldStateObject : MonoBehaviour
{
    [Header("State Control")]
    [Tooltip("The key of the global flag in WorldDataManager that controls this object.")]
    public string controllingFlagKey;

    [Tooltip("The GameObject to affect. If null, this component's GameObject is used.")]
    public GameObject targetObject;

    public enum InitialStateAction
    {
        DoNothing,
        SetActive,
        SetInactive
    }

    [Header("Behavior")]
    [Tooltip("What should happen to the Target Object if the flag is TRUE?")]
    public InitialStateAction actionOnFlagTrue = InitialStateAction.SetInactive;

    [Tooltip("What should happen to the Target Object if the flag is FALSE?")]
    public InitialStateAction actionOnFlagFalse = InitialStateAction.SetActive;


    void Start()
    {
        if (string.IsNullOrEmpty(controllingFlagKey))
        {
            Debug.LogWarning($"[WorldStateObject] on '{gameObject.name}' has no 'Controlling Flag Key' set.", this);
            return;
        }

        if (WorldDataManager.Instance == null)
        {
            Debug.LogError($"[WorldStateObject] on '{gameObject.name}' cannot find WorldDataManager.Instance. Is it in the scene and persistent?", this);
            return;
        }

        // Use this GameObject if no specific target is assigned
        if (targetObject == null)
        {
            targetObject = this.gameObject;
        }

        ApplyState();
    }

    /// <summary>
    /// Reads the controlling flag and applies the defined behavior to the target object.
    /// </summary>
    public void ApplyState()
    {
        bool flagValue = WorldDataManager.Instance.GetGlobalFlag(controllingFlagKey);

        Debug.Log($"[WorldStateObject] '{gameObject.name}' checking flag '{controllingFlagKey}'. Value is '{flagValue}'. Applying state.");

        InitialStateAction actionToPerform = flagValue ? actionOnFlagTrue : actionOnFlagFalse;

        switch (actionToPerform)
        {
            case InitialStateAction.SetActive:
                targetObject.SetActive(true);
                break;
            case InitialStateAction.SetInactive:
                targetObject.SetActive(false);
                break;
            case InitialStateAction.DoNothing:
                // Intentionally do nothing
                break;
        }
    }
}