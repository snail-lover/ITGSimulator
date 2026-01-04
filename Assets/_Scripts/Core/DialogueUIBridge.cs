using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public sealed class DialogueViewChoice
    {
        public string text;
        public Sprite icon;
        public Action onClick;
    }

    public sealed class DialogueViewNode
    {
        public string speakerName;
        public Sprite portrait;
        public string text;
        public bool isGreeting;
        public List<DialogueViewChoice> choices = new List<DialogueViewChoice>();
    }

    public interface IDialogueUIRoot
    {
        void ShowDialogue(DialogueViewNode view);
        void UpdateDialogueContent(DialogueViewNode view);
        void HideDialogue(Action onHidden = null);
        void SetStatsButtonVisibility(bool visible);
    }
}
