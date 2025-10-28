using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Boar : PushableObjects, IDashDirection
{
    [Header("Canvas")]
    [SerializeField] GameObject btnGroup;
    [SerializeField] Button up;
    [SerializeField] Button down;
    [SerializeField] Button left;
    [SerializeField] Button right;
    [SerializeField] Image typeIcon;

    [Header("Player Detection")]
    public float detectRadius = 3f; // 플레이어 감지 범위
    public LayerMask playerLayer; // 플레이어 레이어 마스크
    private Transform playerTrans; // 감지된 플레이어의 Transform
    
    [Header("Boar Dash Settings")]
    private LayerMask collisionMask;
    public LayerMask pushableLayer;
    [Tooltip("돌진 속도를 조정하려면 여기를 수정")]
    public float dashSpeed = 0.08f; // 돌진 속도
    public float rotateLerp = 10f;
    //[SerializeField] float gridYawDeg = 45f; // 45도 다이아 격자로 사용하려고 할 경우 필요

    [Header("HitStop")]
    public bool useGlobalTimeScale = true; // 전체 일시정지(0.06s)로 타격감
    [Range(0f, 0.2f)]
    public float hitstopSeconds = 0.06f;
    //public ParticleSystem hitFx;


    protected override void Awake()
    {
        base.Awake();
        collisionMask = blockingMask | pushableLayer;

        // 버튼 그룹의 방향 버튼에 돌진 로직 연결
        up.onClick.AddListener(() => { GetDirection(new Vector2Int(0, 1)); btnGroup.SetActive(false); });
        down.onClick.AddListener(() => { GetDirection(new Vector2Int(0, -1)); btnGroup.SetActive(false); });
        left.onClick.AddListener(() => { GetDirection(new Vector2Int(-1, 0)); btnGroup.SetActive(false); });
        right.onClick.AddListener(() => { GetDirection(new Vector2Int(1, 0)); btnGroup.SetActive(false); });

        btnGroup.SetActive(false);
        
        // 플레이어 transform 찾기
        // NOTE : 플레이어 Tag를 Player로 설정
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerTrans = playerGO.transform;
        }
    }

    void Update()
    {
        if (isHoling && !isMoving)
        {
            currHold += Time.deltaTime;
            if (currHold >= requiredHoldtime)
            {
                TryPush(holdDir);
                currHold = 0f;
                isHoling = false;
            }
        }       
        DetectPlayer();
    }

    // HACK : 카메라 세팅에 따라 카메라 정면을 조금 따라가도록 수정하는 것이 나은 방법일 수도 있음. 추후 joystick, camera 변경 후 다시 생각.
    private void LateUpdate()
    {
        // 멧돼지(부모)의 회전에 상관없이 UI가 월드 축에 고정되게 유지
        if (btnGroup != null)
        {
            // World Rotation을 Quaternion.identity(X=0, Y=0, Z=0)로 설정
            btnGroup.transform.rotation = Quaternion.identity;
        }
    }

    // 플레이어 감지
    void DetectPlayer()
    {
        if (playerTrans == null) return;
        if (isMoving || isFalling)
        {
            if (btnGroup.activeSelf)
            {
                btnGroup.SetActive(false);
            }
            return;
        }
        Vector3 checkOrigin = transform.position + Vector3.up * 1f;

        // 플레이어까지의 거리 체크
        float distance = Vector3.Distance(checkOrigin, playerTrans.position);

        if (distance <= detectRadius) // 범위 안에 들어옴
        {
            if (!btnGroup.activeSelf)
            {
                btnGroup.SetActive(true);
            }
        }
        else // 범위에서 나감
        {
            if (btnGroup.activeSelf)
            {
                btnGroup.SetActive(false);
            }
        }
    }

    public void GetDirection(Vector2Int dashDir)
    {
        if (isMoving || isFalling) return;

        // NOTE : 카메라만 돌리는 게 아니라 타일 자체를 마름모 격자 스타일로 사용하려고 하면 아래 주석을 해제하고 테스트 시도.
        //var q = Quaternion.Euler(0f, gridYawDeg, 0f);
        //Vector3 gx = q * Vector3.right; // 회전된 X axis
        //Vector3 gz = q * Vector3.forward; // 회전된 Z axis
        //Vector3 moveDir = gx * dashDir.x + gz * dashDir.y; ; // 회전 축으로 방향 합성

        Vector3 moveDir = new Vector3(dashDir.x, 0, dashDir.y); // 위 주석 켜면 이 줄 삭제
        StartCoroutine(DashCoroutine(moveDir, dashDir));

    }

    protected IEnumerator DashMoveTo(Vector3 target, Vector3 moveDir)
    {
        Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);

        isMoving = true;
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < dashSpeed)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation,targetRot, rotateLerp * Time.deltaTime);
            transform.position = Vector3.Lerp(start, target, elapsed / dashSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = targetRot;
        transform.position = target;
        isMoving = false;
    }

    private IEnumerator DashCoroutine(Vector3 moveDir, Vector2Int dashDir)
    {
        isMoving = true;
        btnGroup.SetActive(false);

        Quaternion initTargetRot = Quaternion.LookRotation(moveDir, Vector3.up);
        float rotateTime = 0.1f;
        float rotElapsed = 0f;
        Quaternion startRot = transform.rotation;

        while(rotElapsed < rotateTime)
        {
            rotElapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, initTargetRot, rotElapsed / rotateTime);
            yield return null;
        }
        while (true)
        {
            Vector3 currentPos = transform.position;
            Vector3 nextPos = currentPos + moveDir * tileSize;

            // 다음 칸에 무언가 있는지 (block + pushable 모두 체크 해야 함)
            var hits = Physics.OverlapBox(nextPos + Vector3.up * 0.5f, Vector3.one * 0.4f, Quaternion.identity, collisionMask, QueryTriggerInteraction.Collide);

            if (hits.Length == 0)
            {
                // 완전 비었으면 전진
                yield return StartCoroutine(DashMoveTo(nextPos, moveDir));
                continue;
            }

            // Pushable이 하나라도 있나?
            bool foundPushable = false;
            Collider pushHead = null;
            foreach (var h in hits)
            {
                if (h == null || h.isTrigger) continue;
                if (h.TryGetComponent<IPushHandler>(out _)) { foundPushable = true; pushHead = h; break; }
            }

            if (foundPushable)
            {
                // head 위치에서 연속 체인 수집
                if (!CollectChain(pushHead.transform.position, dashDir, out var chain, out Vector3 tailNextWorld))
                {
                    // 체인 뒤가 막혀서 못 밈
                    break;
                }

                // 연속적인 오브젝트의 꼬리 뒤 칸이 비어있고 바닥 OK인지 확인 (통과X/푸시블 없어야 함)
                bool tailBlocked = Physics.CheckBox(
                    tailNextWorld + Vector3.up * 0.5f,
                    new Vector3(0.45f, 0.5f, 0.45f),
                    Quaternion.identity,
                    collisionMask, // block + pushables
                    QueryTriggerInteraction.Collide
                );

                bool tailHasGround = Physics.Raycast(
                    tailNextWorld + Vector3.up * 0.1f,
                    Vector3.down,
                    1.5f,
                    groundMask, // PushableObjects에서 가져오게 됨
                    QueryTriggerInteraction.Ignore
                );

                if (tailBlocked || !tailHasGround)
                {
                    // 뒤가 막혀 있으면 밀기 실패
                    HitStop(pushHead.gameObject);
                    break;
                }

                // 연속된 모든 pushables를 동시에 1칸 이동
                yield return StartCoroutine(ChainShiftOneCell(chain, dashDir));

                // 멧돼지는 보어는 전진하지 않고 멈춤
                HitStop(pushHead.gameObject);
                break;
            }

            // pushables는 없고 다른 막는 물체(벽/고정O)만 있음
            HitStop(null);
            break;
        }
        isMoving = false;
        StartCoroutine(CheckFall());
    }

    // 연속된 PushableObjects 체인 수집
    bool CollectChain(Vector3 headWorldPos, Vector2Int dir, out System.Collections.Generic.List<PushableObjects> chain, out Vector3 tailNextWorld)
    {
        chain = new System.Collections.Generic.List<PushableObjects>();
        tailNextWorld = Vector3.zero;

        Vector3 step = new Vector3(dir.x, 0, dir.y) * tileSize;
        Vector3 cursor = headWorldPos;

        while (true)
        {
            // cursor 위치에 있는 물체(첫 반복은 head 위치)
            var cols = Physics.OverlapBox(
                cursor + Vector3.up * 0.5f,
                new Vector3(0.45f, 0.5f, 0.45f),
                Quaternion.identity,
                collisionMask, // block + pushable
                QueryTriggerInteraction.Collide
            );

            // 이 칸에 PushableObjects가 있어야 체인에 포함
            PushableObjects push = null;
            bool nonPushBlocking = false;

            foreach (var c in cols)
            {
                if (c == null || c.isTrigger) continue;
                // pushable 레이어가 지정돼 있으면 레이어 필터
                if (pushableLayer.value != 0 && (pushableLayer.value & (1 << c.gameObject.layer)) == 0)
                {
                    // 푸시 대상 아님(다른 블록)
                    nonPushBlocking = true;
                    break;
                }
                if (c.TryGetComponent(out PushableObjects p))
                {
                    push = p;
                    // 같은 칸에 다른 충돌체가 더 있더라도, pushable이 1개라도 있으면 우선 push로 취급
                    break;
                }
                else
                {
                    // 비푸시블 충돌체가 있으면 막힘. 체인 종료.
                    nonPushBlocking = true;
                    break;
                }
            }

            if (push == null)
            {
                // 현재 칸이 비었거나(첫 반복 아닐 수도) pushable이 아니라면
                if (chain.Count == 0)
                {
                    // headWorldPos에서부터 push를 못 찾으면 실패
                    return false;
                }
                else
                {
                    // 체인 끝. 꼬리 뒤 칸은 cursor가 됨
                    tailNextWorld = cursor;
                    return true;
                }
            }

            if (nonPushBlocking)
            {
                return false;
            }

            // pushable 체인에 누적
            chain.Add(push);
            // 다음 칸으로 이동
            cursor += step;
        }
    }

    // 연속된 pushables를 동시에 1칸 이동시키는 메서드
    IEnumerator ChainShiftOneCell(System.Collections.Generic.List<PushableObjects> chain, Vector2Int dir)
    {
        int n = chain.Count;
        if (n == 0) yield break;

        // 서로간 충돌 잠시 무시(체인 끼리만)
        var collLists = new System.Collections.Generic.List<Collider[]>(n);
        for (int i = 0; i < n; ++i)
            collLists.Add(chain[i].GetComponentsInChildren<Collider>(true));
        for (int i = 0; i < n; ++i)
            for (int j = i + 1; j < n; ++j)
                foreach (var a in collLists[i])
                    foreach (var b in collLists[j])
                        if (a && b) Physics.IgnoreCollision(a, b, true);

        // 타깃 좌표 계산
        Vector3 step = new Vector3(dir.x, 0, dir.y) * tileSize;
        var startPos = new Vector3[n];
        var targetPos = new Vector3[n];
        for (int i = 0; i < n; ++i)
        {
            startPos[i] = chain[i].transform.position;
            targetPos[i] = startPos[i] + step;
        }

        // 동시에 1칸
        float dur = Mathf.Max(0.05f, dashSpeed); // 멧돼지의 dashSpeed와 비슷하게
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            for (int i = 0; i < n; ++i)
                chain[i].transform.position = Vector3.Lerp(startPos[i], targetPos[i], k);
            yield return null;
        }
        for (int i = 0; i < n; ++i)
            chain[i].transform.position = targetPos[i];

        // 충돌 원복
        for (int i = 0; i < n; ++i)
            for (int j = i + 1; j < n; ++j)
                foreach (var a in collLists[i])
                    foreach (var b in collLists[j])
                        if (a && b) Physics.IgnoreCollision(a, b, false);

        // NOTE : 낙하/정착 처리는 PushableObjects 쪽 루틴(체크/코루틴)에서 자연히 수행될 것.
    }

    void HitStop(GameObject hit = null)
    {
        // 나중에 이펙트도 넣으려나? 안 넣으면 이거 그냥 계속 주석처리 하면 됨.
        //if (hitFx)
        //{
        //    // 이펙트 위치를 충돌지점 근처로
        //    hitFx.transform.position = hit ? hit.transform.position : transform.position;
        //    hitFx.Play();
        //}
        StartCoroutine(HitStopCo());
    }

    IEnumerator HitStopCo()
    {
        if (hitstopSeconds <= 0f) yield break;

        if (useGlobalTimeScale)
        {
            float prev = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(hitstopSeconds);
            Time.timeScale = prev;
        }
        else
        {
            // 글로벌 정지 X, 로컬 비정규 시간 대기
            float t = 0f;
            while (t < hitstopSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
    protected override bool CheckBlocking(Vector3 target)
    {
        // 멧돼지 크기에 맞는 충돌체 검사
        return Physics.CheckBox(target + Vector3.up * 0.5f, Vector3.one * 0.4f, Quaternion.identity, blockingMask);
    }
}