using UnityEngine;

public class BaseInteract : MonoBehaviour, IInteractable
{
    public virtual void OnClick()
    {
        Debug.Log("On click fired");
    }

    public virtual void Interact()
    {
        Debug.Log("Interacting");  
    }

    public virtual void WhenHovered()
    {
        Debug.Log("Hovering Object");
    }

    public virtual bool CanInteract()
    {  return true; }
}
