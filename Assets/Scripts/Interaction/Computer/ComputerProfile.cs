// ComputerProfile.cs
using UnityEngine;
using System.Collections.Generic; // Required for using Lists

[CreateAssetMenu(fileName = "New Computer Profile", menuName = "Computer/New Computer Profile")]
public class ComputerProfile : ScriptableObject
{
    [Header("Owner & Appearance")]
    [Tooltip("The name of the computer's owner. Displayed on the login screen.")]
    public string ownerName = "Public Terminal";

    [Tooltip("The owner's profile picture. Displayed on the login screen.")]
    public Sprite profilePicture;

    [Tooltip("The background wallpaper for this computer terminal.")]
    public Sprite backgroundImage;

    [Header("Security")]
    [Tooltip("If checked, the player must enter a password to access this computer.")]
    public bool requiresPassword;

    [Tooltip("The password required to unlock the computer. Only used if 'Requires Password' is checked.")]
    public string password;

    [Header("Content")]
    [Tooltip("The text that appears for the old 'system info' button. Can be repurposed or removed.")]
    [TextArea(5, 15)]
    public string systemInfoText; // We can keep this for other potential uses

    [Tooltip("A list of notes found on this computer.")]
    public List<NoteEntry> notes = new List<NoteEntry>();

    [Tooltip("A gallery of images found on this computer.")]
    public List<Sprite> imageGallery = new List<Sprite>();
}