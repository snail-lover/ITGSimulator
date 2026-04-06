using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerDragController : MonoBehaviour
    {
        [Header("Settings")]
        public float breakDistance = 1.5f; 
        public float draggingMass = 50f;

        // References
        private PlayerMotor _motor;
        private Rigidbody _playerRb;
        private Rigidbody _currentObjectRb;
        private ConfigurableJoint _joint;
        private PlayerHoldController _holdController; // <--- NEW REF

        // State
        private float _originalMass;
        private float _originalObjectAngularDrag;
        private float _initialDistance;

        void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _playerRb = GetComponent<Rigidbody>();
            _holdController = GetComponent<PlayerHoldController>(); // <--- Get Component
        }

        void Update()
        {
            if (_joint != null && _currentObjectRb != null)
            {
                float currentDist = Vector3.Distance(transform.position, _currentObjectRb.position);
                if (currentDist > _initialDistance + breakDistance)
                {
                    StopDrag();
                    Debug.Log("Drag connection broken (Object stuck?)");
                }
            }
        }

        public void StartDrag(GameObject target)
        {
            if (target == null) return;

            // --- NEW: LOGIC TO HOLSTER ITEM ---
            // If we are holding something, put it away (back to inventory) before grabbing the couch
            if (_holdController != null)
            {
                // DropItem() clears visuals and flags, but does NOT remove from Inventory list.
                // This effectively "Holsters" it.
                _holdController.DropItem(); 
            }
            // ----------------------------------

            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb == null) return;

            _currentObjectRb = targetRb;
            _motor.SetFacingTarget(target.transform);

            _originalMass = _playerRb.mass;
            _playerRb.mass = draggingMass;

            _originalObjectAngularDrag = _currentObjectRb.angularDamping; 
            _currentObjectRb.angularDamping = 10f; 

            _joint = gameObject.AddComponent<ConfigurableJoint>();
            _joint.connectedBody = _currentObjectRb;
            _joint.autoConfigureConnectedAnchor = true;

            _joint.xMotion = ConfigurableJointMotion.Locked;
            _joint.yMotion = ConfigurableJointMotion.Locked;
            _joint.zMotion = ConfigurableJointMotion.Locked;

            _joint.angularXMotion = ConfigurableJointMotion.Locked;
            _joint.angularYMotion = ConfigurableJointMotion.Free; 
            _joint.angularZMotion = ConfigurableJointMotion.Locked;

            _initialDistance = Vector3.Distance(transform.position, target.transform.position);
        }

        public void StopDrag()
        {
            if (_joint != null)
            {
                Destroy(_joint);
                _joint = null;
            }

            if (_currentObjectRb != null)
            {
                _currentObjectRb.angularDamping = _originalObjectAngularDrag;
            }

            _playerRb.mass = _originalMass;
            _motor.SetFacingTarget(null);
            _currentObjectRb = null;
        }

        public bool IsDragging => _joint != null;
    }
}