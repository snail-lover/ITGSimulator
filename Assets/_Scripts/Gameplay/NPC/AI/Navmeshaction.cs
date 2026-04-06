using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;

namespace Game.Gameplay.AI.Tasks
{
    /// <summary>
    /// Base class for any BT action that moves an NPC on the NavMesh.
    /// Handles off-mesh link traversal (doors, etc.) via TraversalManager,
    /// so subclasses don't need to duplicate this logic.
    /// </summary>
    public abstract class NavMeshAction : Action
    {
        protected NavMeshAgent Agent;
        protected bool IsTraversing = false;
        protected bool TraversalFailed = false;

        protected void InitAgent()
        {
            Agent = GetComponent<NavMeshAgent>();
            IsTraversing = false;
            TraversalFailed = false;

            if (Agent != null)
                Agent.autoTraverseOffMeshLink = false; // Always manual
        }

        /// <summary>
        /// Call this at the top of OnUpdate() in any subclass.
        /// Returns Running while traversing, Failure if a locked door blocked us,
        /// or null if no link is active (subclass should handle movement normally).
        /// </summary>
        protected TaskStatus? HandleOffMeshLink()
        {
            if (Agent == null) return TaskStatus.Failure;

            Agent.autoTraverseOffMeshLink = false;

            if (TraversalFailed)
            {
                Agent.ResetPath();
                TraversalFailed = false; // Reset so NPC can try new paths
                return TaskStatus.Failure;
            }

            if (IsTraversing)
                return TaskStatus.Running;

            if (Agent.isOnOffMeshLink)
            {
                IsTraversing = true;
                StartCoroutine(ExecuteTraversal());
                return TaskStatus.Running;
            }

            return null; // No link active, caller handles normally
        }

        private IEnumerator ExecuteTraversal()
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;

            Vector3 linkStartPos = Agent.currentOffMeshLinkData.startPos;
            SmartPathLink smartLink = TraversalManager.Instance?.GetLinkAtPosition(linkStartPos);

            bool canCross = false;

            if (smartLink != null)
            {
                yield return StartCoroutine(smartLink.PerformInteraction(gameObject));
                canCross = smartLink.IsOpen();

                if (!canCross)
                    Debug.LogWarning($"[NavMeshAction] '{gameObject.name}' blocked by locked door.");
            }
            else
            {
                // No smart link = normal jump/drop, just cross
                canCross = true;
            }

            if (!canCross)
            {
                TraversalFailed = true;
                Agent.Warp(transform.position);
                Agent.ResetPath();
                IsTraversing = false;
                yield break;
            }

            yield return StartCoroutine(CrossLinkPhysically());

            Agent.CompleteOffMeshLink();
            yield return null; // One frame for NavMesh to settle
            
            OnLinkCrossed(); // Notify subclass (e.g. to re-issue SetDestination)

            IsTraversing = false;
            Agent.isStopped = false;
        }

        /// <summary>
        /// Called after successfully crossing an off-mesh link.
        /// Override in subclass if you need to re-issue SetDestination.
        /// </summary>
        protected virtual void OnLinkCrossed() { }

        private IEnumerator CrossLinkPhysically()
        {
            Vector3 endPos = Agent.currentOffMeshLinkData.endPos;
            endPos.y += Agent.baseOffset;

            while (Vector3.Distance(transform.position, endPos) > 0.1f)
            {
                Vector3 dir = (endPos - transform.position).normalized;
                if (dir != Vector3.zero)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);
                }

                transform.position = Vector3.MoveTowards(transform.position, endPos, Agent.speed * Time.deltaTime);
                yield return null;
            }

            transform.position = endPos;
            Agent.Warp(endPos);
        }
    }
}