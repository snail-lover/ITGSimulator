using UnityEngine;

public class ClickableCube : BaseInteract
{
    public override void Interact()
    {
        base.Interact();
        GetComponent<Renderer>().material.color = Random.ColorHSV();
    }
}
