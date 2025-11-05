using UnityEngine;

public class PlayerSlope : MonoBehaviour, IMoveStrategy
{
    [Header("Slope Settings")]
    public float slopeLimitDeg = 45f;
    public float groundProbeRadius = 0.25f;
    public float groundProbeExtra = 0.1f;
    public LayerMask groundMask;

    public (Vector3, Vector3) Execute(Vector3 moveDir, Rigidbody rb, PlayerMovement player)
    {
        // 입력이 없으면 보정도 X
        if (moveDir.sqrMagnitude < 0.0001f) return (moveDir, Vector3.zero);

        Vector3 finalDir = moveDir;

        if (TryGetGround(rb, out var gnorm))
        {
            // 경사 한계 각도 내에서만 투영
            float slopeAngle = Vector3.Angle(gnorm, Vector3.up);
            if (slopeAngle <= slopeLimitDeg + 0.01f)
            {
                // 보정된 이동 방향
                finalDir = Vector3.ProjectOnPlane(moveDir, gnorm).normalized;
            }
        }

        return (finalDir, Vector3.zero);
    }

    bool TryGetGround(Rigidbody rb, out Vector3 groundNormal)
    {
        groundNormal = Vector3.up; // 기본 바닥 노멀은 위쪽

        // 구체 캐스트 시작 위치는 rigidbody 중심보다 살짝 위.
        // Collider와 지면이 너무 가까우면 캐스트가 지면에 걸리거나 false반환할 수 있기 때문. 확실히 콜라이더 위에서 시작하게 만들기 위함.
        float castDist = groundProbeRadius + groundProbeExtra * 2f; 
        
        // 캐스트 길이 설정(반지름 + 여유거리 * 2). 시작 위치를 높여놨기 때문에 거리도 더 확보.
        var pos = rb.position + Vector3.up * (groundProbeRadius + groundProbeExtra); 

        // 지면 탐색
        if (Physics.SphereCast(pos, groundProbeRadius, Vector3.down, out RaycastHit hit,
            castDist, groundMask, QueryTriggerInteraction.Ignore))
        {
            // 바닥 감지 성공 시 노멀 정보 반환
            groundNormal = hit.normal;
            return true;
        }
        return false; // 바닥 없음
    }
}
