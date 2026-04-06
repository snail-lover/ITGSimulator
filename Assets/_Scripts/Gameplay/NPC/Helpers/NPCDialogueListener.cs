using BehaviorDesigner.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Gameplay
{
    public class NPCDialogueListener : MonoBehaviour
    {
        [Header("Conversation Stance")]
        public float conversationDistance = 2.0f;

        private NavMeshAgent _agent;
        private NPCBrain _brain;
        private BehaviorTree _behaviorTree;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _brain = GetComponent<NPCBrain>();
            _behaviorTree = GetComponent<BehaviorTree>();
        }

        private void OnEnable()
        {
            DialogueEvents.OnDialogueStarted += OnDialogueStarted;
            DialogueEvents.OnDialogueEnded += OnDialogueEnded;
        }

        private void OnDisable()
        {
            DialogueEvents.OnDialogueStarted -= OnDialogueStarted;
            DialogueEvents.OnDialogueEnded -= OnDialogueEnded;
        }

        private void OnDialogueStarted(Transform npcTransform)
        {
            // Freeze the BT exactly where it is — works regardless of what task is running
            _behaviorTree.enabled = false;

            // Stop the brain from thinking while paused
            _brain?.SetDialogue(true);

            // Stop the agent — PauseBehavior freezes the BT but not the NavMeshAgent
            if (_agent != null)
                _agent.isStopped = true;

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                SnapToConversationStance(playerObj.transform, transform);
        }

        private void OnDialogueEnded()
        {
            if (_agent != null)
                _agent.isStopped = false;

            _brain?.SetDialogue(false);

            // Unfreeze the BT — resumes from exactly where it left off
            _behaviorTree.enabled = true;
        }

        private void SnapToConversationStance(Transform player, Transform npc)
        {
            Vector3 midpoint = Vector3.Lerp(player.position, npc.position, 0.5f);
            Vector3 direction = (npc.position - player.position).normalized;

            if (direction == Vector3.zero)
                direction = npc.forward;

            player.position = midpoint - direction * (conversationDistance / 2f);
            npc.position = midpoint + direction * (conversationDistance / 2f);

            player.rotation = Quaternion.LookRotation(direction);
            npc.rotation = Quaternion.LookRotation(-direction);
        }
    }
}