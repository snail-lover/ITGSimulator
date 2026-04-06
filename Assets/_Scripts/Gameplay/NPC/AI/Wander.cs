using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime.Tasks;

namespace Game.Gameplay.AI.Tasks
{
    [TaskCategory("Game")]
    [TaskDescription("Wanders the NavMesh indefinitely, opening doors via TraversalManager. Interrupted by NPCBrain restarting the BT when a need becomes urgent.")]
    public class Wander : NavMeshAction
    {
        public float wanderRadius = 8f;
        public float minWanderDistance = 3f;

        public float arrivalDistance = 0.6f;

        public float minIdleTime = 1f;
        public float maxIdleTime = 4f;

        private float _idleTimer = 0f;
        private float _idleDuration = 0f;
        private bool _arrived = false;
        private bool _hasDestination = false;
        private Vector3 _destination;

        public override void OnStart()
        {
            InitAgent();
            _arrived = false;
            _hasDestination = false;
            _idleTimer = 0f;

            if (Agent != null)
                Agent.speed = 3.5f;

            PickNewDestination();
        }

        public override TaskStatus OnUpdate()
        {
            if (Agent == null) return TaskStatus.Failure;

            // Let base class handle any off-mesh link (doors etc.)
            TaskStatus? linkStatus = HandleOffMeshLink();
            if (linkStatus.HasValue)
            {
                // If traversal failed (locked door), just pick a different wander point
                if (linkStatus.Value == TaskStatus.Failure)
                {
                    TraversalFailed = false;
                    _arrived = true;
                    _idleDuration = 0.5f;
                    _idleTimer = 0f;
                }
                return TaskStatus.Running; // Wander never returns Failure to the tree
            }

            if (!_arrived)
            {
                if (Agent.pathPending) return TaskStatus.Running;

                float dist = Vector3.Distance(transform.position, _destination);
                if (_hasDestination && dist <= arrivalDistance)
                {
                    _arrived = true;
                    Agent.isStopped = true;
                    _idleDuration = Random.Range(minIdleTime, maxIdleTime);
                    _idleTimer = 0f;
                }

                return TaskStatus.Running;
            }

            // Idling at destination
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= _idleDuration)
            {
                _arrived = false;
                Agent.isStopped = false;
                PickNewDestination();
            }

            return TaskStatus.Running; // Always running — NPCBrain restarts BT to interrupt
        }

        public override void OnEnd()
        {
            if (Agent != null) Agent.isStopped = false;
        }

        protected override void OnLinkCrossed()
        {
            // Re-issue destination after crossing a link
            if (_hasDestination)
                Agent.SetDestination(_destination);
        }

        private void PickNewDestination()
        {
            if (Agent == null) return;

            Vector3 point = GetRandomNavMeshPoint();

            if (point == Vector3.zero)
            {
                // No point found — wait briefly and retry
                _arrived = true;
                _idleDuration = 1f;
                _idleTimer = 0f;
                return;
            }

            _destination = point;
            _hasDestination = true;
            Agent.isStopped = false;
            Agent.SetDestination(_destination);
        }

        private Vector3 GetRandomNavMeshPoint()
        {
            Vector3 bestFallback = Vector3.zero;

            for (int i = 0; i < 15; i++)
            {
                Vector2 flatRandom = Random.insideUnitCircle.normalized;
                float distance = Random.Range(minWanderDistance, wanderRadius);
                Vector3 candidate = transform.position + new Vector3(flatRandom.x, 0, flatRandom.y) * distance;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(candidate, out hit, wanderRadius, NavMesh.AllAreas))
                {
                    if (Vector3.Distance(transform.position, hit.position) >= minWanderDistance)
                        return hit.position;

                    if (bestFallback == Vector3.zero)
                        bestFallback = hit.position;
                }
            }

            return bestFallback;
        }
    }
}