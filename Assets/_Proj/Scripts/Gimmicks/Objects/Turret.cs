using System.Security.Cryptography;
using UnityEngine;

[DisallowMultipleComponent]
public class Turret : MonoBehaviour, ISignalSender
{
    [Header("Detection Settings")]
    public float detectRadius = 4f;
    [Range(30f, 180f)] public float fov = 90f;
    public LayerMask targetMask;
    public LayerMask occluderMask;
    public bool useOcclusion = true;

    [Header("Signal Settings")]
    public ISignalReceiver Receiver { get; set; }

    [Header("Visual Range")]
    [SerializeField] private RingRange ring;
    [Tooltip("감지중일 때 색상")]
    public Color detectedColor;
    [Tooltip("기본 상태 색상")]
    public Color idleColor;

    private bool targetInside; // 현재 감지 중인지
    private float myYLevel; // 자기 층 높이
    private const float heightTolerance = 0.4f; // 층 오차 허용
    private bool doorShouldBeClosed; // 현재 문이 닫혀 있어야 하는지

    private const float ConnectionSearchRadius = 60f;

    void Start()
    {
        myYLevel = Mathf.Round(transform.position.y / 1f); // tileHeight 1기준
        AutoConnectReceiver();
        if (ring)
        {
            // 터렛이 움직이지 않는 고정 상태기 때문에 Start에서 한 번만 처리
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localRotation = Quaternion.identity;
            ring.visibleAngle = fov;
            ring.SetRadius(detectRadius);
            ring.fillColor = idleColor;
            ring.lineColor = idleColor;
            ring.RebuildAll();
            ring.lr.material.color = idleColor;
        }
    }

    void AutoConnectReceiver()
    {

        float searchRadius = ConnectionSearchRadius;
        Collider[] cols = Physics.OverlapSphere(transform.position, searchRadius, ~0);

        foreach (var c in cols)
        {
            var recv = c.GetComponentInParent<ISignalReceiver>();
            if (recv == null) continue;

            // DoorBlock만 연결 대상으로 필터링
            if (recv is DoorBlock door)
            {
                Receiver = recv;
                Debug.Log($"[Turret] {name} → {door.name} 자동 연결 완료 (거리 {Vector3.Distance(transform.position, door.transform.position):F1})");
                return;
            }
        }

        Debug.LogWarning($"[Turret] {name}: {searchRadius}m 안에 연결 가능한 Door를 찾지 못함");
    }
    void Update()
    {
        bool nowDetected = DetectTarget();

        // 감지 상태 변화만 처리
        if (nowDetected && !targetInside)
        {
            targetInside = true;
            if (!doorShouldBeClosed)
            {
                doorShouldBeClosed = true;
                Debug.Log("[Turret] Target detected -> Send CLOSE signal");
                SendSignal(); // Door 토글 -> 닫힘
            }
            UpdateRingColour(true);
        }
        // 감지 해제 -> 열기 명령 (문이 닫혀 있을 때만)
        else if (!nowDetected && targetInside)
        {
            targetInside = false;
            if (doorShouldBeClosed)
            {
                doorShouldBeClosed = false;
                Debug.Log("[Turret] Target lost -> Send OPEN signal");
                SendSignal(); // Door 토글 -> 열림
            }
            UpdateRingColour(false);
        }
    }

    void UpdateRingColour(bool detected)
    {
        if (!ring) return;
        ring.fillColor = detected ? detectedColor : idleColor;
        ring.lineColor = detected ? detectedColor : idleColor;
        ring.RebuildAll();
    }

    public void ConnectReceiver(ISignalReceiver receiver)
    {
        Receiver = receiver;
    }

    public void SendSignal()
    {
        if (Receiver != null)
            Receiver.ReceiveSignal();
    }

    bool DetectTarget()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, detectRadius, targetMask, QueryTriggerInteraction.Ignore);
        if (cols.Length == 0) return false;

        foreach (var c in cols)
        {
            // 같은 층인지 확인
            float targetY = Mathf.Round(c.transform.position.y / 1f);
            if (Mathf.Abs(targetY - myYLevel) > heightTolerance) continue;

            // 부채꼴(FOV) 각도 체크
            Vector3 dir = (c.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dir);
            if (angle > fov * 0.5f) continue;

            // 차폐 여부
            if (useOcclusion && IsLineBlocked(transform.position, c, occluderMask))
                continue;

            return true; // 감지 성공
        }
        return false;
    }

    bool IsLineBlocked(Vector3 origin, Collider target, LayerMask blockMask)
    {
        Vector3 dir = target.bounds.center - origin;
        float dist = dir.magnitude;
        if (dist < 0.01f) return false;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, blockMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != target)
                return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        Vector3 forward = transform.forward;
        float halfFov = fov * 0.5f;

        // 결 자동 검색 범위 시각화 (DoorBlock을 찾을 때 사용)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, ConnectionSearchRadius);

        // 주요 감지 구체 범위 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(position, detectRadius);

        // 시야각 왼쪽 경계 Ray
        Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFov, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * forward;
        Gizmos.DrawRay(position, leftRayDirection * detectRadius);

        // 시야각 오른쪽 경계 Ray
        Quaternion rightRayRotation = Quaternion.AngleAxis(halfFov, Vector3.up);
        Vector3 rightRayDirection = rightRayRotation * forward;
        Gizmos.DrawRay(position, rightRayDirection * detectRadius);

        // 시야각 중앙 Ray (감지 방향)
        Gizmos.DrawRay(position, forward * detectRadius);
    }
}
