using UnityEngine;

public class EdgeGuard : MonoBehaviour, IMoveStrategy
{
    [Header("Grid / Footprint")]
    public float tileSize = 1f;
    [Tooltip("발바닥 박스 가로/세로. 타일보다 살짝 작게")]
    public Vector2 footprintSize = new Vector2(0.6f, 0.6f);
    [Tooltip("지면을 찾기 위해 아래로 내리는 높이")]
    public float probeHeight = 0.8f;
    [Tooltip("지면과 겹치는 최소 콜라이더 개수(0이면 바로 낭떠러지로 판단)")]
    public int minSupportContacts = 1;

    [Header("Layers")]
    public LayerMask groundMask;  // 평지/플랫폼
    public LayerMask slopeMask;   // 경사면

    private Collider[] _hits = new Collider[8];

    public (Vector3, Vector3) Execute(Vector3 inputDir, Rigidbody rb, PlayerMovement ctx)
    {
        // 입력 없으면 패스
        if (inputDir.sqrMagnitude < 1e-6f)
            return (inputDir, Vector3.zero);

        // 다음 위치 예측
        float step = ctx.moveSpeed * Time.fixedDeltaTime;
        Vector3 nextPos = rb.position + inputDir.normalized * step;

        // 아래 체크 (평지)
        bool hasGround = HasSupportUnder(nextPos);

        if (!hasGround)
        {
            // 경사 예외 허용
            bool hasSlope = HasSlopeUnder(nextPos);
            if (!hasSlope)
            {
                // 낭떠러지 -> 이동 차단
                return (Vector3.zero, Vector3.zero);
            }
        }
        return (inputDir, Vector3.zero);
    }

    bool HasSupportUnder(Vector3 worldPos)
    {
        // OverlapBox 중심을 살짝 위에 두고 아래로 내린 범위를 커버
        Vector3 center = new Vector3(worldPos.x, worldPos.y - probeHeight * 0.5f, worldPos.z);
        Vector3 halfExt = new Vector3(footprintSize.x * 0.5f, probeHeight * 0.5f, footprintSize.y * 0.5f);

        int count = Physics.OverlapBoxNonAlloc(center, halfExt, _hits, Quaternion.identity, groundMask, QueryTriggerInteraction.Ignore);
        return count >= minSupportContacts;
    }

    bool HasSlopeUnder(Vector3 worldPos)
    {
        Vector3 origin = worldPos + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out var hit, 2f, slopeMask, QueryTriggerInteraction.Ignore))
            return true;

        // 경사도 타원/사다리꼴이면 OverlapBox로도 한 번 더 체크
        Vector3 center = new Vector3(worldPos.x, worldPos.y - probeHeight * 0.5f, worldPos.z);
        Vector3 halfExt = new Vector3(footprintSize.x * 0.5f, probeHeight * 0.5f, footprintSize.y * 0.5f);
        int count = Physics.OverlapBoxNonAlloc(center, halfExt, _hits, Quaternion.identity, slopeMask, QueryTriggerInteraction.Ignore);
        return count > 0;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 디버그: 발판 박스 표시
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position + Vector3.down * (probeHeight * 0.5f);
        Vector3 size = new Vector3(footprintSize.x, probeHeight, footprintSize.y);
        Gizmos.DrawCube(center, size);
    }
#endif
}
