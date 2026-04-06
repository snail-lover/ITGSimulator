using UnityEngine;
using System.Collections.Generic;
using Game.Core;

namespace Game.Gameplay
{
    public class SmartObjectManager : MonoBehaviour
    {
        public static SmartObjectManager Instance { get; private set; }

        // A list of all objects that have AT LEAST one AI interaction
        private List<SmartObject> _aiRelevantObjects = new List<SmartObject>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RegisterObject(SmartObject obj)
        {
            if (!_aiRelevantObjects.Contains(obj))
            {
                _aiRelevantObjects.Add(obj);
            }
        }

        public void UnregisterObject(SmartObject obj)
        {
            if (_aiRelevantObjects.Contains(obj))
            {
                _aiRelevantObjects.Remove(obj);
            }
        }

        // The Optimized Query
        public List<SmartObject> GetRelevantObjects()
        {
            return _aiRelevantObjects;
        }
    }
}