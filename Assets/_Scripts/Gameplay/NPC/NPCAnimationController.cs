using UnityEngine;
using UnityEngine.AI;

public class NPCAnimationController : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>(); // optional, if using NavMesh
    }

    void Update()
    {
        // Drive locomotion blend tree from agent speed
        float speed = agent != null ? agent.velocity.magnitude : 0f;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime); // 0.1f = damping

        if (PlayerIsNearby())
        {
        }
    }

    // Call this from dialogue or event systems
    public void SetAttacking(bool isAttacking)
    {
        animator.SetBool("IsAttacking", isAttacking);
    }

    bool PlayerIsNearby()
    {
        // Replace with your own detection logic
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.transform.position) < 3f;
    }
}