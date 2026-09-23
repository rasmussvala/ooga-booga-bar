using UnityEngine;

public class PickupHighlight : MonoBehaviour
{
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private float intensity = 1.5f;

    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private Material[] materials;

    private void Awake()
    {
        // .materials makes per-object copies, so other objects sharing the material don't glow
        var list = new System.Collections.Generic.List<Material>();
        foreach (var r in GetComponentsInChildren<Renderer>())
            list.AddRange(r.materials);
        materials = list.ToArray();

        foreach (var m in materials)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor(EmissionColor, Color.black);
        }
    }

    public void SetHighlighted(bool on)
    {
        Color c = on ? highlightColor * intensity : Color.black;
        foreach (var m in materials)
            m.SetColor(EmissionColor, c);
    }
}
