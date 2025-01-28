using UnityEngine;

public class BaseNPC : MonoBehaviour
{
    public virtual void Interact()
    {
        BaseDialogue.Instance.StartDialoguePlaceholder(this);
    }
}
