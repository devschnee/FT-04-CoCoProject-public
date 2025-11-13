using UnityEngine;

[DisallowMultipleComponent]
public class Turret : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectRadius = 4f;
    [Range(30f, 180f)] public float fov = 90f;
    public LayerMask targetMask;
    public LayerMask occluderMask;
    public bool useOcclusion = true;

    [Header("Visual Range")]
    [SerializeField] private RingRange ring;
    [Tooltip("감지중일 때 색상")]
    public Color detectedColor;
    [Tooltip("기본 상태 색상")]
    public Color idleColor;

    private bool targetInside; // 현재 감지 중인지
    private float myYLevel; // 자기 층 높이
    private const float heightTolerance = 0.3f; // 층 오차 허용
    private bool doorShouldBeClosed; // 현재 문이 닫혀 있어야 하는지

    void Start()
    {
        myYLevel = transform.position.y - 0.5f; // tileHeight 1기준
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

    //void ConnectReceiver()
    //{
    //    Vector3Int linkedPos = GetComponent<TurretBlock>().origin.property.linkedPos;
    //    Collider[] cols = Physics.OverlapBox(linkedPos, new(.2f, .2f, .2f));
    //    foreach (var c in cols)
    //    {
    //        if (c.transform.parent.name != "Stage") continue; //최상위 오브젝트만 감지
    //        var receiver = c.GetComponent<ISignalReceiver>();
    //        ConnectReceiver(receiver);
    //        print($"[{this.GetType()}]:[{this.name}] - [{receiver.GetType()}]:[{c.name}]");
    //        //if (receiver is DoorBlock door)
    //        //{
    //        //    Receiver = receiver;
    //        //    Debug.Log($"[Turret] {name} → {door.name} 자동 연결 완료 (거리 {Vector3.Distance(transform.position, door.transform.position):F1})");
    //        //    return;
    //        //}
    //    }
    //}
    //void AutoConnectReceiver()
    //{
    //    float searchRadius = 30f;
    //    Collider[] cols = Physics.OverlapSphere(transform.position, searchRadius, ~0);

    //    foreach (var c in cols)
    //    {
    //        var recv = c.GetComponentInParent<ISignalReceiver>();
    //        if (recv == null) continue;

    //        // DoorBlock만 연결 대상으로 필터링
    //        if (recv is DoorBlock door)
    //        {
    //            Receiver = recv;
    //            Debug.Log($"[Turret] {name} → {door.name} 자동 연결 완료 (거리 {Vector3.Distance(transform.position, door.transform.position):F1})");
    //            return;
    //        }
    //    }

    //    Debug.LogWarning($"[Turret] {name}: {searchRadius}m 안에 연결 가능한 Door를 찾지 못함");
    //}
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
                GetComponent<ISignalSender>().SendSignal(); // Door 토글 -> 닫힘
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
                GetComponent<ISignalSender>().SendSignal(); // Door 토글 -> 열림
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


    bool DetectTarget()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, detectRadius, targetMask, QueryTriggerInteraction.Ignore);
        if (cols.Length == 0) return false;

        foreach (var c in cols)
        {
            // 같은 층인지 확인
            float targetY = c.transform.position.y;
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

    private void OnDrawGizmos()
    {
        // 터렛의 위치 및 방향
        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward;
        float halfFov = fov * 0.5f;

        // 전체 탐지 구체 범위
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(pos, detectRadius);

        // 층 감지 높이 표시
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.25f);
        Vector3 lower = pos - Vector3.up * heightTolerance;
        Vector3 upper = pos + Vector3.up * heightTolerance;
        Gizmos.DrawWireCube(pos, new Vector3(detectRadius * 2f, heightTolerance * 2f, detectRadius * 2f));
        Gizmos.DrawLine(lower, upper);

        // 실제 감지 시야(FOV) 표시
        int segments = 32;
        Gizmos.color = Color.green;

        // FOV 경계면을 원호로 그림
        for (int i = 0; i < segments; i++)
        {
            float angleA = -halfFov + (fov / segments) * i;
            float angleB = -halfFov + (fov / segments) * (i + 1);

            Vector3 dirA = Quaternion.Euler(0f, angleA, 0f) * fwd;
            Vector3 dirB = Quaternion.Euler(0f, angleB, 0f) * fwd;

            Vector3 p1 = pos + dirA * detectRadius;
            Vector3 p2 = pos + dirB * detectRadius;

            // 원호
            Gizmos.DrawLine(p1, p2);
            // 중심선
            Gizmos.DrawLine(pos, p1);
        }

        // 실제 감지 부피(시야 원뿔) 표현
        // 반투명 MeshCone 효과 시각화
#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(0f, 1f, 0f, 0.1f);
        UnityEditor.Handles.DrawSolidArc(
            pos,
            Vector3.up,
            Quaternion.Euler(0f, -halfFov, 0f) * fwd,
            fov,
            detectRadius
        );
#endif
        // 현재 감지 구체 기준
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos - new Vector3(0, 0.5f, 0), detectRadius);
    }
}
