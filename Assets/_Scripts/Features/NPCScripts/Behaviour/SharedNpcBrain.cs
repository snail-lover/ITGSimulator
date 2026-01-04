// SharedNpcBrain.cs
using BehaviorDesigner.Runtime;

// The [System.Serializable] attribute is important.
[System.Serializable]
public class SharedNpcBrain : SharedVariable<NpcBrain>
{
    // This part just makes it easier to use in code, it's good practice.
    public static implicit operator SharedNpcBrain(NpcBrain value) { return new SharedNpcBrain { Value = value }; }
}