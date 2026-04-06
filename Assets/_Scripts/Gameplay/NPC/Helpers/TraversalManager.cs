using UnityEngine;
using System.Collections.Generic;

namespace Game.Gameplay
{
    public class TraversalManager : MonoBehaviour
    {
        public static TraversalManager Instance { get; private set; }

        private List<SmartPathLink> _activeLinks = new List<SmartPathLink>();

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        public void RegisterLink(SmartPathLink link)
        {
            if (link != null && !_activeLinks.Contains(link))
            {
                _activeLinks.Add(link);
                // Debug.Log($"[TraversalManager] Registered '{link.name}'");
            }
        }

        public void UnregisterLink(SmartPathLink link)
        {
            if (_activeLinks.Contains(link)) _activeLinks.Remove(link);
        }

        public SmartPathLink GetLinkAtPosition(Vector3 agentPos)
        {
            // --- EMERGENCY FIX: RECOVERY ---
            // If the list is empty, the Execution Order messed up. Fix it now.
            if (_activeLinks.Count == 0)
            {
                SmartPathLink[] foundLinks = FindObjectsOfType<SmartPathLink>();
                if (foundLinks.Length > 0)
                {
                    //Debug.LogWarning($"[TraversalManager] List was empty but found {foundLinks.Length} links in scene. Re-registering...");
                    foreach (var link in foundLinks) RegisterLink(link);
                }
            }
            // -------------------------------

            float searchRadius = 3.0f; // Wide radius to catch offset doors
            SmartPathLink bestLink = null;
            float closestDistance = float.MaxValue;
            int checkedCount = 0;

            foreach (var link in _activeLinks)
            {
                if (link == null) continue;
                checkedCount++;

                link.GetWorldPositions(out Vector3 start, out Vector3 end);

                // Check distance to both sides of the door
                float dist = Mathf.Min(Vector3.Distance(agentPos, start), Vector3.Distance(agentPos, end));

                if (dist < searchRadius && dist < closestDistance)
                {
                    closestDistance = dist;
                    bestLink = link;
                }
            }

            if (bestLink == null)
            {
                Debug.LogWarning($"[TraversalManager] Lookup Failed at {agentPos}. Checked {checkedCount} links. (Is the link start point too far from the agent?)");
            }

            return bestLink;
        }
    }
}