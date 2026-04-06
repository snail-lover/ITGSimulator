using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class FootstepSystem : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private List<AudioClip> footstepClips;

    [Header("Variance")]
    [Range(0.1f, 2f)]   [SerializeField] private float volume = 0.5f;
    [Range(0.8f, 1.2f)] [SerializeField] private float minPitch = 0.85f;
    [Range(0.8f, 1.2f)] [SerializeField] private float maxPitch = 1.1f;

    [Header("Foot Bones — drag them in from the Hierarchy")]
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightFoot;

    [Header("Logic")]
    [SerializeField] private float minSpeedForSound = 0.5f;
    // How low the foot must be (world Y) to count as a "step"
    [SerializeField] private float footGroundThreshold = 0.1f;

    private AudioSource _source;
    private CharacterController _cc;
    private Rigidbody _rb;
    private Animator _animator;

    // Track whether each foot was already "down" last frame
    private bool _leftWasDown;
    private bool _rightWasDown;

    private void Awake()
    {
        _source   = GetComponent<AudioSource>();
        _cc       = GetComponentInParent<CharacterController>();
        _rb       = GetComponentInParent<Rigidbody>();
        _animator = GetComponentInParent<Animator>();
    }

    private void Update()
    {
        bool moving = IsMovingFastEnough();
        //Debug.Log($"[Footstep] Moving: {moving} | LeftFoot Y: {leftFoot?.position.y:F3} | RightFoot Y: {rightFoot?.position.y:F3} | Root Y: {transform.root.position.y:F3} | Clips: {footstepClips.Count}");

        if (!moving) return;

        CheckFoot(leftFoot,  ref _leftWasDown);
        CheckFoot(rightFoot, ref _rightWasDown);
    }

private void CheckFoot(Transform foot, ref bool wasDown)
{
    if (foot == null) return;

    float diff = foot.position.y - transform.root.position.y;
    bool isDown = diff < footGroundThreshold;

    if (isDown && !wasDown)
    {
        PlayFootstep();
    }

    wasDown = isDown;
}

    private void PlayFootstep()
    {
        if (footstepClips.Count == 0) return;

        var clip = footstepClips[Random.Range(0, footstepClips.Count)];
        _source.pitch = Random.Range(minPitch, maxPitch);
        _source.PlayOneShot(clip, volume);
    }

    private bool IsMovingFastEnough()
    {
        if (_rb != null && !_rb.isKinematic)
            return _rb.linearVelocity.magnitude >= minSpeedForSound;

        if (_cc != null)
            return _cc.velocity.magnitude >= minSpeedForSound;

        if (_animator != null)
            return _animator.GetFloat("Speed") >= minSpeedForSound;

        return true;
    }
}