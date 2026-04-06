using System;
using UnityEngine;

public static class DialogueEvents
{
    public static event Action<Transform> OnDialogueStarted;
    public static event Action OnDialogueEnded;

    public static void RaiseDialogueStarted(Transform npcTransform) => OnDialogueStarted?.Invoke(npcTransform);
    public static void RaiseDialogueEnded() => OnDialogueEnded?.Invoke();
}