using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editable cut-corner neon frame for the Campus Ride UI prefab.
/// </summary>
[ExecuteAlways]
public class CampusRideCutFrame : Graphic
{
    [Range(2f, 80f)] public float cornerCut = 26f;
    [Range(1f, 12f)] public float thickness = 3f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        float l = r.xMin, b = r.yMin, rr = r.xMax, t = r.yMax;
        float c = Mathf.Min(cornerCut, Mathf.Min(r.width, r.height) * 0.25f);
        float inset = Mathf.Max(0.5f, thickness);
        Vector2[] outer =
        {
            new Vector2(l + c, t), new Vector2(rr - c, t), new Vector2(rr, t - c),
            new Vector2(rr, b + c), new Vector2(rr - c, b), new Vector2(l + c, b),
            new Vector2(l, b + c), new Vector2(l, t - c)
        };
        Vector2[] inner =
        {
            new Vector2(l + c + inset, t - inset), new Vector2(rr - c - inset, t - inset),
            new Vector2(rr - inset, t - c - inset), new Vector2(rr - inset, b + c + inset),
            new Vector2(rr - c - inset, b + inset), new Vector2(l + c + inset, b + inset),
            new Vector2(l + inset, b + c + inset), new Vector2(l + inset, t - c - inset)
        };
        for (int i = 0; i < 8; i++)
        {
            vh.AddVert(outer[i], color, Vector2.zero);
            vh.AddVert(inner[i], color, Vector2.zero);
        }
        for (int i = 0; i < 8; i++)
        {
            int n = (i + 1) % 8;
            int o0 = i * 2, o1 = n * 2;
            vh.AddTriangle(o0, o1, o1 + 1);
            vh.AddTriangle(o0, o1 + 1, o0 + 1);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
