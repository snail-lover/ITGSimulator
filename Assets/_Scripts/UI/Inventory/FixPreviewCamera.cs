using UnityEngine;
using System.Reflection; // Allows us to touch URP settings without the reference

[RequireComponent(typeof(Camera))]
public class FixPreviewCamera : MonoBehaviour
{
    void Awake()
    {
        Camera cam = GetComponent<Camera>();

        // 1. Force Standard Camera Settings
        // This ensures the camera clears to 100% transparent black.
        // Your YAML showed it was Red (1,0,0,0), this forces it to (0,0,0,0).
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0); 
        
        cam.allowHDR = false;
        cam.allowMSAA = false;

        // 2. Force URP Specific Settings via Reflection
        // This finds the "UniversalAdditionalCameraData" component dynamically
        // and turns off the settings that force Opaque backgrounds.
        Component urpCamData = cam.GetComponent("UniversalAdditionalCameraData");
        
        if (urpCamData != null)
        {
            System.Type type = urpCamData.GetType();
            
            // Force Render Post Processing OFF (Crucial for Alpha)
            PropertyInfo postProcessProp = type.GetProperty("renderPostProcessing");
            if (postProcessProp != null) postProcessProp.SetValue(urpCamData, false);

            // Force Render Shadows OFF (Optimization)
            PropertyInfo shadowProp = type.GetProperty("renderShadows");
            if (shadowProp != null) shadowProp.SetValue(urpCamData, false);
        }
    }
}