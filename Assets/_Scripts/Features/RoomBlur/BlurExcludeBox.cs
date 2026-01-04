using UnityEngine;

[ExecuteAlways]
public class BlurExcludeBox : MonoBehaviour
{
    public BoxCollider box;
    public bool invert; // if true, blur only inside the box

    static readonly int ID_WorldToBox = Shader.PropertyToID("_WorldToBox");
    static readonly int ID_BoxEnabled = Shader.PropertyToID("_BoxEnabled");
    static readonly int ID_BoxInvert = Shader.PropertyToID("_BoxInvert");

    void OnEnable() { Update(); }
    void OnDisable() { Shader.SetGlobalFloat(ID_BoxEnabled, 0f); }

    void LateUpdate() { Update(); }

    void Update()
    {
        if (!box) TryGetComponent(out box);
        if (!box) { Shader.SetGlobalFloat(ID_BoxEnabled, 0f); return; }

        // Build a TRS at the box center, with the object's rotation, and world-scaled size
        var t = box.transform;
        Vector3 worldCenter = t.TransformPoint(box.center);
        Vector3 worldSize = Vector3.Scale(t.lossyScale, box.size);
        var trs = Matrix4x4.TRS(worldCenter, t.rotation, worldSize); // unit cube [-0.5..0.5] scaled to box
        var worldToBox = trs.inverse;

        Shader.SetGlobalMatrix(ID_WorldToBox, worldToBox);
        Shader.SetGlobalFloat(ID_BoxInvert, invert ? 1f : 0f);
        Shader.SetGlobalFloat(ID_BoxEnabled, 1f);
    }
}
