using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    // 10/27 기획안 변경됨.
    // TODO : 플레이어 낭떨어지 막힘. 경사로를 통해서만 y칸 오르내릴 수 있음. 동물친구 (일단은 거북이 제외) 올라타면 안 됨. 1초에 n칸 전진하도록 수정.
    #region Variables
    [Header("Refs")]
    public Joystick joystick;
    public Rigidbody rb;
    public List<IMoveStrategy> moveStrategies;

    // 캐릭터의 현재 이동 방향을 월드 좌표계에 맞게 변환하기 위해 필요
    private Transform camTr; // NOTE : 비워두면 자동으로 Camera.main을 사용

    [Header("Move")]
    public float moveSpeed = 3.0f;
    public float accel = 25f;
    public float rotateLerp = 10f;
    #endregion

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        camTr = Camera.main != null ? Camera.main.transform : null;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;

        // Strategy Pattern: 시작 시 전략 컴포넌트들을 가져옴
        moveStrategies = new List<IMoveStrategy>(GetComponents<IMoveStrategy>());
    }

    void FixedUpdate()
    {
        if (joystick == null) return;
        if (camTr == null) camTr = Camera.main?.transform;

        Vector2 input = new Vector2(joystick.InputDir.x, joystick.InputDir.z);

        if (input.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

            // Strategy Pattern: 입력이 0일 때도 Push 전략이 StopPushAttempt를 호출하도록 실행. 
            if (moveStrategies != null)
            {
                foreach (var strategy in moveStrategies)
                    strategy.Execute(Vector3.zero, rb, this);
                // 입력 없음 상태도 전략이 받아야 함. 그래서 zero로라도 실행.
            }

            return;
        }

        Vector3 fwd = camTr ? camTr.forward : Vector3.forward;
        Vector3 right = camTr ? camTr.right : Vector3.right;
        fwd.y = 0;
        right.y = 0;
        fwd.Normalize();
        right.Normalize();

        Vector3 moveDir = (right * input.x) + (fwd * input.y);
        if (moveDir.sqrMagnitude > 0.1f) moveDir.Normalize();

        Vector3 finalDir = moveDir;
        Vector3 stepOffset = Vector3.zero;

        // NOTE: Strategy Pattern 적용 - 모든 상호작용을 전략에게 위임
        if (moveStrategies != null)
        {
            // 전략은 순서대로 실행되며, 이전 전략이 반환한 finalDir를 다음 전략에 전달
            foreach (var strategy in moveStrategies)
            {
                Vector3 currentDir = finalDir;
                Vector3 currentOffset = stepOffset;

                (Vector3 newDir, Vector3 newOffset) = strategy.Execute(currentDir, rb, this);

                // 경사 보정 전략은 finalDir를 변경 (ProjectOnPlane)
                // 스텝 전략은 stepOffset을 변경
                finalDir = newDir;
                stepOffset += newOffset;
            }
        }

        // 위치 이동
        Vector3 nextPos = rb.position + finalDir * (moveSpeed * Time.fixedDeltaTime) + stepOffset;
        rb.MovePosition(nextPos);
         
        // 회전 처리
        Quaternion targetRot = Quaternion.LookRotation(new Vector3(finalDir.x, 0, finalDir.z), Vector3.up);
        Quaternion smoothRot = Quaternion.Slerp(rb.rotation, targetRot, rotateLerp * Time.fixedDeltaTime);
        rb.MoveRotation(smoothRot);
    }

    public Vector2Int To4Dir(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
            return dir.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            return dir.z > 0 ? Vector2Int.up : Vector2Int.down;
    }
}