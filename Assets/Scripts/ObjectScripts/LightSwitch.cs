using UnityEngine;

public class LightSwitch : BaseInteract
{
    public Light controlledLight;
    public bool isLightOn = false;

    public override void Interact()
    {
        base.Interact();

        if (controlledLight != null)
        {
            isLightOn = !isLightOn;
            controlledLight.enabled = isLightOn;
        }
    }
}
