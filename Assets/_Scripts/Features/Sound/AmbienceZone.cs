using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class AmbienceZone : MonoBehaviour
{
    [Header("The Sound for this Area")]
    public AudioClip ambienceClip;

    [Header("Gizmo Color")]
    public Color debugColor = new Color(0, 1, 0, 0.3f);

    private void OnTriggerEnter(Collider other)
    {
        // Check if the thing entering is the Player Root
        // (Assuming your Player script is on the root)
        if (other.GetComponent<PlayerMotor>() != null || other.CompareTag("Player"))
        {
            AmbienceManager.Instance.SetAmbience(ambienceClip);
        }
    }

    // Draw a colored box in the Scene view so you can see the zones
    private void OnDrawGizmos()
    {
        Gizmos.color = debugColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}