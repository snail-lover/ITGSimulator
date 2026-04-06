using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Game.Core;
using Unity.AI.Navigation;

namespace Game.Gameplay
{
    [RequireComponent(typeof(NavMeshLink))]
    [RequireComponent(typeof(SmartObject))]
    public class SmartPathLink : MonoBehaviour
    {
        [Header("State Configuration")]
        public StateDefinition openState;    // e.g. State_Open
        public StateDefinition lockedState;  // e.g. State_Locked

        private NavMeshLink _navLink;
        private SmartObject _smartObject;

        void Awake()
        {
            _navLink = GetComponent<NavMeshLink>();
            _smartObject = GetComponent<SmartObject>();
        }

        void OnEnable() { if(TraversalManager.Instance) TraversalManager.Instance.RegisterLink(this); }
        void OnDisable() { if(TraversalManager.Instance) TraversalManager.Instance.UnregisterLink(this); }

        public void GetWorldPositions(out Vector3 start, out Vector3 end)
        {
            if (_navLink == null) _navLink = GetComponent<NavMeshLink>();
            start = transform.TransformPoint(_navLink.startPoint);
            end = transform.TransformPoint(_navLink.endPoint);
        }

        // --- NEW HELPER FUNCTION ---
        public bool IsOpen()
        {
            // If there is no logic defined, assume it's always open (e.g. a window jump)
            if (_smartObject == null || openState == null) return true;
            
            // Return TRUE only if the Open State is 1
            return _smartObject.GetState(openState) == 1;
        }

        public IEnumerator PerformInteraction(GameObject npc)
        {
            if (_smartObject == null || openState == null) yield break;

            // 1. IS IT LOCKED?
            if (lockedState != null && _smartObject.GetState(lockedState) == 1)
            {
                Debug.Log($"[SmartPathLink] '{npc.name}' blocked: Door is LOCKED.");
                yield break; // Exit immediately. We do NOT open it.
            }

            // 2. IS IT ALREADY OPEN?
            if (_smartObject.GetState(openState) == 1)
            {
                yield break; // Already open, just proceed
            }

            // 3. OPEN IT
            Vector3 lookTarget = transform.position;
            lookTarget.y = npc.transform.position.y;
            npc.transform.LookAt(lookTarget);

            _smartObject.SetState(openState, 1);
            
            // Wait for animation
            yield return new WaitForSeconds(1.0f);
        }
    }
}