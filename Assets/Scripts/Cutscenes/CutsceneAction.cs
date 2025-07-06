using UnityEngine;
using System.Collections.Generic; 

public enum CutsceneActionType
{
    Wait,                   // Simple delay
    MoveCharacter,          // Move an NPC or Player
    TeleportCharacter,      // Teleport an NPC or Player to a position
    PlayAnimation,          // Play an animation on a character
    ShowDialogueSegment,    // Display a line/segment of dialogue via DialogueManager
    FaceDirection,          // Make a character face a target or direction
    CameraControl,          // (Future) Move camera, switch virtual cameras
    ActivateGameObject,     // Activate/Deactivate a GameObject
    CustomNPCMethod,        // Call a specific method on a BaseNPC
    ChangeScene,            // Load a new scene
    SetWorldState,
    // Add more as needed: PlaySound, FadeScreen, etc.
}

[System.Serializable]
public class CutsceneAction
{
    // === General ===
    public string actionName = "New Action";
    public CutsceneActionType type;

    // === Targeting ===
    [Header("Targeting")]
    [Tooltip("The GameObject name of the NPC in the scene, or a unique ID you assign.")]
    public string targetNPCIdentifier;
    [Tooltip("If true, this action targets the player character, ignoring targetNPCIdentifier.")]
    public bool targetIsPlayer = false;

    public Transform targetTransform; // Be careful if this is a scene object!
    public string targetTransformMarkerName; // Alternative: use name of a marker GameObject

    public Vector3 targetPosition;
    public bool useTargetTransformForPosition = true;

    // === Camera Control ===
    [Header("Camera Control Parameters")]
    [Tooltip("The GameObject name of the Camera to control. If empty, defaults to Camera.main.")]
    public string cameraIdentifier;
    [Tooltip("Move the camera to this Transform's position and rotation. Takes precedence over targetPosition/targetRotation if assigned via marker name.")]
    public string cameraTargetMarkerName; // To use a scene marker for camera position/rotation
    public Vector3 cameraTargetPosition;
    public Quaternion cameraTargetRotation = Quaternion.identity; // Or Euler angles if you prefer
    public float cameraMoveDuration = 1.0f; // Duration for smooth movement
    public bool cameraWaitForMoveCompletion = true;

    // === Scene Change ===
    [Header("Scene Change Parameters")]
    [Tooltip("The name of the scene to load (must be in Build Settings).")]
    public string sceneNameToLoad;

    // === World State ===
    [Header("World State Parameters")]
    [Tooltip("The key of the global flag to set in WorldDataManager.")]
    public string worldStateKey;
    [Tooltip("The value to set the flag to.")]
    public bool worldStateValue = true;

    // === Type-Specific / Miscellaneous ===
    [Header("Type-Specific Parameters")]
    public float duration;
    public string stringParameter;
    public TextAsset dialogueFile;

    // === GameObject Activation ===
    public GameObject gameObjectToToggle; // If this is a scene object, it also has the same issue.
    public string gameObjectToToggleName; // Alternative for scene GameObjects
    public bool activateState;

    // === Control ===
    public bool waitForCompletion = true;
}