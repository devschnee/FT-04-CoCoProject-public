using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PushableObjects : MonoBehaviour, IPushHandler, IRider
{
    #region Variables
    public float moveTime = 0.12f;
    public float tileSize = 1f;
    [Tooltip("이동 막는 물체")]
    public LayerMask blockingMask;
    [Tooltip("땅 판정")]
    public LayerMask groundMask;
    [Tooltip("경사로")]
    public LayerMask slopeMask;
    [Tooltip("통과 허용 like water")]
    public LayerMask throughLayer;

    protected Rigidbody rb;
    protected bool isMoving = false;
    protected bool isHoling = false;
    protected bool isFalling = false;
    protected bool isRiding = false;
    public float requiredHoldtime = 0.6f;
    protected float currHold = 0f;
    protected Vector2Int holdDir;
    private const string STAGE_NAME = "Stage";

    public bool allowFall = true;
    public bool allowSlope = false;
    private BoxCollider boxCol;

    [Header("For Flow Water")]
    public bool IsMoving => isMoving;
    public bool IsFalling => isFalling;

    private static Dictionary<int, float> gloablShockImmunity = new();
    [Header("Min : [Iron Balls'] Lift Rising Time + Hold + Fall + 0.2f")]
    [Tooltip("충격파 맞은 오브젝트가 다시 반응하기까지 쿨타임")]
    public float immuneTime = 5f;

    [Header("Shockwave Lift Override [Activate this option to control each type]")]
    public bool overrideLiftTiming = false;
    [Tooltip("재정의 시 사용되는 상승 시간")]
    public float overrideRiseSec = 0.5f;
    [Tooltip("재정의 시 사용되는 홀딩 시간")]
    public float overrideHangSec = 0.2f;
    [Tooltip("재정의 시 사용되는 하강 시간")]
    public float overrideFallSec = 0.5f;

    #endregion

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        boxCol = GetComponent<BoxCollider>();
    }
    void Update()
    {
        if (!isHoling || isMoving || isRiding) return;

        // 밀고 있는 시간 누적
        currHold += Time.deltaTime;
        // 민 시간이 조건 이상이면 Push 시도
        if (currHold >= requiredHoldtime)
        {
            TryPush(holdDir);
            currHold = 0f;
            isHoling = false;
        }
    }

    // 밀기 시도(경사로 / 낙하 포함)
    public bool TryPush(Vector2Int dir)
    {
        if (isMoving || isFalling) return false;

        Vector3 offset = new Vector3(dir.x, 0f, dir.y) * tileSize;
        // XZ로만 이동할 목표(1칸) 먼저 계산.
        Vector3 target = transform.position + offset;

        // TODO : 경사로를 만나면 위로 1칸 -> 앞으로 2칸 이동하도록
        // 경사로가 있다면 위로 1칸 추가 이동
        if (allowSlope && Physics.Raycast(target + Vector3.up * 0.5f, Vector3.down, 1f, slopeMask))
        {
            Vector3 up = target + Vector3.up * tileSize;
            // 윗칸이 비어있고, 그 칸이 바닥이면 경사로 위로 이동
            if (!CheckBlocking(up) &&
                Physics.Raycast(up + Vector3.up * 0.1f, Vector3.down, 1.5f, groundMask))
            {
                StartCoroutine(MoveAndFall(up));
                return true;
            }
        }

        // 목적지에 뭔가 있으면 못 감
        if (CheckBlocking(target))
            return false;

        // 바닥 유무 확인
        bool hasGroundAtTarget = Physics.Raycast(target + Vector3.up * 0.1f, Vector3.down, 10f, groundMask);

        if (!hasGroundAtTarget)
        {
            // 타겟 바로 아래 칸에 뭔가 있으면 전진 금지
            Vector3 oneDown = target + Vector3.down * tileSize;
            if (CheckBlocking(oneDown))
                return false;

            // 실제 착지 위치를 미리 계산, 그 칸이 비어있는지도 확인
            if (Physics.Raycast(target + Vector3.up * 3f, Vector3.down, out var hit, 20f, groundMask))
            {
                // 착지 y를 칸 단위로 정규화
                Vector3 landing = new Vector3(target.x, Mathf.Floor(hit.point.y / tileSize) * tileSize, target.z);
                if (CheckBlocking(landing))
                    return false;
            }
            else
            {
                // 아래에 바닥 자체가 전혀 없다면, 낙하 허용 여부에 따라 결정
                if (!allowFall) return false;
                // 허용이면 그대로 진행 (낙하 연출로 사라지는/끝층까지 떨어지는 케이스)
            }
        }
        // 이동 후 낙하여부까지 처리
        StartCoroutine(MoveAndFall(target));
        return true;
    }

    // 모양에 맞는 충돌 검사 구현하도록
    //protected abstract bool CheckBlocking(Vector3 target);
    // NOTE : 11/5 Orb가 BoxCollider를 갖게 되면서 PuhsableObjects에 통합됨.
    protected virtual bool CheckBlocking(Vector3 target)
    {
        var b = boxCol.bounds;
        Vector3 half = b.extents - Vector3.one * 0.005f;
        Vector3 center = new Vector3(target.x, target.y + b.extents.y, target.z);

        // 규칙상 차단 (blocking)
        if (Physics.CheckBox(center, half, transform.rotation, blockingMask, QueryTriggerInteraction.Ignore))
            return true;

        // 점유 차단(허용 레이어 제외)
        var hits = Physics.OverlapBox(center, half, transform.rotation, ~throughLayer, QueryTriggerInteraction.Ignore);
        foreach (var c in hits)
        {
            //if ((groundMask.value & (1 << c.gameObject.layer)) != 0) continue;
            if (rb && c.attachedRigidbody == rb) continue; // 자기 자신
            if (c.transform.IsChildOf(transform)) continue; // 자식
            return true;
        }

        return false;
    }

    // 단순 이동(1칸 Lerp 이동)
    protected IEnumerator MoveTo(Vector3 target)
    {
        //이동 시작 순간 내 머리 위에 있는 콜라이더, 주변 콜라이더 전부 켜주기

        //이동 시작 전 내 주변 사방에 있는 타일들에게서 IEdgeColliderHandler 검출하여 캐싱
        List<IEdgeColliderHandler> startCache = new();
        if (TryGetComponent<IEdgeColliderHandler>(out var handler))
        {
            startCache = handler.DetectGrounds();
            handler.SetAllCollider();
            startCache.ForEach((x) => x.SetAllCollider());
        }
        for (int i = 0; i < 4; i++)
        {
            Vector3 checkDir = i == 0 ? transform.forward : i == 1 ? -transform.right : i == 2 ? -transform.forward : transform.right;
            Ray ray = new(transform.position - (Vector3.up * .49f), checkDir);
            var result = Physics.RaycastAll(ray, 1.4f, groundMask);
            foreach (var hit in result)
            {
                Debug.Log($"PushableObj: {name} moving start. hitted {hit.collider.name}");
                if (hit.collider.TryGetComponent<IEdgeColliderHandler>(out var targetHandler))
                {
                    startCache.Add(targetHandler);
                }
            }
        }

        isMoving = true;
        Vector3 start = transform.position;
        float elapsed = 0f;

        // 탑승 리스트(적층형)
        List<IRider> riders = new List<IRider>();

        Vector3 center = transform.position + Vector3.up * tileSize * 0.5f;
        Vector3 halfExtents = boxCol.size * 0.5f;

        // throughLayer는 제외하고 검사
        LayerMask riderMask = blockingMask & ~throughLayer;

        Collider[] riderHits = Physics.OverlapBox(center + Vector3.up * tileSize * 0.5f, halfExtents * .9f, transform.rotation, riderMask);
        Transform playerTransform = null;
        foreach (var hit in riderHits)
        {
            if (hit.gameObject != gameObject && hit.TryGetComponent<PushableObjects>(out var rider))
            {
                // Y좌표 검사: 바로 위에 있는 오브젝트인지 확인 (탑승 중인 오브젝트는 y + tileSize 위치)
                if (Mathf.Abs(rider.transform.position.y - (transform.position.y + tileSize)) < 0.01f)
                {
                    // 탑승자 감지 시 자식으로 설정
                    rider.OnStartRiding();
                    rider.transform.SetParent(this.transform);
                    riders.Add(rider);
                }
            }
            if (hit.TryGetComponent<PlayerMovement>(out PlayerMovement player))
            {
                print("########박스의 IRider로 playerMovement 검출했음.########");
                playerTransform = player.transform;
            }
        }

        
        while (elapsed < moveTime)
        {
           
            
            transform.position = Vector3.Lerp(start, target, elapsed / moveTime);
            elapsed += Time.deltaTime;


            if (playerTransform)
            {
                float yOffset = playerTransform.position.y - transform.position.y;
                playerTransform.position = Vector3.Lerp(playerTransform.position, transform.position + Vector3.up * yOffset, elapsed);
            }
            yield return null;
        }

        transform.position = target;

        isMoving = false;
        if (!IsOnFlowWater() && isRiding)
        {
            Debug.Log($"[PushableObjects] {name} : 물 밖으로 이동했으므로 OnStopRiding() 호출");
            OnStopRiding();
        }

        // 탑승 해제
        foreach (var rider in riders)
        {
            if (rider != null)
            {
                //rider.transform.SetParent(null);
                rider.OnStopRiding(); // OnStopRiding 내부에서 CheckFall()을 호출함
            }
        }
        riders.Clear();

        //중요: 한 프레임만 뒤에 실행시키기.
        
        yield return null;

        if (handler != null)
            //만약 내가 머리 위에 투명벽이 달린 객체라면?? 바꿔 말해, 내가 올라탈 수 있는 객체라면?
        {
            handler.DetectAndApplyFourEdge();
            handler.DetectGrounds().ForEach(x => x.DetectAndApplyFourEdge());
        }

        ////이동이 끝나고 나서 곧바로 내 주변 사방에 있는 타일에서 IEdgeColliderHandler 검출
        //for (int i = 0; i < 4; i++)
        //{
        //    Vector3 checkDir = i == 0 ? transform.forward : i == 1 ? -transform.right : i == 2 ? -transform.forward : transform.right;
        //    Ray ray = new(transform.position - (Vector3.up * .49f), checkDir);
        //    var result = Physics.RaycastAll(ray, 1.4f, groundMask);
        //    foreach(var hit in result)
        //    {
        //        Debug.Log($"PushableObj: {name} moved. hitted {hit.collider.name}");
        //        if (hit.collider.TryGetComponent<IEdgeColliderHandler>(out var targetHandler))
        //        {
        //            targetHandler.DetectAndApplyFourEdge();
        //        }
        //    }
        //}

        //이동이 끝나고 나서 캐싱해놨던 핸들러들도 Inspect(); 호출.
        foreach(var cached in startCache)
        {
            cached.DetectAndApplyFourEdge();
        }

        yield break;
    }

    protected IEnumerator MoveAndFall(Vector3 target)
    {
        yield return StartCoroutine(MoveTo(target));

        // 낙하 조건 확인 및 처리
        if (allowFall)
        {
            yield return StartCoroutine(CheckFall());
        }
    }

    public IEnumerator CheckFall()
    {
        if (isFalling) yield break; // .

        isFalling = true;
        Vector3 currPos = transform.position;
        bool fell = false; //.

        // Pushable도 땅으로 인식
        while (!Physics.BoxCast(
            currPos + Vector3.up * 0.3f,
            new Vector3(0.4f, 0.05f, 0.4f),
            Vector3.down,
            out _,
            Quaternion.identity,
            tileSize * 1.2f,
            groundMask))
        {
            Vector3 fallTarget = currPos + Vector3.down * tileSize;
            yield return StartCoroutine(MoveTo(fallTarget));
            currPos = transform.position;
            fell = true; //.
        }

        isFalling = false;
        if (fell) //.
            OnLanded();
    }


    // Push 시도 시작(방향 기억, 시간 누적)
    public void StartPushAttempt(Vector2Int dir)
    {
        if (isRiding || isMoving || isFalling) return;
        if (isHoling && dir != holdDir)
        {
            currHold = 0f;
        }

        holdDir = dir;
        isHoling = true;
    }

    // Push 중단 시 호출(시간 초기화)
    public void StopPushAttempt()
    {
        isHoling = false;
        currHold = 0f;
    }

    public void ImmediatePush(Vector2Int dir)
    {
        if (isMoving || isFalling) return;
        TryPush(dir);
        OnStopRiding();
    }

    protected virtual bool IsImmuneToWaveLift()
    {
        // 연속 튕김에 기본적으로 면역 없음. 필요 한 물체에 구현 해 줄 것.
        return false;
    }

    protected virtual void OnLanded() { }

    public void OnStartRiding()
    {
        isRiding = true;
        StartCoroutine(EstablishStackCoroutine());
        StartCoroutine(RidingCoroutine());
        isMoving = false;
        isHoling = false;
        currHold = 0f;
    }

    // Stack Coroutine For Flow Water
    IEnumerator EstablishStackCoroutine()
    {
        List<IRider> newRiders = new List<IRider>();

        Vector3 halfExtents = boxCol.size * 0.5f;

        // Rider 검사 영역을 현재 위치 + 1칸 위로 확장.
        Vector3 center = transform.position + Vector3.up * (halfExtents.y + tileSize * 0.5f);
        LayerMask riderMask = blockingMask & ~throughLayer;

        Collider[] riderHits = Physics.OverlapBox(
            center,
            halfExtents * .9f,
            transform.rotation,
            riderMask);

        foreach (var hit in riderHits)
        {
            // PlayerMovement는 OnStartRiding을 통해 자식으로 설정되지 않으므로, PushableObjects(Rider)만 확인
            if (hit.gameObject != gameObject && hit.TryGetComponent<PushableObjects>(out var rider))
            {
                if (Mathf.Abs(rider.transform.position.y - (transform.position.y + tileSize)) < 0.05f)
                {
                    rider.OnStartRiding();
                    rider.transform.SetParent(this.transform);
                    newRiders.Add(rider);
                }
            }
        }
        yield break;
    }


    IEnumerator RidingCoroutine()
    {
        Debug.Log($"PushableObj: 라이딩코루틴 실행");
        Transform originalParent = transform.parent; //null일 수도 있음. null이라면 부모 없도록 설정됨.
        List<IEdgeColliderHandler> myAdjHandlers = new();
        if (TryGetComponent<IEdgeColliderHandler>(out var myHandler))
        {
        Debug.Log($"PushableObj: myHandler 할당함.");
                myAdjHandlers = myHandler.DetectGrounds();
            while (isRiding)
            {
                //탑승 상태인 동안에 매 프레임마다 원래의 위치 인근 사방의 블록들이 스스로를 검사하도록 함. 좋은 처리는 아님.
                myAdjHandlers.ForEach((x) => x.DetectAndApplyFourEdge());
                yield return null;
            }
        //이동이 다 끝났음.
                yield return null;
                myHandler.DetectAndApplyFourEdge();

        //이번엔 이동이 끝난 위치에서의 내 인근 핸들러.
                myHandler.DetectGrounds().ForEach((x) => x.DetectAndApplyFourEdge());
        }
        //else yield break;하면 안 될거같다.
        //왜냐? 이게 달린 오브젝트가 IEdgeColliderHandler를 구현하지 않을 가능성 있음(예: 철구)
        //다만 그 경우엔 이 오브젝트 포함(애초에 머리 위에 투명콜라이더가 없음) 주변의 투명콜라이더를 재설정할 필요가 없음.
        //아래 조건으로 yield return하면 위의 if문에 걸리든 아니든 !isRiding까지 기다림.
        else yield return new WaitUntil(()=>!isRiding);

        transform.SetParent(originalParent);
    }

    public void OnStopRiding()
    {
        Debug.Log($"[OnStopRiding] called on {name}, parent = {(transform.parent ? transform.parent.name : "null")}");
        isRiding = false;
        Transform p = transform.parent;
        Transform stage = GameObject.Find(STAGE_NAME)?.transform;
        if (p != null)
        {
            transform.SetParent(stage, true);
        }

        StartCoroutine(CheckFall());
    }

    IEnumerator BreakStackCoroutine()
    {
        // Stack쌓듯이 반대로 내 부모의 부모의 부모 ... 를 찾아서 FlowWater를 감지하고 있지 않으면 chian을 끊어버리는 로직을 짜면 되지 않을까....
        // 내 조상중에 flow water안에 있는 obj를 찾아서...
        // if(flow water 찾았으면) Coroutine 실행 X
        // if(flow water 찾았으면) Coroutine 실행해서 체인 끊기
        // 아니면...지금 Flow에서 ImmediatePush를 호출해서 실행시켜주는데, flowInterval이 넘었는데도 ImmmidataPush가 호출이 안 되면 체인을 끊어주는 걸로...? <- 근데 이러면 그냥 가만히 두다가 다시 어떠한 방식으로 ImmediatePush가 호출되면 체인 이미 끊어져서 다시 안 따라갈 수도 있음.
        // 물에 들어간 상태면 Update에서 실시간으로 계속 물에 있는지 없는지 감지하고 물이 아니라면 감지 정지..? <- 너무 메모리 많이 잡아먹는 비효율적 방법같음...

        yield return null;
    }

    // ========== 공중 띄우기용 ==========
    // 충격파 맞았을 때 y+1 duration 동안 띄우기
    public void WaveLift(float shockRise, float shockHold, float shockFall)
    {
        int id = GetInstanceID();
        float now = Time.time;
        if (gloablShockImmunity.TryGetValue(id, out var lastTime))
        {
            if (now - lastTime < immuneTime)
            {
                return;
            }
        }
        gloablShockImmunity[id] = now;
        // box의 경우 충격파를 발생시키는 주체가 아니기 때문에 rise, hold, fall을 직접적으로 조정할 수 있도록 override변수를 추가
        float rise = overrideLiftTiming ? overrideRiseSec : shockRise;
        float hold = overrideLiftTiming ? overrideHangSec : shockHold;
        float fall = overrideLiftTiming ? overrideFallSec : shockFall;

        if (isMoving || isFalling || IsImmuneToWaveLift()) return;
        StartCoroutine(WaveLiftCoroutine(rise, hold, fall));
    }

    // 복귀
    IEnumerator WaveLiftCoroutine(float rise, float holdSec, float fall)
    {
        isFalling = true; //.
        isMoving = true;

        Vector3 start = transform.position;
        Vector3 target = start + Vector3.up * tileSize;

        rise = Mathf.Max(0.01f, rise);
        holdSec = Mathf.Max(0f, holdSec);
        fall = Mathf.Max(0.01f, fall);

        float t = 0f;
        while (t < rise)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, t / rise);
            yield return null;
        }
        transform.position = target;

        if (holdSec > 0f)
            yield return new WaitForSeconds(holdSec);

        t = 0f;
        while (t < fall)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(target, start, t / fall);
            yield return null;
        }
        transform.position = start;

        isMoving = false;
        isFalling = false; //.
        Debug.Log($"{name} 충격파 영향 받음");
        //yield break;
        // NOTE : 혹시나 뭔가 다른 작업을 하다 여기에서 CheckFall()을 할 일이 생긴다면 차라리 다른 스크립트를 작성하는 것을 권장. 원위치 복귀 후 다시 낙하 검사하는 실수 생기면 안 됨.
        // pushables가 충격파 받은 이후로 적층된 물체들이 원위치 후 다시 낙하 검사를 하게 되면 한 번 더 낙하해서 원위치에서 -y로 더 내려가게 됨
        StartCoroutine(CallFallCheck()); //.
    }

    //. 직접적인 대기 연결 고리 끊기 위해 WaveLiftCoroutine에서 분리
    IEnumerator CallFallCheck()
    {
        yield return null;
        if (allowFall)
        {
            yield return StartCoroutine(CheckFall());
        }
    }

    private bool IsOnFlowWater()
    {
        // 자신의 중심 기준 아래 방향으로 Raycast
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.5f))
        {
            return hit.collider.gameObject.layer == LayerMask.NameToLayer("Water");
        }
        return false;
    }

    private bool HasFlowWaterAncestor()
    {
        Transform current = transform;
        int safety = 0; // 무한루프 방지용

        while (current != null && safety < 20)
        {
            safety++;
            if (current.TryGetComponent(out PushableObjects po))
            {
                if (po.IsOnFlowWater())
                {
                    return true;
                }
            }
            current = current.parent;
        }

        // 위로 올라가도 FlowWater 위에 있는 부모가 없음 -> 해제 필요
        return false;
    }
}
