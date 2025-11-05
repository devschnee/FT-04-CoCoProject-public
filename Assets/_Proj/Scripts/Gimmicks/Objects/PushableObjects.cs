using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PushableObjects : MonoBehaviour, IPushHandler, IRider
{
    // 10/27 기획안 변경됨.
    /* 
     * TODO : 박스, 구체 통과 못하는 객체로 막혀 있는 게 아니라면 밀려날 수 있음. 
     * 박스, 철구는 한 칸 떴다 떨어지고, 이 과정에서 철구는 충격파 발생시키는 로직이 추가 되어야 함.
     * 발생된 충격파에 의해서 다시 공중으로 뜨거나 하는 과정은 없음.
     */
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
    public float requiredHoldtime = 0.9f;
    protected float currHold = 0f;
    protected Vector2Int holdDir;

    public bool allowFall = true;
    public bool allowSlope = false;
    private BoxCollider boxCol;

    private static Dictionary<int, float> gloablShockImmunity = new();
    [Tooltip("충격파 맞은 오브젝트가 다시 반응하기까지 쿨타임")]
    public float immuneTime = 5f;

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
        if(currHold >= requiredHoldtime)
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
        bool hasGroundAtTarget = Physics.Raycast(target + Vector3.up * 0.1f, Vector3.down, 2.5f, groundMask);

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
        //이동 시작 전 내 주변 사방에 있는 타일들에게서 IEdgeColliderHandler 검출하여 캐싱
        List<IEdgeColliderHandler> cache = new();
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
                    cache.Add(targetHandler);
                }
            }
        }

        isMoving = true;
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < moveTime)
        {
            transform.position = Vector3.Lerp(start, target, elapsed / moveTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;

        isMoving = false;

        //중요: 한 프레임만 뒤에 실행시키기.
        yield return null;

        if (gameObject.TryGetComponent<IEdgeColliderHandler>(out var handler))
            //만약 내가 머리 위에 투명벽이 달린 객체라면??
        {
            handler.Inspect();
        }

        //이동이 끝나고 나서 곧바로 내 주변 사방에 있는 타일에서 IEdgeColliderHandler 검출
        for (int i = 0; i < 4; i++)
        {
            Vector3 checkDir = i == 0 ? transform.forward : i == 1 ? -transform.right : i == 2 ? -transform.forward : transform.right;
            Ray ray = new(transform.position - (Vector3.up * .49f), checkDir);
            var result = Physics.RaycastAll(ray, 1.4f, groundMask);
            foreach(var hit in result)
            {
                Debug.Log($"PushableObj: {name} moved. hitted {hit.collider.name}");
                if (hit.collider.TryGetComponent<IEdgeColliderHandler>(out var targetHandler))
                {
                    targetHandler.Inspect();
                }
            }
        }

        //이동이 끝나고 나서 캐싱해놨던 핸들러들도 Inspect(); 호출.
        foreach(var cached in cache)
        {
            cached.Inspect();
        }

        //// 낙하 이벤트 위해 추가
        //if (allowFall)
        //{
        //    yield return StartCoroutine(CheckFall());
        //}
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


    // 지면 없으면 아래로 반복 낙하. Boar에서 참조하므로 public
    public IEnumerator CheckFall()
    {
        isFalling = true;

        bool startedFalling = false; // 낙하 실제 수행?

        Vector3 currPos = transform.position;

        while(!Physics.Raycast(currPos + Vector3.up * 0.1f, Vector3.down, 1.5f, groundMask))
        {
            startedFalling = true;
            Vector3 fallTarget = currPos + Vector3.down * tileSize;

            if(fallTarget.y < -100f) // 무한 추락 방지
            {
                isFalling = false;
                yield break;
            }

            // 한 칸 아래로
            yield return StartCoroutine(MoveTo(fallTarget));
            currPos = transform.position;
        }

        isFalling = false;
        OnLanded();
    }

    // Push 시도 시작(방향 기억, 시간 누적)
    public void StartPushAttempt(Vector2Int dir)
    {
        if(isRiding || isMoving || isFalling) return;
        if(isHoling && dir != holdDir)
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
        isMoving = false;
        isHoling = false;
        currHold = 0f;

    }

    public void OnStopRiding()
    {
        isRiding = false;
        StartCoroutine(CheckFall());
    }

    // ========== 공중 띄우기용 ==========
    // 충격파 맞았을 때 y+1 duration 동안 띄우기
    public void WaveLift(float rise, float hold, float fall)
    {
        int id = GetInstanceID();
        float now = Time.time;
        if(gloablShockImmunity.TryGetValue(id, out var lastTime))
        {
            if(now - lastTime < immuneTime)
            {
                return;
            }
        }
        gloablShockImmunity[id] = now;

        if (isMoving || isFalling || IsImmuneToWaveLift()) return;
        StartCoroutine(WaveLiftCoroutine(rise, hold, fall));
    }

    // 복귀
    IEnumerator WaveLiftCoroutine(float rise, float holdSec, float fall)
    {
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

        if(holdSec > 0f) 
            yield return new WaitForSeconds(holdSec);

        t = 0f;
        while(t < fall)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(target, start, t / fall);
            yield return null;
        }
        transform.position = start;
         
        isMoving = false;
        Debug.Log($"{name} 충격파 영향 받음");
        yield break;
        // NOTE : 혹시나 뭔가 다른 작업을 하다 여기에서 CheckFall()을 할 일이 생긴다면 차라리 다른 스크립트를 작성하는 것을 권장. 원위치 복귀 후 다시 낙하 검사하는 실수 생기면 안 됨.
        // pushables가 충격파 받은 이후로 적층된 물체들이 원위치 후 다시 낙하 검사를 하게 되면 한 번 더 낙하해서 원위치에서 -y로 더 내려가게 됨
    }
}
