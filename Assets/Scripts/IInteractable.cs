using UnityEngine;

public interface IInteractable
{
    void OnClick(); //called when the object is clicked
    void Interact(); //called when interaction occurs
    void WhenHovered(); //called when the object is hovered
    bool CanInteract(); // returns when it can be interacted with
}
