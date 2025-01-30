using UnityEngine;

public class ClickableCube : BaseInteract
{
    public override void Interact()
    {
        GetComponent<Renderer>().material.color = Random.ColorHSV();
        base.Interact();
    }
}
