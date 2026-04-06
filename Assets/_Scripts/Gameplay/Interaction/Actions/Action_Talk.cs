using UnityEngine;
using Yarn.Unity;
using Game.Core;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Actions/Action_Talk")]
    public class Action_Talk : InteractionAction
    {
        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            if (IsBlocked(target)) return null;

            // Need an identity to look up dialogue
            NPCIdentity identity = target.GetComponent<NPCIdentity>();
            if (identity == null || string.IsNullOrEmpty(identity.npcID))
            {
                Debug.LogWarning($"[Action_Talk] {target.name} has no NPCIdentity or npcID is empty.");
                return null;
            }

            return new InteractionOption
            {
                Label = actionName,
                Priority = priority,
                ActionToRun = () => Execute(target, identity.npcID)
            };
        }

        private void Execute(SmartObject target, string npcID)
        {
            if (DialogueService.Instance == null)
            {
                Debug.LogWarning("[Action_Talk] No DialogueService found in scene.");
                return;
            }

            DialogueRunner runner = Object.FindFirstObjectByType<DialogueRunner>();
            if (runner == null)
            {
                Debug.LogWarning("[Action_Talk] No DialogueRunner found in scene.");
                return;
            }

            if (runner.IsDialogueRunning) return;

            string nodeName = DialogueService.Instance.GetNodeForNPC(npcID);
            if (string.IsNullOrEmpty(nodeName))
            {
                Debug.LogWarning($"[Action_Talk] No dialogue found for NPC '{npcID}'.");
                return;
            }

            DialogueEvents.RaiseDialogueStarted(target.transform);
            _ = runner.StartDialogue(nodeName);
        }
    }
}