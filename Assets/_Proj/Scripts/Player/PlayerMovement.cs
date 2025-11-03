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
    public LayerMask slopeMask; // 경사로에서만 올라타기는 허용시키기 위해. PlayerSlope.cs와 역할이 다름.
                                // 살짝 겹치는 내용 있을 수 있어서 전체적으로 리팩터링 하면 좋긴 함.
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

        //Vector3 inputOffset = new(joystick.InputDir.x, 0, joystick.InputDir.z);
        //Ray ray = new(transform.position + (inputOffset * .3f), inputOffset);
        //RaycastHit[] results = new RaycastHit[10];

        //int hitnums = Physics.BoxCastNonAlloc(transform.position - Vector3.up * .3f + new Vector3(input.x, 0, input.y) * .2f, Vector3.one * .1f, Vector3.down, results);
        //print(hitnums);

        //if (hitnums < 2)






        //    input = Vector2.zero;

        //생각을 해봅시다...
        //내가 갈 곳에 땅바닥이 있는가? 를 판단하려면
        //내 발밑에서 내가 입력한 방향으로 조금 보낸 지점에서부터 입력 방향으로 0.5거리만큼 레이를 발사함.
        //콜라이더가 검출되면 => 다음 땅이 있다는 뜻.
        //다음 땅이 단순히 있다/없다를 떠나, 다음 땅의 x,z 인접 타일도 있는지 없는지 검사해야 함.


        //TODO: 모르겠다;
        //Vector3 roundPos = Vector3Int.RoundToInt(transform.position);
        //Ray ray = new Ray(roundPos, new(input.x,0, input.y));
        //if (!Physics.Raycast(ray, .5f, LayerMask.NameToLayer("Ground")))
        //{
        //    input = Vector2.zero;
        //}
        ////1. 이동할 방향의 땅 검사: 현재 나의 위치에서, 이동방향으로 .2f만큼 레이캐스트
        ////Ray groundcheck = new(transform.position - new Vector3(0f,.1f,0f), new Vector3(input.x, -.1f, input.y));

        ////if (!Physics.Raycast(groundcheck, .5f, 9))
        ////    input = Vector2.zero;

        //Ray ray = new(transform.position - Vector3.up * .1f, input);



        //RaycastHit[] hits = new RaycastHit[3];
        //if (Physics.RaycastNonAlloc(ray, hits, .2f, LayerMask.NameToLayer("Ground")) <= 2)
        //{
        //    if (Physics.BoxCastNonAlloc((transform.position - Vector3.up * .1f), Vector3.one * .05f, Vector3.down, hits) < 2)
        //    {
        //        input = Vector2.zero;
        //    }
        //}





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

        // 같은 y층 오브젝트 감지
        float halfTile = 0.5f;
        Vector3 boxCenter = nextPos + Vector3.up * 0.5f; // 플레이어 중심
        Vector3 halfExt = new Vector3(0.4f, 0.45f, 0.4f);
        Collider[] sameYHits = Physics.OverlapBox(boxCenter, halfExt, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);

        foreach (var col in sameYHits)
        {
            if (col.attachedRigidbody == rb) continue;
            if (col.isTrigger) continue;

            // 상대의 중심 y값이 같은 층에 있다면 이동 차단
            float dy = Mathf.Abs(col.bounds.center.y - rb.position.y);
            if (dy < halfTile && !Physics.Raycast(rb.position + Vector3.up * 0.1f, Vector3.down, 1.2f, slopeMask))
            {
                // 같은 높이 + 슬로프 아님 -> 이동 금지
                return;
            }
        }
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