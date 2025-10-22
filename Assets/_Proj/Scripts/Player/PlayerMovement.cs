using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    #region Variables
    [Header("Refs")]
    public Joystick joystick;
    public Rigidbody rb;

    // 캐릭터의 현재 이동 방향을 월드 좌표계에 맞게 변환하기 위해 필요
    private Transform camTr; // NOTE : 비워두면 자동으로 Camera.main을 사용

    [Header("Move")]
    public float moveSpeed = 3.0f;
    public float accel = 25f;
    public float rotateLerp = 10f;

    [Header("Push")]
    public float tileSize = 1f;
    public LayerMask pushable;
    public LayerMask blocking;
    public float frontOffset = 0.4f;
    public Vector3 probeHalfExtents = new Vector3(0.25f, 0.6f, 0.25f); // 앞면 검사 박스 크기

    [Header("Slope")]
    public float slopeLimitDeg = 45f;     // 허용 경사 각
    public float groundProbeRadius = 0.25f;
    public float groundProbeExtra = 0.1f; // 여유
    public LayerMask groundMask;          // 평지+경사 모두 포함

    [Header("Step")]
    public bool enableStepAssist = true;
    public float stepHeight = 0.3f;       // 올라설 수 있는 최대 턱 높이
    public float stepCheckDistance = 0.35f;
    #endregion

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        camTr = Camera.main != null ? Camera.main.transform : null;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;
    }
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (joystick == null) return;
        if (camTr == null) camTr = Camera.main?.transform;

        Vector2 input = new Vector2(joystick.InputDir.x, joystick.InputDir.z);

        if(input.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        Vector3 fwd = camTr ? camTr.forward : Vector3.forward;
        Vector3 right = camTr ? camTr.right : Vector3.right;
        fwd.y = 0;
        right.y = 0;
        fwd.Normalize();
        right.Normalize();

        // 최종 월드 이동 방향 계산
        // 조이스틱의 X를 카메라의 Right 방향에, Z를 카메라의 Forward 방향에 적용
        Vector3 moveDir = (right * input.x) + (fwd * input.y);
        if (moveDir.sqrMagnitude > 0.1f) moveDir.Normalize();

        // 경사 보정
        Vector3 finalDir = moveDir;
        if (TryGetGround(out var gnorm))
        {
            // 경사 한계 각도 내에서만 투영
            float slopeAngle = Vector3.Angle(gnorm, Vector3.up);
            if (slopeAngle <= slopeLimitDeg + 0.01f)
                finalDir = Vector3.ProjectOnPlane(moveDir, gnorm).normalized;
        }

        // 스텝(낮은 턱) 보조
        Vector3 addStep = Vector3.zero;
        if (finalDir.sqrMagnitude > 0.0001f && TryStepUp(finalDir, out var step))
            addStep = step;

        // 최종 이동
        Vector3 nextPos = rb.position + finalDir * (moveSpeed * Time.fixedDeltaTime) + addStep;
        rb.MovePosition(nextPos);

        // 회전은 그대로
        Quaternion targetRot = Quaternion.LookRotation(new Vector3(finalDir.x, 0, finalDir.z), Vector3.up);
        Quaternion smoothRot = Quaternion.Slerp(rb.rotation, targetRot, rotateLerp * Time.fixedDeltaTime);
        rb.MoveRotation(smoothRot);

        // 방향 벡터 계산 후, 이동 시도한 위치 앞에 푸시 가능한 오브젝트가 있으면
        Ray ray = new Ray(rb.position + Vector3.up * 0.5f, moveDir);
        if (Physics.Raycast(ray, out RaycastHit hit, 1.1f, pushable))
        {
            if (hit.collider.TryGetComponent<PushableObjects>(out var pushableObj))
            {
                Vector2Int dir = To4Direction(moveDir);
                pushableObj.StartPushAttempt(dir);
            }
        }
        else
        {
            foreach(var obj in FindObjectsOfType<PushableObjects>())
            {
                obj.StopPushAttempt();
            }
        }

    }

    bool TryGetGround(out Vector3 groundNormal)
    {
        // 플레이어 콜라이더 기준으로 아래 측정
        groundNormal = Vector3.up;

        var pos = rb.position + Vector3.up * (groundProbeRadius + groundProbeExtra);
        float castDist = groundProbeRadius + groundProbeExtra * 2f;

        if (Physics.SphereCast(pos, groundProbeRadius, Vector3.down, out RaycastHit hit,
            castDist, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundNormal = hit.normal;
            return true;
        }
        return false;
    }
    Vector2Int To4Direction(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
            return dir.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            return dir.z > 0 ? Vector2Int.up : Vector2Int.down;
    }

    bool TryStepUp(Vector3 moveDir, out Vector3 stepOffset)
    {
        stepOffset = Vector3.zero;
        if (!enableStepAssist || stepHeight <= 0f) return false;

        // 낮은 턱(벽)에 막혔는지 전방 레이로 체크
        Vector3 originLow = rb.position + Vector3.up * 0.05f;
        Vector3 originHigh = rb.position + Vector3.up * (stepHeight + 0.05f);

        if (Physics.Raycast(originLow, moveDir, out RaycastHit hitLow, stepCheckDistance, blocking))
        {
            // 같은 지점에서 높은 위치로 다시 쏴서 위가 비어있으면 올라설 수 있음
            bool upperBlocked = Physics.Raycast(originHigh, moveDir, stepCheckDistance, blocking);
            if (!upperBlocked)
            {
                // 살짝 위로 올려줌
                stepOffset = Vector3.up * (Mathf.Clamp(stepHeight, 0.01f, 0.5f));
                return true;
            }
        }
        return false;
    }
}