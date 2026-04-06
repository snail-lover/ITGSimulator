using UnityEngine;
using UnityEngine.AI;

namespace Game.Gameplay
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class DebugWalker : MonoBehaviour
    {
        public Transform targetDestination;

        private NavMeshAgent _agent;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            
            // 1. Verify Component Exists
            if (_agent == null)
            {
                Debug.LogError($"[CRITICAL] No NavMeshAgent found on GameObject '{gameObject.name}'!");
            }
            else
            {
                Debug.Log($"[SUCCESS] NavMeshAgent found on '{gameObject.name}'.");
            }
        }

        void Start()
        {
            // 2. Verify NavMesh Placement
            if (_agent != null && !_agent.isOnNavMesh)
            {
                Debug.LogError($"[CRITICAL] Agent is on '{gameObject.name}' but NOT placed on a valid NavMesh. Is the mesh baked? Is the NPC floating?");
            }
        }

        void Update()
        {
            if (targetDestination == null || _agent == null) return;

            // 3. Command Movement
            _agent.SetDestination(targetDestination.position);

            // Debug Lines
            Debug.DrawLine(transform.position, targetDestination.position, Color.yellow);
        }
    }
}