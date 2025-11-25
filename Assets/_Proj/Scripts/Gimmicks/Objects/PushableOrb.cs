using System.Collections;
using UnityEngine;

// Shockwave 사용해서 버팔로와 같이 충격파 발생시킬 수 있어야 함.
// 버팔로와 같이 낙하 시점에 충격파를 발생시켜야 함. -> 감지탑에 충격파 발생 여부 전달
// 물체에 의해 막힐 수 있음.(차폐물 레이어 필요. 차폐 범위 계산 필요)
// 수직으로 떨어질 때만 충격파 발생시켜야 함. 수평 이동 시 충격파 발생되면 안 됨.
// 충격파 발생 shockwave.Fire();

[RequireComponent(typeof(Shockwave))]
public class PushableOrb : PushableObjects
{
    [SerializeField] private Shockwave shockwave;
    [SerializeField] private ShockPing shockPing;

    [Tooltip("Orb 자신이 충격파 발생시킬 수 있는 쿨타임")]
    public float orbCoolTime = 6f;
    private float lastShockwaveTime = -float.MaxValue;
    //private static Dictionary<int, float> floorCooldowns = new();

    [Header("Orb Fall Detection")]
    public float probeUp = 0.1f;
    [Min(0.6f)] public float probeDown = 0.6f;
    private bool wasGrounded;
    [Tooltip("SphereCast 반지름 (Orb의 반지름)")]
    public float sphereRadius = 0.35f;
    public LayerMask orbLandLayer;

    protected override void Awake()
    {
        base.Awake();
        allowSlope = false;
        shockwave = GetComponent<Shockwave>();
        shockPing = GetComponent<ShockPing>();
        wasGrounded = true;
    }

    void FixedUpdate()
    {
        Vector3 origin = transform.position + Vector3.up * probeUp * tileSize;
        float distance = probeDown * tileSize;

        bool grounded = Physics.SphereCast(
            origin: transform.position + transform.up * probeUp * tileSize,
            sphereRadius,
            Vector3.down,
            out RaycastHit hit,
            maxDistance: probeDown * tileSize,
            layerMask: orbLandLayer,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        // 이전에는 공중이었는데, 지금은 지상에 닿은 경우 = 착지 완료
        if (!wasGrounded && grounded)
        {
            Debug.Log($"[Orb] 충격파 발생 {name}", this);
            TryFireShockwave(); // 충격파 발생 시도
        }
        wasGrounded = grounded;
    }

    protected override bool IsImmuneToWaveLift()
    {
        return Time.time < lastShockwaveTime + orbCoolTime;
    }

    protected override void OnLanded()
    {
        isMoving = false;
        isFalling = false;

        if (Time.time - lastShockwaveTime < orbCoolTime)
            return;

        bool grounded = Physics.SphereCast(
            origin: transform.position + transform.up * probeUp * tileSize,
            sphereRadius,
            Vector3.down,
            out RaycastHit hit,
            maxDistance: probeDown * tileSize,
            layerMask: groundMask,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );
        if (!grounded || isMoving || isFalling)
            return;

        TryFireShockwave();
    }

    void TryFireShockwave()
    {
        if (shockwave == null) return;
        //int yFloor = Mathf.RoundToInt(transform.position.y / tileSize);

        float now = Time.time;

        //if (floorCooldowns.TryGetValue(yFloor, out var lastTime) && now - lastTime < orbCoolTime) return;

        if (now - lastShockwaveTime < orbCoolTime) return;

        //floorCooldowns[yFloor] = now;
        lastShockwaveTime = now;

        //Debug.Log($"[Orb] {yFloor}층 충격파 발생", this);
        Debug.Log($"[Orb] {name} 충격파 발생", this);
        //WaveLift(shockwave.riseSec, shockwave.hangSec, shockwave.fallSec);

        shockwave.Fire(
            origin: transform.position,
            tile: tileSize,
            radiusTiles: shockwave.radiusShock,
            targetLayer: shockwave.targetLayer,
            useOcclusion: shockwave.useOcclusion,
            occludeMask: shockwave.occluderMask,
            riseSeconds: shockwave.riseSec,
            hangSeconds: shockwave.hangSec,
            fallSeconds: shockwave.fallSec
        );

        // 감지탑 통지
        if (shockPing)
        {
            shockPing.PingTowers(transform.position);
            Debug.Log($"[Orb] 감지탑 Ping 전송 by {name}.", this);
        }
    }

    // Orb(Ironball)는 근처 물체가 lifting 중이더라도 그 밑으로 밀릴 수 없음
    protected override bool CheckBlocking(Vector3 target)
    {
        var b = boxCol.bounds;
        Vector3 half = b.extents - Vector3.one * 0.005f;
        Vector3 center = new Vector3(target.x, target.y + b.extents.y, target.z);

        // 규칙상 차단(blocking)
        if (Physics.CheckBox(center, half, transform.rotation, blockingMask, QueryTriggerInteraction.Ignore))
            return true;

        // 점유 차단(허용 레이어 제외)
        var hits = Physics.OverlapBox(center, half, transform.rotation, ~throughLayer, QueryTriggerInteraction.Ignore);
        foreach (var c in hits)
        {
            if (rb && c.attachedRigidbody == rb) continue; // 자기 자신
            if (c.transform.IsChildOf(transform)) continue; // 자식
            return true;
        }
        return false;
    }
}
