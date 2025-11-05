using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 11/4
// TODO : 버튼이 눌려서 거북이가 이동을 시작하면 플레이어의 움직임을 정지 시켜야 함. 그 외에는 자유롭게 넘나들 수 있도록 플레이어 이동 로직 막으면 안 됨.

[RequireComponent(typeof(Rigidbody))]
public class Turtle : MonoBehaviour, IDashDirection, IPlayerFinder
{
    [Header("Movement Settgins")]
    public float tileSize = 1f;
    [Tooltip("이동 속도 (tile/s)")]
    public float moveSpeed = 5f;
    [Tooltip("거북이 충돌 레이어")]
    public LayerMask blockLayer;
    [Tooltip("터틀 위에 올라탈 수 있는 오브젝트의 레이어 이름")]
    public LayerMask ridableLayer;

    [Header("Player Detection")]
    public float detectRadius = 3f; // 플레이어 감지 범위
    public LayerMask playerLayer; // 플레이어 레이어 마스크
    private Transform playerTrans; // 감지된 플레이어의 Transform

    private bool isMoving = false;
    private Vector3 targetPos;
    private Rigidbody rb; // 물리 처리용 (물살 영향 방지)

    [Header("Popup Buttons")]
    [SerializeField] private GameObject btnGroup;
    [SerializeField] private Button up;
    [SerializeField] private Button down;
    [SerializeField] private Button left;
    [SerializeField] private Button right;
    [SerializeField] Image typeIcon;

    public bool CanInteract => !isMoving; // 이동 중이 아닐 때만 상호작용 가능

    Transform IPlayerFinder.Player { get => playerTrans; set => playerTrans = value; }

    void Awake()
    {
        up.onClick.AddListener(() => { GetDirection(new Vector2Int(0, 1)); btnGroup.SetActive(false); });
        down.onClick.AddListener(() => { GetDirection(new Vector2Int(0, -1)); btnGroup.SetActive(false); });
        left.onClick.AddListener(() => { GetDirection(new Vector2Int(-1, 0)); btnGroup.SetActive(false); });
        right.onClick.AddListener(() => { GetDirection(new Vector2Int(1, 0)); btnGroup.SetActive(false); });

        btnGroup.SetActive(false);

        // 플레이어 transform 찾기
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerTrans = playerGO.transform;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    void Update()
    {
        if (!isMoving)
        {
            DetectPlayer();
        }
    }

    void DetectPlayer()
    {
        if (!playerTrans || !btnGroup) return;

        float dist = Vector3.Distance(transform.position + Vector3.up * 0.5f, playerTrans.position);
        bool inRange = dist <= detectRadius;

        if (btnGroup.activeSelf != inRange)
        {
            btnGroup.SetActive(inRange);
            if (inRange)
            {
                // NOTE : 추후 각도 변경 가능성 있음.
                btnGroup.transform.rotation = Quaternion.Euler(90f, 0, 0);
            }
        }
    }

    public void GetDirection(Vector2Int dir)
    {
        if (isMoving) return;

        isMoving = true;
        btnGroup.SetActive(false);

        Vector3 moveDir = new Vector3(dir.x, 0, dir.y);

        // 빙판 로직 적용 : 멈출 때까지 방향으로 이동할 목표 위치 계산
        targetPos = CalculateSlideTarget(moveDir);

        // 이동 시작 시, 위에 얹힌 모든 오브젝트도 같이 이동 시작
        StartCoroutine(MoveSlideCoroutine(moveDir, transform.position, targetPos));
    }

    // 빙판 로직 계산
    private Vector3 CalculateSlideTarget(Vector3 dir)
    {
        Vector3 currTile = transform.position;
        Vector3 nextTile = currTile + dir * tileSize;

        // 충돌 체크용 박스 크기
        Vector3 boxHalfExt = Vector3.one * (tileSize * 0.45f) + Vector3.up * 0.25f;

        // Block Layer를 만날 때까지 계속 이동
        while (true)
        {
            Collider[] hits = Physics.OverlapBox(
                nextTile,
                boxHalfExt,
                Quaternion.identity,
                blockLayer,
                QueryTriggerInteraction.Ignore
            );

            // 충돌 발생하면 break
            if (hits.Length > 0)
            {
#if UNITY_EDITOR
                foreach (var hit in hits)
                {
                    string layerName = LayerMask.LayerToName(hit.gameObject.layer);
                    Debug.Log($"[Turtle] 충돌 객체: {hit.name}, Layer: {layerName} ({hit.gameObject.layer}), Tag: {hit.tag}");
                }
#endif
                break;
            }
            // 이동 조건이 충족되었으므로 다음 타일로 이동
            currTile = nextTile;
            nextTile += dir * tileSize;
        }
        return currTile; // 블록 타일 바로 앞 타일 위치 반환
    }

    // 탑승 물체 이동
    // HACK : 이 부분은 움직일 수 있는 물체가 Turtle에 안 막히게 하면 될 것 같기도 한데 어떤 부분이 효율적일지 고민해봐야 함. 둘 다 처리 하는 게 안전한 방법일 수도.
    IEnumerator MoveSlideCoroutine(Vector3 dir, Vector3 startPos, Vector3 endPos)
    {
        float dist = Vector3.Distance(startPos, endPos);
        if (dist < tileSize * 0.5f)
        {
            isMoving = false;
            yield break;
        }

        float duration = dist / moveSpeed;
        float elapsed = 0f;

        Vector3 offset = endPos - startPos;

        // 탑승 가능한 오브젝트 감지
        Vector3 overlapOrigin = transform.position + Vector3.up * (tileSize / 2f);
        Vector3 halfExtents = new Vector3(tileSize * 0.45f, tileSize * 0.75f, tileSize * 0.45f);

        Collider[] ridables = Physics.OverlapBox(
            overlapOrigin,
            halfExtents,
            Quaternion.identity,
            ridableLayer,
            QueryTriggerInteraction.Ignore
        );

        List<Transform> ridableTrans = new List<Transform>();
        List<Vector3> ridableTargetPos = new List<Vector3>();
        List<Rigidbody> ridableRbs = new List<Rigidbody>();
        List<PlayerMovement> playerMovement = new List<PlayerMovement>();
        List<PushableObjects> pushableObjs = new List<PushableObjects>(); // NOTE : Box만 탑승 가능하다면 PushableBox로 바꾸면 될 듯. 이하 모든 PushableObjects 동일.


        foreach (var col in ridables)
        {
            Debug.Log($"[Trutle] {col.name} 탑승 감지됨.");

            if (col.transform == transform) continue;

            Rigidbody riderRb = col.attachedRigidbody;
            PlayerMovement pm = col.GetComponent<PlayerMovement>();
            PushableObjects po = col.GetComponent<PushableObjects>();

            // 탑승 가능한 객체인지 확인
            if (pm == null && po == null) continue;

            // 공통 데이터 추가
            ridableTargetPos.Add(col.transform.position + offset);
            ridableTrans.Add(col.transform);

            // Rigidbody 쓰는 객체 처리
            if (riderRb != null)
            {
                riderRb.isKinematic = true;
                ridableRbs.Add(riderRb); // Rigidbody가 있는 객체만 리스트에 추가
            }

            // PlayerMovement 처리
            if (pm != null)
            {
                pm.enabled = false;
                playerMovement.Add(pm);
            }

            // PushableObjects 처리
            if (po != null)
            {
                po.enabled = false;
                pushableObjs.Add(po);
            }
            // 거북이를 부모로 설정
            col.transform.SetParent(transform);
        }

        // 터틀과 탑승 물체 동시 이동
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 터틀 이동
            transform.position = Vector3.Lerp(startPos, endPos, t);
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
            yield return null;
        }

        // 이동 완료 및 상태 정리
        transform.position = endPos;

        for (int i = 0; i < ridableTrans.Count; i++)
        {
            if (ridableTrans[i] == null) continue;

            Vector3 finalPos = ridableTargetPos[i];

            // 부모가 아직 이 거북이(this.transform)인지 확인 후 해제
            if (ridableTrans[i].parent == transform)
            {
                // 부모 해제
                ridableTrans[i].SetParent(null);

                // 하차 후 타깃 위치 설정
                ridableTrans[i].position = finalPos;
            }
            else { }
        }
        yield return null;

        // Rigidbody 가진 객체만 복원
        foreach (var rb in ridableRbs)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }

        // PlayerMovement 재활성화
        foreach (var pm in playerMovement)
        {
            if (pm != null)
            {
                pm.enabled = true;
            }
        }

        // PushableObjects 재활성화 
        foreach (var po in pushableObjs)
        {
            if (po != null)
            {
                po.enabled = true;
            }
        }
        isMoving = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 플레이어 감지 범위
        Gizmos.color = Color.yellow * 0.5f;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, detectRadius);

        // Ridable 오브젝트 감지 범위
        Gizmos.color = Color.cyan * 0.5f;
        Vector3 overlapOrigin = transform.position + Vector3.up * (tileSize / 2f); // Centre
        Vector3 halfExtents = new Vector3(tileSize * 0.45f, tileSize * 0.75f, tileSize * 0.45f);
        Vector3 size = halfExtents * 2f;
        Gizmos.DrawWireCube(overlapOrigin, size);

        // 최종 목표 위치
        if (Application.isPlaying && isMoving)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(targetPos, 0.2f);
        }
    }
#endif
}
