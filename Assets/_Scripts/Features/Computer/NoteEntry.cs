// NoteEntry.cs
using UnityEngine;

[System.Serializable] // This makes it visible and editable in the Unity Inspector
public class NoteEntry
{
    public string title;
    [TextArea(10, 20)] // Makes the content field a large, easy-to-use text box
    public string content;
}