using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Buffalo : MonoBehaviour
{
    [Header("Timer & Jump")]
    [Tooltip("버튼 누른 뒤 실행까지 대기 시간")]
    public float interactionSeconds = 0.1f;
    [Tooltip("충격 전 점프 연출에 걸리는 시간")]
    public float jumpDuration = 0.35f;
    [Tooltip("점프 높이 곡선 0~1 비율")]
    public AnimationCurve jumpY = AnimationCurve.EaseInOut(0, 0, 1, 0.5f);

    [Header("Shockwave Range (circle)")]
    [Tooltip("중요!! : 여기 바뀌면 RingRange 컴포넌트의 범위도 같이 바꿔줘야 함!!!")]
    public float radiusShock = 5f; // 충격파 반경
    public float tileHeight = 1f; // 한 칸 높이
    [Tooltip("충격파 영향 받는 Pushables가 포함된 레이어")]
    public LayerMask targetLayer;

    [Header("Lift Timing")]
    [Tooltip("떠오르는 데 걸리는 시간")]
    public float riseSecondsPerTile = 0.5f;
    [Tooltip("공중에서 홀딩되는 시간")]
    public float hangSeconds = 0.5f;

    // HACK : 이건 따로 빼서 차폐물 자체에 붙여 주는 형식으로 변경하는 것이 나을 듯
    [Header("Blocking")]
    [Tooltip("충격파 차폐 될 지 여부")]
    public bool useBlocking = false;
    [Tooltip("차폐 인정할 레이어")]
    public LayerMask occluderMask = ~0;

    [Header("Visual")]
    public RingRange ring;
    public Button interactionBtn;

    [Header("Player Detection")]
    public float detectRadius = 3f; // 플레이어 감지 범위
    public LayerMask playerLayer; // 플레이어 레이어 마스크
    private Transform playerTrans; // 감지된 플레이어의 Transform

    bool running;

    void Awake()
    {
        interactionBtn.gameObject.SetActive(false);
        ring.gameObject.SetActive(false);

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerTrans = playerGO.transform;
        }
    }

    void Start()
    {
        if (ring == null) ring = GetComponentInChildren<RingRange>(true);
    }

    void Update()
    {
        DetectPlayer();
    }

    void LateUpdate()
    {
        if (interactionBtn != null)
        {
            // World Rotation을 Quaternion.identity(X=0, Y=0, Z=0)로 설정
            interactionBtn.transform.rotation = Quaternion.identity;
        }
    }

    // 플레이어 감지(거리 기반)
    void DetectPlayer()
    {
        if (playerTrans == null) return;
        if (interactionBtn.gameObject.activeSelf)
        {
            interactionBtn.gameObject.SetActive(false);
            ring.gameObject.SetActive(false);
        }
        
        Vector3 checkOrigin = transform.position + Vector3.up * 0.5f;

        // 플레이어까지의 거리 체크
        float distance = Vector3.Distance(checkOrigin, playerTrans.position);

        if (distance <= detectRadius) // 범위 안에 들어옴
        {
            if (!interactionBtn.gameObject.activeSelf)
            {
                interactionBtn.gameObject.SetActive(true);
                ring.gameObject.SetActive(true);
            }
        }
        else // 범위에서 나감
        {
            if (interactionBtn.gameObject.activeSelf)
            {
                interactionBtn.gameObject.SetActive(false);
                ring.gameObject.SetActive(false);
            }
        }
    }

    public void Interact()
    {
        Debug.Log("[BuffaloShockwave] Interact called");
        if (!running) StartCoroutine(WaveRunCoroutine());
    }
    void OnMouseDown() => Interact();

    IEnumerator WaveRunCoroutine()
    {
        running = true;

        float tile = Mathf.Max(0.01f, tileHeight);

        // 타이머
        float t = 0f;
        while (t < interactionSeconds) { t += Time.deltaTime; yield return null; }

        // 점프
        yield return StartCoroutine(JumpCoroutine(tile));

        // 충격파
        DoShockwave(tile);

        running = false;
    }

    IEnumerator JumpCoroutine(float tile)
    {
        var p0 = transform.position; // 시작 위치
        float t = 0f;
        while (t < jumpDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / jumpDuration); // 0~1 정규화 진행도
            float addY = jumpY.Evaluate(u) * tile; // 실제 상승량
            transform.position = new Vector3(p0.x, p0.y + addY, p0.z);
            yield return null;
        }
        transform.position = p0; // 끝나면 원위치
    }

    void DoShockwave(float tile)
    {
        float radiusWorld = radiusShock * tile;
        int centerH = Mathf.FloorToInt(transform.position.y / tile + 1e-4f);

        var cols = Physics.OverlapSphere(transform.position, radiusWorld, targetLayer, QueryTriggerInteraction.Ignore);
        if (cols == null || cols.Length == 0) return;

        // PushableObjects / PushableBox / PushableOrb 중 같은 높이만 추출
        var pushables = new List<Component>();
        foreach (var c in cols)
        {
            var p = (Component)c.GetComponent("PushableObjects")
                  ?? (Component)c.GetComponent("PushableBox")
                  ?? (Component)c.GetComponent("PushableOrb");
            if (p == null) continue;

            float ts = GetTileSize(p, tile);
            // 바닥 높이 기준
            int h = Mathf.FloorToInt(p.transform.position.y / ts + 1e-4f);
            if (h != centerH) continue;

            // NOTE : 차폐 체크를 켠 경우, 버팔로~대상 사이에 고정O/통과X 물체가 있으면 제외
            if (useBlocking && IsBlocked(transform.position, p.transform.position, centerH, tile))
                continue;

            pushables.Add(p);
        }
        if (pushables.Count == 0) return;

        // XZ 칸으로 스택 묶어서 전부 띄우기
        var groups = pushables.GroupBy(p =>
        {
            float ts = GetTileSize(p, tile);
            int gx = Mathf.FloorToInt((p.transform.position.x + 0.5f * ts) / ts);
            int gz = Mathf.FloorToInt((p.transform.position.z + 0.5f * ts) / ts);
            return (gx, gz, ts);
        });

        foreach (var g in groups)
        {
            var stack = g.ToList();

            if (HasFixedAboveAny(stack, 1)) continue;

            foreach (var p in stack)
            {
                float ts = GetTileSize(p, tile);
                float rise = Mathf.Max(0.01f, riseSecondsPerTile);
                float hang = Mathf.Max(0f, hangSeconds);

                if (!TryCallLiftAny(p, rise, hang))
                {
                    StartCoroutine(LiftTransCoroutine(p.transform, ts, rise, hang));
                }
            }
        }
    }

    bool TryCallLiftAny(Component target, float rise, float hang)
    {
        string methodName = "WaveLift"; // 지원 메서드 이름

        var t = target.GetType();

        // 메서드를 직접 검색
        var methods = t.GetMethods().Where(m => m.Name == methodName);

        foreach (var m in methods) // 검색된 메서드 루프 돌기
        {
            var pars = m.GetParameters();
            object[] args = null;

            // 2개 인자(duration, hold)
            if (pars.Length == 2)
                args = new object[] { ConvertArg(pars[0], rise), ConvertArg(pars[1], hang) };
            // 1개 인자(duration만)
            else if (pars.Length == 1)
                args = new object[] { ConvertArg(pars[0], rise) };
            // 0개 인자면 그대로 호출
            else if (pars.Length == 0)
                args = null;
            else
                continue; // 지원하지 않는 시그니처

            try { m.Invoke(target, args); return true; }
            catch { /* 시그니처 불일치 시 다음 후보 시도 */ }
        }
        return false;
    }

    object ConvertArg(System.Reflection.ParameterInfo pi, float value)
    {
        // int 파라미터로 선언된 경우에도 대응
        if (pi.ParameterType == typeof(int)) return Mathf.RoundToInt(value);
        if (pi.ParameterType == typeof(float)) return value;
        // 다른 타입이면 기본값 사용
        return null;
    }

    float GetTileSize(Component p, float fallback)
    {
        // 대상 필드에 tileSize 있으면 그 값 사용 없으면 fallback
        var t = p.GetType();
        var f = t.GetField("tileSize");
        if (f != null && f.FieldType == typeof(float)) return Mathf.Max(0.01f, (float)f.GetValue(p));
        var prop = t.GetProperty("tileSize");
        if (prop != null && prop.PropertyType == typeof(float)) return Mathf.Max(0.01f, (float)prop.GetValue(p));
        return Mathf.Max(0.01f, fallback);
    }

    IEnumerator LiftTransCoroutine(Transform tr, float tile, float rise, float hang)
    {
        Vector3 from = tr.position, to = from + Vector3.up * tile;

        float t = 0f;
        while (t < rise)
        {
            t += Time.deltaTime;
            tr.position = Vector3.Lerp(from, to, t / rise);
            yield return null;
        }
        tr.position = to;

        if (hang > 0f) yield return new WaitForSeconds(hang);
        // NOTE : 낙하는 Pushable 쪽에서 처리하므로 여기선 유지
    }

    bool HasFixedAboveAny(List<Component> stack, int upTiles)
    {
        // 스택 중 하나라도 윗칸에 고정 & 통과X 오브젝트 있으면 true
        foreach (var p in stack)
        {
            float ts = GetTileSize(p, tileHeight);
            Vector3 center = p.transform.position + Vector3.up * (upTiles * ts);
            float r = ts * 0.25f; // 교차 확인용 작은 탐지 반경

            var cols = Physics.OverlapSphere(center, r, ~0, QueryTriggerInteraction.Ignore);
            foreach (var c in cols)
            {
                var so = c.GetComponent<ShockwaveObject>();
                if (so != null && so.isFixed && !so.passThrough) return true;
            }
        }
        return false;
    }

    bool IsBlocked(Vector3 from, Vector3 to, int centerH, float tile)
    {
        // 같은 Y에서 쏴서 차폐물 있으면 true
        float y = centerH * tile + tile * 0.5f;
        Vector3 a = new(from.x, y, from.z);
        Vector3 b = new(to.x, y, to.z);
        Vector3 dir = b - a;
        float dist = dir.magnitude; if (dist <= 0.0001f) return false; dir /= dist;

        var hits = Physics.RaycastAll(a, dir, dist, occluderMask, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            var so = h.collider.GetComponent<ShockwaveObject>();
            if (so != null && so.isFixed && !so.passThrough) return true;
        }
        return false;
    }
}
