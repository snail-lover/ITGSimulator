using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Game.Gameplay.AI.Tasks
{
    [TaskCategory("Game")]
    [TaskDescription("Moves to a target position, handling doors and off-mesh links via TraversalManager.")]
    public class MoveToDestination : NavMeshAction
    {
        public SharedGameObject interactionPoint;
        public SharedGameObject targetObject;
        public float stopDistance = 0.5f;

        private bool _pathSet = false;
        private Vector3 _finalTargetPos;

        public override void OnStart()
        {
            InitAgent();
            _pathSet = false;
            _finalTargetPos = Vector3.zero;

            if (Agent == null) return;

            Vector3 targetPos = Vector3.zero;
            if (interactionPoint.Value != null) targetPos = interactionPoint.Value.transform.position;
            else if (targetObject.Value != null) targetPos = targetObject.Value.transform.position;
            else return;

            Agent.speed = 3.5f;
            Agent.stoppingDistance = stopDistance;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 2.0f, NavMesh.AllAreas))
            {
                _finalTargetPos = hit.position;
                Agent.SetDestination(_finalTargetPos);
                _pathSet = true;
                Agent.isStopped = false;
            }
        }

        public override TaskStatus OnUpdate()
        {
            if (!_pathSet || Agent == null) return TaskStatus.Failure;

            // Let base class handle any off-mesh link
            TaskStatus? linkStatus = HandleOffMeshLink();
            if (linkStatus.HasValue) return linkStatus.Value;

            // Check arrival
            float distToTarget = Vector3.Distance(transform.position, _finalTargetPos);
            if (!Agent.pathPending && distToTarget <= stopDistance)
            {
                Agent.isStopped = true;
                Debug.Log($"[MoveToDestination] Arrived. Distance: {distToTarget}");
                return TaskStatus.Success;
            }

            return TaskStatus.Running;
        }

        protected override void OnLinkCrossed()
        {
            // Re-issue destination after crossing a link so path recalculates correctly
            if (_finalTargetPos != Vector3.zero)
                Agent.SetDestination(_finalTargetPos);
        }
    }
}