using UnityEngine;
using System.Collections.Generic;

/// 게임플레이용 범위 시각화:
// - 바닥에 깔리는 반투명 채워진 링(메시, 소프트 엣지)
// - 외곽 라인(LineRenderer)

[RequireComponent(typeof(LineRenderer), typeof(MeshFilter), typeof(MeshRenderer))]
public class RingRange : MonoBehaviour
{
    [Header("Size")]
    [Min(0.01f)] public float radius = 5f; // 반경
    [Tooltip("0=완전 채움 1=아웃라인만 남음")][Range(0f, 0.99f)] public float innerGap = 0.55f;
    [Tooltip("n각형")][Range(8, 128)] public int segments = 96;

    [Header("Outline")]
    [Min(0.001f)] public float lineWidth = 0.05f;
    public Color lineColor = new(1f, 0.2f, 0.6f, 0.9f);

    [Header("Fill (ground ring)")]
    public Color fillColor = new(1f, 0.2f, 0.6f, 0.18f);
    [Range(0f, 0.5f)] public float softEdge = 0.12f; // 바깥쪽 부드러운 투명 그라데이션 폭(비율)

    [Header("Ripple on Shock")]
    public bool rippleOnShock = true;
    public float rippleWidthMul = 2.1f;
    public float rippleDuration = 0.2f;

    LineRenderer lr; // 외곽 라인
    MeshFilter mf; // 채워진 링
    MeshRenderer mr;
    Mesh fillMesh;

    void Awake()
    {
        // ===== Outline =====
        lr = GetComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.TransformZ;
        lr.textureMode = LineTextureMode.Stretch;
        lr.widthMultiplier = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.material.renderQueue = 3000; // Transparent
        lr.material.color = lineColor;

        // ===== Fill (mesh) =====
        // 비어 있으면 자동할당
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Sprites/Default"));
        mr.material.renderQueue = 3000; // Transparent
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        transform.position += Vector3.up * 0.02f;

        RebuildAll();
    }

    public void SetRadius(float r)
    {
        radius = Mathf.Max(0.01f, r);
        RebuildAll();
    }

    public void ShockRipple()
    {
        if (!rippleOnShock) return;
        StopAllCoroutines();
        StartCoroutine(CoRipple());
    }

    System.Collections.IEnumerator CoRipple()
    {
        float t = 0f;
        float baseW = lr.widthMultiplier;
        Color baseC = lr.material.color;
        while (t < rippleDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / rippleDuration);
            lr.widthMultiplier = Mathf.Lerp(baseW * rippleWidthMul, baseW, u);
            lr.material.color = Color.Lerp(lineColor, baseC, u);
            yield return null;
        }
        lr.widthMultiplier = baseW;
        lr.material.color = baseC;
    }

    void RebuildAll()
    {
        // 외곽 라인
        lr.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 p = new(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            lr.SetPosition(i, transform.position + p);
        }
        lr.material.color = lineColor;

        // 채워진 링(도넛) 메시
        if (fillMesh == null) fillMesh = new Mesh { name = "RingFillMesh" };
        BuildFilledRing(ref fillMesh, radius * Mathf.Clamp01(1f - softEdge), radius, segments, innerGap);
        mf.sharedMesh = fillMesh;

        // vertex color로 알파 주기(소프트 엣지)
        var colors = new Color[fillMesh.vertexCount];
        var verts = fillMesh.vertices;
        float rInner = radius * innerGap;
        float rSoftStart = radius * (1f - softEdge);
        for (int i = 0; i < verts.Length; i++)
        {
            float r = new Vector2(verts[i].x, verts[i].z).magnitude;
            float t = Mathf.InverseLerp(rSoftStart, radius, r); // 0=안쪽,1=바깥
            float a = Mathf.Lerp(fillColor.a, 0f, t); // 바깥으로 갈수록 페이드아웃
            colors[i] = new Color(fillColor.r, fillColor.g, fillColor.b, a);
        }
        fillMesh.colors = colors;
        mr.material.color = Color.white; // vertex color 사용
    }

    // rSoft/rOuter는 소프트엣지 영역, innerGap으로 가운데 구멍
    void BuildFilledRing(ref Mesh mesh, float rSoft, float rOuter, int seg, float inner)
    {
        float rInner = Mathf.Clamp(rOuter * inner, 0f, rOuter * 0.999f);
        int ringVerts = (seg + 1) * 2; // inner/outer
        List<Vector3> v = new(ringVerts);
        List<int> tri = new(seg * 6);
        List<Vector2> uv = new(ringVerts);

        for (int i = 0; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            Vector3 pOuter = new(ca * rOuter, 0f, sa * rOuter);
            Vector3 pInner = new(ca * rInner, 0f, sa * rInner);
            v.Add(pInner); uv.Add(new Vector2(0, i / (float)seg));
            v.Add(pOuter); uv.Add(new Vector2(1, i / (float)seg));
        }
        for (int i = 0; i < seg; i++)
        {
            int i0 = i * 2;
            tri.Add(i0); tri.Add(i0 + 1); tri.Add(i0 + 3);
            tri.Add(i0); tri.Add(i0 + 3); tri.Add(i0 + 2);
        }

        mesh.Clear();
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tri, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
