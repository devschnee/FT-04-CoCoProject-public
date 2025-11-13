using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Water;

// 10/29 TODO : 카메라 세팅 후 팝업 UI 조정 76라인
// NOTE : 추후 필요 시 -> Boar가 Turtle을 탑승할 수 있게 된다면, IRider를 구현해줘야 함.
// 탑승하지 못하지만 위에 쌓인 박스는 밀게 하고 싶다면 돌진 로직에서 ground에 감지되는 것이 Turtle이라면 밀고 나서 Boar도 한 칸 이동하는 이동로직을 안 하도록 분기해줘야 함.
public class Boar : PushableObjects, IDashDirection, IPlayerFinder
{
    [Header("Canvas")]
    [SerializeField] GameObject btnGroup;
    [SerializeField] Button up;
    [SerializeField] Button down;
    [SerializeField] Button left;
    [SerializeField] Button right;
    [SerializeField] Image typeIcon; // 그냥 무슨 스킬인지 알려주는 이미지용임.

    [Header("Player Detection")]
    public float detectRadius = 3f; // 플레이어 감지 범위
    private Transform playerTrans; // 감지된 플레이어의 Transform

    [Header("Boar Dash Settings")]
    public LayerMask pushableLayer;
    [Tooltip("돌진 속도를 조정하려면 여기를 수정")]
    public float dashSpeed = 0.08f; // 돌진 속도
    public float rotateLerp = 10f;

    [Header("HitStop")]
    public bool useGlobalTimeScale = true; // 전체 일시정지(0.06s)로 타격감
    [Range(0f, 0.2f)]
    public float hitstopSeconds = 0.06f;

    Transform IPlayerFinder.Player { get => playerTrans; set => playerTrans = value; }

    //public ParticleSystem hitFx;
    Flow flowWater;


    protected override void Awake()
    {
        base.Awake();

        // 버튼 그룹의 방향 버튼에 돌진 로직 연결
        up.onClick.AddListener(() => { GetDirection(new Vector2Int(0, 1)); btnGroup.SetActive(false); });
        down.onClick.AddListener(() => { GetDirection(new Vector2Int(0, -1)); btnGroup.SetActive(false); });
        left.onClick.AddListener(() => { GetDirection(new Vector2Int(-1, 0)); btnGroup.SetActive(false); });
        right.onClick.AddListener(() => { GetDirection(new Vector2Int(1, 0)); btnGroup.SetActive(false); });

        btnGroup.SetActive(false);

        // 플레이어 transform 찾기
        // NOTE : 플레이어 Tag를 Player로 설정
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO) playerTrans = playerGO.transform;
    }

    // Boar가 FlowWater위에 들어간 PushableObjects를 Flow Interval 시간보다 먼저 밀어서 오류 사항을 만들어내지 않기 위해 requiredTime을 조정하기 위함
    void Start()
    {
        if (flowWater == null)
        {
            flowWater = FindAnyObjectByType<Flow>(FindObjectsInactive.Include);
        }
    }

    void Update()
    {
        if ((isHoling || !isMoving) && flowWater != null)
        {
            currHold += Time.deltaTime;
            float holdThreshold = Mathf.Max(requiredHoldtime, flowWater.flowInterval + 1f);
            if (currHold >= holdThreshold)
            {
                TryPush(holdDir);
                currHold = 0f;
                isHoling = false;
            }
        }
        DetectPlayer();
    }

    private void LateUpdate()
    {
        // 멧돼지(부모)의 회전에 상관없이 UI가 월드 축에 고정되게 유지
        if (btnGroup) btnGroup.transform.rotation = Quaternion.Euler(60f, 0, 0);
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

        // 감지 기준 위치
        Vector3 checkOrigin = transform.position + Vector3.up * 1f;

        // 플레이어까지의 거리 체크
        float distance = Vector3.Distance(checkOrigin, playerTrans.position);

        // 플레이어가 범위 안에 있을 때만 팝업 켜줌.
        bool shouldBeActive = distance <= detectRadius;
        if (btnGroup.activeSelf != shouldBeActive)
        {
            btnGroup.SetActive(shouldBeActive);
        }
    }

    public void GetDirection(Vector2Int dashDir)
    {
        if (isMoving || isFalling) return;

        Vector3 moveDir = new Vector3(dashDir.x, 0, dashDir.y);
        StartCoroutine(DashCoroutine(moveDir, dashDir));

    }

    #region Dash
    // 이동 및 회전(한 칸 이동)
    protected IEnumerator DashMoveTo(Vector3 target, Vector3 moveDir)
    {
        Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);

        isMoving = true;
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < dashSpeed)
        {
            // 회전 및 이동
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateLerp * Time.deltaTime);
            transform.position = Vector3.Lerp(start, target, elapsed / dashSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 최종 위치, 회전값 설정
        transform.rotation = targetRot;
        transform.position = target;
        isMoving = false;
    }

    // ==== 한 칸 이동을 반복되게 처리, 충돌 및 밀기 처리 ====
    private IEnumerator DashCoroutine(Vector3 moveDir, Vector2Int dashDir)
    {
        isMoving = true;
        btnGroup.SetActive(false);

        // 돌진 시작 시 회전
        Quaternion initTargetRot = Quaternion.LookRotation(moveDir, Vector3.up);
        float rotateTime = 0.1f;
        float rotElapsed = 0f;
        Quaternion startRot = transform.rotation;

        while (rotElapsed < rotateTime)
        {
            rotElapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, initTargetRot, rotElapsed / rotateTime);
            yield return null;
        }
        yield return null;

        // 멧돼지가 위치한 층 계산(PushableObjects 수집 기준)
        float baseY = Mathf.Floor(transform.position.y / tileSize + 1e-4f);
        int stepGuard = 0;

        // === 돌진 ===
        while (true)
        {
            stepGuard++;

            Vector3 currentPos = transform.position;
            Vector3 nextPos = currentPos + moveDir * tileSize;
            Vector3 boxCenter = nextPos + Vector3.up * 0.5f;
            //Vector3 halfExt = new Vector3(0.45f, 0.6f, 0.45f); // 충돌 검사용 박스 크기
            Vector3 halfExt = new Vector3(0.2f, 0.2f, 0.2f); // 충돌 검사용 박스 크기

            // blocking 우선 검사(다음 칸에 뭐 있는지)
            Collider[] blockHits = Physics.OverlapBox(boxCenter, halfExt, Quaternion.identity, blockingMask);

            bool hasBlocking = false;

            foreach (var h in blockHits)
            {
                if (!h || h.isTrigger) continue;
                if (h.transform == transform) continue;
                hasBlocking = true;
                break;
            }

            if (hasBlocking)
            {
                Debug.Log($"[Boar.DashCoroutine] 다음 칸({nextPos})에 Blocking 오브젝트 감지. 돌진 정지.");
                transform.position = currentPos;
                HitStop(gameObject);
                isMoving = false;
                yield break;
            }

            // pushables 감지
            RaycastHit hit;
            bool hasPushable = Physics.BoxCast(
              currentPos + Vector3.up * 0.5f, // 시작 위치
                      halfExt * 0.9f, // 충돌체 크기
                      moveDir, // 이동 방향
                      out hit, // 충돌 정보
                      Quaternion.identity,
              tileSize * 1.1f, // 검사 거리
                      pushableLayer
            );

            if (hasPushable)
            {
                Debug.Log($"[Boar.DashCoroutine] Pushable 오브젝트 감지. 충돌 대상: {hit.collider.name}. 체인 밀기 로직 시작.");
                // Pushable 오브젝트 감지되면 밀기 실행
                PushableObjects headPush = hit.collider?.GetComponent<PushableObjects>();

                // 같은 층 기준 스택 수집
                List<PushableObjects> vstack = CollectVerticalStack(headPush.transform.position, baseY);
                Debug.Log($"[Boar.DashCoroutine] 충돌 지점의 수직 스택 수: {vstack.Count}");

                // 연속된 Pushable 체인 전체 수집. 체인 뒤에 blocking 없는지 확인
                if (!CollectChain(headPush.transform.position, dashDir, baseY, out var chainStacks, out Vector3 tailNextWorld))
                {
                    // 막히면 정지
                    Debug.Log("[Boar.DashCoroutine] 체인 밀기 실패 (체인 중간 Blocking 또는 Pushable 체인 아님). 돌진 정지.");
                    HitStop(vstack[0].gameObject);
                    isMoving = false;
                    break;
                }

                Debug.Log($"[Boar.DashCoroutine] 체인 수집 성공. 체인 길이: {chainStacks.Count}개 스택.");

                Vector3 checkTailPos = tailNextWorld;
                float tailY = Mathf.Floor(tailNextWorld.y / tileSize + 1e-4f) * tileSize;
                checkTailPos.y = tailY;

                // 꼬리 다음칸(이동할 위치에) blocking 있는지 검사
                bool tailBlocked = Physics.CheckBox(
                checkTailPos + Vector3.up * 0.5f,
                halfExt,
                Quaternion.identity,
                 blockingMask
                );

                if (tailBlocked)
                {
                    // 막히면 정지
                    Debug.Log($"[Boar.DashCoroutine] 체인 꼬리 다음 칸({tailNextWorld}) Blocking 감지. 돌진 정지.");
                    HitStop(vstack[0].gameObject);
                    isMoving = false;
                    break;
                }

                Debug.Log("[Boar.DashCoroutine] 체인 밀기(ChainShiftOneCell) 시작.");
                // 밀기

                yield return StartCoroutine(ChainShiftOneCell(chainStacks, dashDir));


                // 돼지 전진(밀고 나서 자신도 한 칸 전진)
                var posAfterPush = transform.position + new Vector3(dashDir.x, 0, dashDir.y);
                yield return StartCoroutine(DashMoveTo(posAfterPush, new Vector3(dashDir.x, 0, dashDir.y)));

                HitStop(vstack[0].gameObject);

                // 낙하 처리 (꼬리부터)
                for (int i = chainStacks.Count - 1; i >= 0; i--)
                {
                    foreach (var p in chainStacks[i])
                        yield return StartCoroutine(p.CheckFall());
                }
                chainStacks.Clear();
                break;
            }

            if (!HasGround(nextPos))
            {
                Debug.Log($"[Boar.DashCoroutine] 다음 칸({nextPos}) 바닥 없음 감지. 돌진 정지 및 낙하 검사 시작.");
                // 다음 칸 바닥 검사
                isMoving = false;
                yield break;
            }
            yield return StartCoroutine(DashMoveTo(nextPos, moveDir));
        }
        isMoving = false;
    }
    #endregion

    #region Stack Chain
    // 수펑 위치 기준으로 수직으로 쌓인 PushableObjects 수집
    List<PushableObjects> CollectVerticalStack(Vector3 baseWorldPos, float yFloor)
    {
        List<PushableObjects> stack = new List<PushableObjects>();
        //Vector3 cursor = baseWorldPos;

        Vector3 startPos = new Vector3(baseWorldPos.x, yFloor * tileSize - 1, baseWorldPos.z);
        Debug.Log($"[Boar.CollectVerticalStack] 호출. 기준 위치: {baseWorldPos}, Y층: {yFloor}");

        // 최대 높이(일단 10층)까지 탐색
        for (int i = 0; i < 10; i++)
        {
            Vector3 checkPos = startPos + Vector3.up * (0.5f + i * tileSize);

            var cols = Physics.OverlapBox(
              checkPos,
              new Vector3(0.45f, 0.5f, 0.45f),
              Quaternion.identity,
              pushableLayer,
              QueryTriggerInteraction.Collide
            );

            PushableObjects push = null;
            foreach (var c in cols)
            {
                if (c == null || c.isTrigger) continue;
                if (!c.TryGetComponent(out PushableObjects p)) continue;

                // 층 비교
                float pushY = Mathf.Floor(p.transform.position.y / tileSize + 1e-4f);
                if (pushY + 1e-4f < Mathf.Floor(transform.position.y / tileSize + 1e-4f)) continue; // 보어 아래층이면 무시

                push = p;
                break;
            }

            if (push != null)
            {
                stack.Add(push);
            }
            else
            {
                // 현재 칸에 Pushable이 없으면 더 이상 위에도 없을 것으로 가정하고 종료
                break;
            }
        }
        return stack;
    }

    // 연속된 PushableObjects 체인 수평 방향 수집, 체인 중간에 blocking 없는지 검사
    bool CollectChain(Vector3 headWorldPos, Vector2Int dir, float yFloor, out List<List<PushableObjects>> chainOfStacks, out Vector3 tailNextWorld)
    {
        chainOfStacks = new List<List<PushableObjects>>();
        tailNextWorld = Vector3.zero;

        Vector3 step = new Vector3(dir.x, 0, dir.y) * tileSize;
        Vector3 cursor = headWorldPos;

        while (true)
        {
            // CollectVerticalStack을 사용하여 현재 칸의 수직 스택을 가져옴
            List<PushableObjects> verticalStack = CollectVerticalStack(new Vector3(cursor.x, 0, cursor.z), yFloor);

            if (verticalStack.Count == 0)
            {
                // 현재 칸이 비었거나 Pushable이 없다면
                if (chainOfStacks.Count == 0)
                {
                    // headWorldPos에서부터 Pushable을 못 찾으면 실패
                    return false;
                }
                else
                {
                    // 체인 끝. 꼬리 뒤 칸은 cursor가 됨
                    tailNextWorld = new Vector3(cursor.x, yFloor * tileSize, cursor.z);
                    return true;
                }
            }

            // Pushable이 아닌 충돌체(blocking)를 검사
            bool nonPushBlocking = false;
            var cols = Physics.OverlapBox(
              cursor + Vector3.up * 0.5f,
              new Vector3(0.45f, 0.5f, 0.45f),
              Quaternion.identity,
              blockingMask, // blockingLayer만 체크
                      QueryTriggerInteraction.Collide
            );

            foreach (var c in cols)
            {
                if (c == null || c.isTrigger) continue;
                // blockingLayer에 포함된 충돌체가 있으면 밀기 실패
                if (((1 << c.gameObject.layer) & blockingMask.value) != 0)
                {
                    nonPushBlocking = true;
                    break;
                }
            }

            if (nonPushBlocking)
            {
                // 체인 중간에 뭐 있으면 밀기 실패
                return false;
            }

            // pushable 체인에 수직 스택 누적
            chainOfStacks.Add(verticalStack);
            // 다음 칸으로 이동
            cursor += step;
        }
    }
    #endregion

    #region Move Pushables
    // 연속된 pushables를 동시에 1칸 이동시키는 메서드
    IEnumerator ChainShiftOneCell(List<List<PushableObjects>> chainOfStacks, Vector2Int dir)
    {
        int totalObjects = 0;
        foreach (var stack in chainOfStacks)
            totalObjects += stack.Count;

        if (totalObjects == 0)
        {
            yield break;
        }

        // 모든 PushableObject를 단일 리스트로 모으고, 충돌 무시를 설정
        List<PushableObjects> allChainObjects = new List<PushableObjects>(totalObjects);
        foreach (var stack in chainOfStacks)
            allChainObjects.AddRange(stack);

        //HACK: 강욱 - 1110: 모인 모든 PushableObject 에서 IEdgeColliderHandler를 한곳에 모아 보관, 그리고 그 인접 블록도 보관.
        List<IEdgeColliderHandler> startingIEdgeColliderHandlers = new();
        foreach (var po in allChainObjects)
        {
            //if (e is IEdgeColliderHandler startingHandler) //이러면 좋겠지만 따로 TryGetComponent<>해야 함.
            if (po.TryGetComponent<IEdgeColliderHandler>(out IEdgeColliderHandler startingHandler))
            {
                //startingIEdgeColliderHandlers.Add(startingHandler); //검출된 핸들러를 넣고,
                startingIEdgeColliderHandlers.AddRange(startingHandler.DetectGrounds()); //검출된 핸들러 사방의 핸들러를 추가로 넣음
            }

        }

        int n = allChainObjects.Count;
        var collLists = new List<Collider[]>(n);
        for (int i = 0; i < n; ++i)
            collLists.Add(allChainObjects[i].GetComponentsInChildren<Collider>(true));

        // 체인 내 모든 오브젝트 간의 충돌 잠시 무시
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
            startPos[i] = allChainObjects[i].transform.position;
            targetPos[i] = startPos[i] + step;
        }

        // 동시에 1칸 이동
        float dur = Mathf.Max(0.05f, dashSpeed);
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            for (int i = 0; i < n; ++i)
                allChainObjects[i].transform.position = Vector3.Lerp(startPos[i], targetPos[i], k);
            yield return null;
        }
        for (int i = 0; i < n; ++i)
            allChainObjects[i].transform.position = targetPos[i];

        // 충돌 원복
        for (int i = 0; i < n; ++i)
            for (int j = i + 1; j < n; ++j)
                foreach (var a in collLists[i])
                    foreach (var b in collLists[j])
                        if (a && b) Physics.IgnoreCollision(a, b, false);

        //이동이 끝났으므로 저장해놨던 IEdgeColliderHandlers가 각각 검사.
        foreach (var handler in startingIEdgeColliderHandlers)
        {
            //이 핸들러는 시작 시점에 저장되어있던 모든 투명벽을 가진 블록들을 의미함.
            handler.DetectAndApplyFourEdge();

        }

        foreach (var po in allChainObjects)
        {
            if (po.TryGetComponent<IEdgeColliderHandler>(out var handler))
            {
                //이 핸들러는 체인된 오브젝트(함께 밀린 모든 오브젝트)에서 검출한 투명벽 핸들러를 의미함.
                //해당 핸들러의 투명벽 재설정
                handler.DetectAndApplyFourEdge();
                //해당 핸들러 사방의 객체의 투명벽도 재설정
                handler.DetectGrounds().ForEach(x => x.DetectAndApplyFourEdge());
            }
        }
        // NOTE: 낙하 검사는 DashCoroutine에서 한 번에 처리
    }
    #endregion

    void HitStop(GameObject hit = null)
    {
        // 나중에 이펙트도 넣으려나? 안 넣으면 이거 그냥 계속 주석처리 하면 됨.
        //if (hitFx)
        //{
        //    // 이펙트 위치를 충돌지점 근처로
        //    hitFx.transform.position = hit ? hit.transform.position : transform.position;
        //    hitFx.Play();
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
        bool isBlocked = Physics.CheckBox(target + Vector3.up * 0.5f, Vector3.one * 0.4f, Quaternion.identity, blockingMask, QueryTriggerInteraction.Collide);
         Debug.Log($"[Boar.CheckBlocking] 위치 {target}에서 블로킹 검사 결과: {isBlocked}");
        return isBlocked;
    }

    bool HasGround(Vector3 worldPos)
    {
        float half = Mathf.Min(0.45f, tileSize * 0.45f);
        Vector3 halfExt = new Vector3(half, 0.05f, half);

        float castDist = tileSize /* * 1.25f*/;
        Vector3 origin = worldPos + Vector3.up * (tileSize * 0.5f);

        bool hasGround = Physics.BoxCast(
          origin,
          halfExt,
          Vector3.down,
          out _,
          Quaternion.identity,
          castDist,
          groundMask,
          QueryTriggerInteraction.Collide
        );
         Debug.Log($"[Boar.HasGround] 위치 {worldPos}에서 바닥 검사 결과: {hasGround}");
        return hasGround;
    }

    //void OnTriggerEnter(Collider other)
    //{
    //    Vector3 moveDir = Vector3.up * 0.5f;
    //    Vector3 currPos = transform.position;
    //    Vector3 nextPos = currPos + moveDir * tileSize;


    //    StartCoroutine(DashMoveTo(nextPos, moveDir));

    //}

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // OverlapBox
        Vector3 dir = transform.forward;
        Vector3 nextPos = transform.position + dir * tileSize;
        Vector3 boxCenter = nextPos + Vector3.up * 0.5f;
        Vector3 halfExt = new Vector3(0.45f, 0.6f, 0.45f);

        // OverlapBox (blocking 검사 영역)
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(boxCenter, halfExt * 2f);

        // Boar 위치
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, halfExt * 2f);

        // 방향 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, boxCenter);
    }
#endif
}