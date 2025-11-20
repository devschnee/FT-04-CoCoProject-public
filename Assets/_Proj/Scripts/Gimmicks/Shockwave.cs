using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 11/20 TODO : 차폐범위 검토. 수정 필요 시 수정.
// 충격파 발생
[DisallowMultipleComponent]
public class Shockwave : MonoBehaviour
{
    [Header("Shockwave Settings")]
    [Min(1f)] public float tileHeight = 1f; // 타일 한 칸 높이
    [Tooltip("충격파 반경 ==> 중요!! : 여기 바뀌면 RingRange 컴포넌트의 범위도 같이 바꿔줘야 함!!!")]
    [Range(1f, 7f)] public float radiusShock = 5f;
    [Tooltip("충격파 영향 받는 Pushables가 포함된 레이어")]
    public LayerMask targetLayer = ~0;

    // HACK : 이건 따로 빼서 차폐물 자체에 붙여 주는 형식으로 변경하는 것이 나을 지도
    [Header("Occlusion")]
    [Tooltip("충격파 차폐 될 지 여부")]
    public bool useOcclusion = false;
    [Tooltip("차폐 인정할 레이어")]
    public LayerMask occluderMask = ~0;

    [Header("Lift Timing")]
    [Tooltip("떠오르는 데 걸리는 시간")]
    [Min(0.01f)] public float riseSec = 1f;
    [Tooltip("공중에서 홀딩되는 시간")]
    [Min(0.0f)] public float hangSec = 0.4f;
    [Tooltip("떨어지는 데 걸리는 시간")]
    [Min(0.01f)] public float fallSec = 1f;

    [Header("Simple Height Filter")]
    public bool affectSameFloorOnly = false;   // 같은 층만 영향
    public bool alsoAffectStackedAbove = false; // 위층은 '적층'일 때만 (옵션)

    public void Fire()
    {
        long newToken = DateTime.Now.Ticks;
        Fire(
            origin: transform.position,
            tile: tileHeight,
            radiusTiles: radiusShock,
            targetLayer: targetLayer,
            useOcclusion: useOcclusion,
            occludeMask: occluderMask,
            riseSeconds: riseSec,
            hangSeconds: hangSec,
            fallSeconds: fallSec,
            token: newToken
         );
    }

    public void Fire(
        Vector3 origin,
        float tile,
        float radiusTiles,
        LayerMask targetLayer,
        bool useOcclusion,
        LayerMask occludeMask,
        float riseSeconds,
        float hangSeconds,
        float fallSeconds,
        long token = 0L
    )
    {
        float t = Mathf.Max(0.0001f, tile);
        float radiusWorld = Mathf.Max(0.0001f, radiusTiles) * t;

        // 중심 레벨(Y) 스냅
        int centerH = Mathf.FloorToInt(origin.y / t + 1e-4f);

        // 반경 내 후보 수집
        var cols = Physics.OverlapSphere(origin, radiusWorld, targetLayer, QueryTriggerInteraction.Ignore);
        Debug.Log($"[SW] OverlapSphere hits={cols?.Length ?? 0} radiusW={radiusWorld}", this);
        if (cols == null || cols.Length == 0) return;

        var all = new List<(Collider col, Component pushable, int h, int gx, int gz, float ts)>();
        foreach (var c in cols)
        {
            if (c.transform == transform) continue;
            // PushableObejcts.cs를 찾거나 상속 클래스 컴포넌트 찾음
            var p = (Component)c.GetComponent(typeof(PushableObjects));
            float ts = p != null ? GetTileSize(p, tile) : tile; // !pushables는 기본 타일 높이
            int h = Mathf.FloorToInt(c.transform.position.y / ts + 1e-4f);
            int gx = Mathf.FloorToInt((c.transform.position.x + 0.5f * ts) / ts);
            int gz = Mathf.FloorToInt((c.transform.position.z + 0.5f * ts) / ts);
            all.Add((c, p, h, gx, gz, ts));
        }

        // 기준층(centerH) 가시성 검사 (pushables도 occlude 가능)
        LayerMask blockMask = occluderMask | targetLayer;
        var baseHits = new List<(Collider col, Component pushable, int gx, int gz, float ts)>();

        foreach (var e in all)
        {
            if (e.h != centerH) continue;

            if (useOcclusion && IsLineBlocked(origin, e.col, centerH, e.ts, blockMask, this.transform))
                continue; // 가려진 물체는 기준층에서도 제외
            baseHits.Add((e.col, e.pushable, e.gx, e.gz, e.ts));
        }

        // 컬럼 전파: 같은 (gx,gz)에서 h >= centerH 전부 리프트
        foreach (var baseHit in baseHits)
        {
            int gx = baseHit.gx;
            int gz = baseHit.gz;

            // 기준층 포함해서 위로 쭉 검사
            var columnObjs = all.Where(e => e.gx == gx && e.gz == gz && e.h >= centerH)
                                .OrderBy(e => e.h).ToList();

            foreach (var e in columnObjs)
            {
                // 기준층은 origin→타겟 검사
                // 위층은 baseHit→타겟 검사 (중간 occlude 차단)
                Vector3 src = (e.h == centerH)
                    ? origin
                    : baseHit.col.transform.position + Vector3.up * (baseHit.ts * 0.5f);

                if (useOcclusion && IsLineBlocked(src, e.col, e.h, e.ts, blockMask, this.transform))
                    break; // 중간에서 막히면 그 위는 영향 X

                float rise = Mathf.Max(0.0001f, riseSeconds);
                float hold = Mathf.Max(0f, hangSeconds);
                float fall = Mathf.Max(0.001f, fallSeconds);

                if (e.pushable != null)
                {
                    if (!TryCallLiftAny(e.pushable, rise, hold, fall))
                        StartCoroutine(LiftTransCoroutine(e.col.transform, e.ts, rise, hold, fall));
                }
                else
                {
                    StartCoroutine(LiftTransCoroutine(e.col.transform, e.ts, rise, hold, fall));
                }
            }
        }
    }

        static bool TryCallLiftAny(Component target, float rise, float hang, float fall)
    {
        const string methodName = "WaveLift";
        var type = target.GetType();
        var methods = type.GetMethods().Where(m => m.Name == methodName);

        foreach (var m in methods)
        {
            var ps = m.GetParameters();
            object[] args = null;

            if (ps.Length == 3) args = new object[] { ConvertArg(ps[0], rise), ConvertArg(ps[1], hang), ConvertArg(ps[2], fall) };
            else if (ps.Length == 2) args = new object[] { ConvertArg(ps[0], rise), ConvertArg(ps[1], hang) };
            else if (ps.Length == 1) args = new object[] { ConvertArg(ps[0], rise) };
            else if (ps.Length == 0) args = null;
            else continue;

            try { m.Invoke(target, args); return true; }
            catch { /* 다음 시그니처 시도 */ }
        }
        return false;
    }

    static bool IsLineBlocked(
    Vector3 origin, // 충격파 발사 원점
    Collider targetCol, // 리프트 대상
    int centerH,
    float tile,
    LayerMask blockMask, // occludeMask | targetLayer
    Transform ignoreRoot // 발사자(this.transform)
    )
    {
        // 중심 높이에서 가시선 검사
        float yCentre = centerH * tile + tile * 0.5f;
        float halfBand = tile * 0.45f; // 밴드 두께

        Vector3 a = new(origin.x, yCentre, origin.z);
        Vector3 b = new(targetCol.transform.position.x, yCentre, targetCol.transform.position.z);
        Vector3 dir = b - a;
        float dist = dir.magnitude;
        if (dist <= 0.0001f) return false;
        dir /= dist;

        float startOffset = 0.01f;
        Vector3 startRay = a + dir * startOffset;
        float rayDist = dist - startOffset;

        var hits = Physics.RaycastAll(a, dir, dist - 0.01f, blockMask, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            var tr = hit.collider.transform;

            // 발사자/자식 무시
            if (ignoreRoot && (tr == ignoreRoot || tr.IsChildOf(ignoreRoot))) continue;
            // 타깃 자신/자식 무시
            if (hit.collider == targetCol) continue;
            if (tr == targetCol.transform || tr.IsChildOf(targetCol.transform)) continue;

            // 충격파 밴드와 수직으로 겹치지 않으면 무시
            var cb = hit.collider.bounds;
            if (cb.max.y < yCentre - halfBand) continue;
            if (cb.min.y > yCentre + halfBand) continue;

            // 추가 검증 : 차폐물도 정확히 기준층에 위치해야 함
            float tsOcc = tile;
            var pOcc = (Component)hit.collider.GetComponent(typeof(PushableObjects));
            if (pOcc != null) tsOcc = GetTileSize(pOcc, tile);

            int hOcc = Mathf.FloorToInt(hit.collider.transform.position.y / tsOcc + 1e-4f);
            if (hOcc != centerH) continue; // 다른 층이 가리는 건 무시


            return true; // 밴드 안에서 라인 가림
        }
        return false;
    }

    static object ConvertArg(ParameterInfo pi, float value)
    {
        if (pi.ParameterType == typeof(int)) return Mathf.RoundToInt(value);
        if (pi.ParameterType == typeof(float)) return value;
        return null;
    }

    static float GetTileSize(Component p, float fallback)
    {
        var t = p.GetType();
        var f = t.GetField("tileSize");
        if (f != null && f.FieldType == typeof(float))
            return Mathf.Max(0.0001f, (float)f.GetValue(p));

        var prop = t.GetProperty("tileSize");
        if (prop != null && prop.PropertyType == typeof(float))
            return Mathf.Max(0.0001f, (float)prop.GetValue(p));

        return Mathf.Max(0.0001f, fallback);
    }

    static IEnumerator LiftTransCoroutine(Transform tr, float tile, float rise, float hang, float fall)
    {
        Vector3 start = tr.position;
        Vector3 upPos = start + Vector3.up * tile;

        // 상승
        float t = 0f;
        while (t < rise)
        {
            t += Time.deltaTime;
            tr.position = Vector3.Lerp(start, upPos, t / rise);
            yield return null;
        }
        tr.position = upPos;

        // 홀딩
        if (hang > 0f) yield return new WaitForSeconds(hang);

        // 낙하는 대상 PushableObjects 측에서 처리 X
        // 원위치로 하강
        t = 0f;
        while(t < fall)
        {
            t += Time.deltaTime;
            tr.position = Vector3.Lerp(upPos, start,t / fall);
            yield return null;
        }
        tr.position = start;
    }
}
