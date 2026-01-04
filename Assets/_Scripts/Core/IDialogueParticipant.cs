// IDialogueParticipant.cs
using UnityEngine;

/// <summary>
/// A contract for any object that can participate in a dialogue.
/// This allows the DialogueManager to be generic and not know about NPCs specifically.
/// </summary>
public interface IDialogueParticipant
{
    // The data file containing the conversation tree.
    TextAsset GetDialogueDataFile();

    // The name to display in the dialogue UI.
    string GetParticipantName();

    // The portrait to display in the dialogue UI.
    Sprite GetParticipantPortrait();

    // A method for the DialogueManager to call when the conversation is over.
    void OnDialogueEnded();
}