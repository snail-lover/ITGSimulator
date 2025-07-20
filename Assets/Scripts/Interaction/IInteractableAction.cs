// --- START OF FILE IInteractableAction.cs ---

/// <summary>
/// An interface for any component that performs an action when an Interactable is successfully triggered.
/// This allows for modular, reusable actions (e.g., picking up an item, starting dialogue, opening a door).
/// </summary>
public interface IInteractableAction
{
    /// <summary>
    /// Executes the core logic of this action. This is called by the Interactable script
    /// once the player is in range.
    /// </summary>
    void ExecuteAction();

    /// <summary>
    /// Called by the Interactable if the interaction is cancelled before completion.
    /// Used for optional cleanup (e.g., un-highlighting a button).
    /// </summary>
    void ResetAction();
}