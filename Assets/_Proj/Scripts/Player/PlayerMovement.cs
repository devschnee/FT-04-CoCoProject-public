using System;
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

        //Vector3 vel = moveDir * moveSpeed;
        //vel.y = rb.linearVelocity.y;
        //rb.linearVelocity = vel;

        // 그리드/타일 기반에서는 MovePosition이 적합
        Vector3 nextPos = rb.position + moveDir * (moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(nextPos);

        Quaternion targetRot = Quaternion.LookRotation(new Vector3(moveDir.x, 0, moveDir.z), Vector3.up);
        Quaternion smoothRot = Quaternion.Slerp(rb.rotation, targetRot, rotateLerp * Time.fixedDeltaTime);
        rb.MoveRotation(smoothRot);

        TryPushOnce(moveDir);
    }

    private void TryPushOnce(Vector3 moveDir)
    {
        // 전방 탐지. Trigger 무시
        if (moveDir.sqrMagnitude < 1e-4f) return;

        Vector3 flatDir = new Vector3(moveDir.x, 0f, moveDir.z).normalized;
        Vector3 boxCenter = rb.worldCenterOfMass + flatDir * frontOffset;
        Quaternion boxRot = Quaternion.identity;

        Collider[] hits = Physics.OverlapBox(
        boxCenter, probeHalfExtents, boxRot,
        pushable, QueryTriggerInteraction.Ignore
    );
        if (hits == null || hits.Length == 0) return;

        // 가장 가까운 상자 하나만 선택
        Collider best = hits[0];
        float bestDist = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            float d = (hits[i].attachedRigidbody ?
                       (hits[i].attachedRigidbody.worldCenterOfMass - rb.worldCenterOfMass).sqrMagnitude :
                       (hits[i].bounds.center - rb.worldCenterOfMass).sqrMagnitude);
            if (d < bestDist) { bestDist = d; best = hits[i]; }
        }
        if (!best.TryGetComponent<PushableObjects>(out var crate)) return;
        if (crate.IsMoving) return;

        Vector3 pushAxis;
        if (Mathf.Abs(flatDir.x) >= Mathf.Abs(flatDir.z))
            pushAxis = new Vector3(Mathf.Sign(flatDir.x), 0f, 0f);
        else
            pushAxis = new Vector3(0f, 0f, Mathf.Sign(flatDir.z));

        Vector3 currCell = crate.Snap(crate.transform.position);
        Vector3 nextCell = currCell + pushAxis * tileSize;

        if (IsCellBlocked(nextCell))
        {
            Debug.Log("[Push] blocked next cell.");
            return;
        }

        crate.SlideOneCell(nextCell);
    }

    bool IsCellBlocked(Vector3 cellCenter)
    {
        float half = tileSize * 0.45f;
        Vector3 halfExt = new Vector3(half, tileSize * 0.6f, half);
        return Physics.CheckBox(
            cellCenter, halfExt, Quaternion.identity,
            blocking, QueryTriggerInteraction.Ignore
        );
    }
}