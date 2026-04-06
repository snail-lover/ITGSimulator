using UnityEngine;
using System.Collections;
using Game.Core;     
using Game.Gameplay; 

namespace Game.Features
{
    [RequireComponent(typeof(SmartObject))]
    public class ExplosiveDevice : MonoBehaviour
    {
        [Header("Trigger")]
        public StateDefinition fuseState;
        public float fuseDuration = 3.0f;

        [Header("Explosion")]
        public GameObject explosionPrefab; 
        public float explosionRadius = 5f;
        public float explosionForce = 700f;
        
        [Header("Audio")]
        public AudioSource fuseAudio;       // Looping Hiss
        public AudioClip explosionSound;    // ONE-SHOT Boom (Assign this!)

        [Header("Flashbang Effect")]
        public bool causesScreenFlash = true;
        public float flashDuration = 3.0f;

        private SmartObject _smartObj;
        private bool _hasExploded = false;

        void Awake()
        {
            _smartObj = GetComponent<SmartObject>();
        }

        void OnEnable() => _smartObj.OnStateChanged += OnStateChanged;
        void OnDisable() => _smartObj.OnStateChanged -= OnStateChanged;

        private void OnStateChanged(StateDefinition state, int value)
        {
            if (state == fuseState && value == 1 && !_hasExploded)
            {
                StartCoroutine(FuseRoutine());
            }
        }

        private IEnumerator FuseRoutine()
        {
            Debug.Log("Fuse Lit! Run!");
            if (fuseAudio != null) fuseAudio.Play();

            yield return new WaitForSeconds(fuseDuration);

            Explode();
        }

        private void Explode()
        {
            _hasExploded = true;

            // 1. Play Sound (Independent of this object so it doesn't cut off)
            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position);
            }

            // 2. Spawn Visuals
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }

            // 3. Physics Force
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (Collider nearby in colliders)
            {
                Rigidbody rb = nearby.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                }
            }

            // 4. Flash Effect
            if (causesScreenFlash)
            {
                CheckPlayerFlash();
            }

            Destroy(gameObject);
        }

        private void CheckPlayerFlash()
        {
            Camera playerCam = Camera.main; 
            if (playerCam == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerCam.transform.position);

            Debug.Log($"[Explosion] Distance to Player: {distToPlayer:F2}m (Radius: {explosionRadius}m)");

            if (distToPlayer <= explosionRadius)
            {
                // 1. Lift the start point up by 0.5m so we don't hit the floor instantly
                Vector3 startPos = transform.position + Vector3.up * 0.5f;
                Vector3 endPos = playerCam.transform.position;

                // 2. Use RaycastHit to see WHAT we hit
                // We assume walls are on "Default" or "Ground"
                if (Physics.Linecast(startPos, endPos, out RaycastHit hit, LayerMask.GetMask("Default", "Ground")))
                {
                    // WE HIT SOMETHING. Let's check what it is.
                    
                    // Is it the Player? (Check root to handle child colliders)
                    bool hitPlayer = (hit.transform.root == playerCam.transform.root);

                    // Is it the Bomb itself? (In case startPos is still inside the bomb collider)
                    bool hitSelf = (hit.transform == transform);

                    if (hitPlayer || hitSelf)
                    {
                        // Logic: We hit the player (good!) or ourselves (ignore), so the path is effectively clear.
                        TriggerFlash(distToPlayer);
                    }
                    else
                    {
                        // We hit a wall, a crate, or something else.
                        Debug.Log($"[Explosion] Blocked by: {hit.transform.name}");
                    }
                }
                else
                {
                    // We hit NOTHING (Clear air). The path is clear.
                    TriggerFlash(distToPlayer);
                }
            }
            else
            {
                Debug.Log("[Explosion] Player was too far away for flash.");
            }
        }

           private void TriggerFlash(float distToPlayer)
          {
            Debug.Log("<color=white>FLASHBANG! Direct line of sight confirmed.</color>");
            float intensity = 1f - (distToPlayer / explosionRadius);
            GameEvents.RequestScreenFlash(flashDuration, intensity);
        }
        }
    }
