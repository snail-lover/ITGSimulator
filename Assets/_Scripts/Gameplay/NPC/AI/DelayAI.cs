using UnityEngine;
using BehaviorDesigner.Runtime;

public class DelayAI : MonoBehaviour
{
    void Start()
    {
        // Wait 0.1 seconds to ensure NavMeshAgent is ready
        Invoke(nameof(TurnOn), 0.1f);
    }

    void TurnOn()
    {
        GetComponent<BehaviorTree>().EnableBehavior();
    }
}