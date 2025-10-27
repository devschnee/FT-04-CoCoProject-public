using System.Collections;
using UnityEngine;

public abstract class PushableObjects : MonoBehaviour, IPushHandler
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

    protected Rigidbody rb;
    protected bool isMoving = false;
    protected bool isHoling = false;
    protected bool isFalling = false;
    public float requiredHoldtime = 0.9f;
    protected float currHold = 0f;
    protected Vector2Int holdDir;

    public bool allowFall = true;
    public bool allowSlope = false;
    #endregion

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        if (!isHoling || isMoving) return;
        
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

        // 경사로가 있다면 위로 1칸 추가 이동
        if (allowSlope && Physics.Raycast(target + Vector3.up * 0.5f, Vector3.down, 1f, slopeMask))
        {
            Vector3 up = target + Vector3.up * tileSize;
            // 위 칸이 비어있고, 그 칸이 바닥이면 경사로 위로 이동
            if (!Physics.CheckBox(up + Vector3.up * 0.5f, Vector3.one * 0.4f, Quaternion.identity, blockingMask) &&
                Physics.Raycast(up + Vector3.up * 0.1f, Vector3.down, 1.5f, groundMask))
            {
                StartCoroutine(MoveTo(up));
                return true;
            }
        }

        // 목적지에 뭔가 있으면 못 감
        if (Physics.CheckBox(target + Vector3.up * 0.5f, Vector3.one * 0.4f, Quaternion.identity, blockingMask))
            return false;

        // 이동 후 낙하여부까지 처리
        StartCoroutine(MoveAndFall(target));
        return true;
    }

    // 모양에 맞는 충돌 검사 구현하도록
    protected abstract bool CheckBlocking(Vector3 target);

    // 단순 이동(1칸 Lerp 이동)
    protected IEnumerator MoveTo(Vector3 target)
    {
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

    // 지면 없으면 아래로 반복 낙하
    protected IEnumerator CheckFall()
    {
        isFalling = true;

        Vector3 currPos = transform.position;

        while(!Physics.Raycast(currPos + Vector3.up * 0.1f, Vector3.down, 1.5f, groundMask))
        {
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
    }

    // Push 시도 시작(방향 기억, 시간 누적)
    public void StartPushAttempt(Vector2Int dir)
    {
        if(isMoving || isFalling) return;
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
}
