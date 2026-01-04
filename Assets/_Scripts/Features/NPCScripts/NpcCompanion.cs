// NpcCompanion.cs
using UnityEngine;
using BehaviorDesigner.Runtime;

[RequireComponent(typeof(NpcController))]
public class NpcCompanion : MonoBehaviour
{
    private NpcController _controller;
    private BehaviorTree _behaviorTree;

    public void Initialize(NpcController controller)
    {
        _controller = controller;
        _behaviorTree = GetComponent<BehaviorTree>();
    }

    public void Activate()
    {
        if (_behaviorTree != null)
        {
            _behaviorTree.EnableBehavior();
        }
    }

    public void Deactivate()
    {
        if (_behaviorTree != null)
        {
            _behaviorTree.DisableBehavior(true);
        }
    }
}