using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ObjectScript : MonoBehaviour, IInteractable, IDraggable, ILongPressable
{
    [Header("NavMeshAgent")]
    [SerializeField] float moveSpeed = 3.5f; // 이동 속도
    [SerializeField] float angularSpeed = 120f; // 턴 속도
    [SerializeField] float acceleration = 8f; // 가속도
    [Header("Move")]
    [SerializeField] float moveRadius = 10f; // 랜덤 이동 최대 범위
    [SerializeField] float waitTime = 2f; // 목표 지점 도달 후 대기 시간
    private float timer; //

    [SerializeField] Transform[] Waypoints { get; set; } // 웨이 포인트
    private int currentwaypoinIndex = 0;

    private ObjectAnimationController oAC;
    private ObjectNavMeshAgentController oNMAC;
    private Camera mainCam;
    private Vector3 originalPos;
    private bool isDragging = false;

    private bool isInteracting = false;
    private bool isReturningToPath = false;


    private void Awake()
    {
        var agent = GetComponent<NavMeshAgent>();
        var anim = GetComponent<Animator>();
        oAC = new ObjectAnimationController(anim);
        oNMAC = new ObjectNavMeshAgentController(agent, moveSpeed, angularSpeed, acceleration, moveRadius, waitTime, timer, transform);
        mainCam = Camera.main;
        Debug.Log($"Main Cam = {mainCam.name}");
    }

    private void OnEnable()
    {
        if (Waypoints.Length > 0)
        {
        }
        oNMAC.MoveRandomPosition();
        
    }

    private void Start()
    {
    }

    // 아마 로비에 생성되는 순간에는 Enable이 맞을지도?
    //private void OnEnable()
    //{
    //    oNMAC.MoveToRandomPosition();
    //}

    private void Update()
    {
        oNMAC.MoveValueChanged();
        oAC.MoveAnim(oNMAC.ValueOfMagnitude());
        oNMAC.WaitAndMove();
    }

    public void OnDragStart(Vector3 position)
    {
        // 나중에 bool 변수(메인 캐릭터 등등) 리턴.
        originalPos = transform.position;
        isDragging = true;
    }

    public void OnDrag(Vector3 position)
    {
        if (!isDragging) return;
        // 나중에 bool 변수(메인 캐릭터 등등) 리턴.
        Ray ray = mainCam.ScreenPointToRay(position);
        RaycastHit[] CanMovePlace = Physics.RaycastAll(ray);
        // 나중에 큐브로 만든 로비 바닥에서 첫 번째 판만 탐지하게 방어 추가하기
        if (CanMovePlace.Length > 0)
        {
            foreach (var hit in CanMovePlace)
            {
                if (hit.collider == this.GetComponent<Collider>()) continue;
                Vector3 pos = hit.point;
                pos.y = 0f;
                transform.position = pos;
            }
        }
    }

    public void OnDragEnd(Vector3 position)
    {
        // 나중에 bool 변수(메인 캐릭터 등등) 리턴.
        //var endPos = transform.position;
        //transform.position = position;

        // 잘못된 위치면 원위치 복귀
        
        isDragging = false;
    }

    public void OnInteract()
    {
        // PointerClick 쪽으로 감
        // 애니메이션 재생, 사운드 각각 한줄 쓱
        Debug.Log($"클릭클릭");
        oAC.PlaySpinAmin();
    }

    public void OnLongPress()
    {
        Debug.Log($"편집모드 진입");
        oNMAC.AgentStop();
        oAC.StopAnim();
    }

    private void ConfirmPlacement()
    {
        Debug.Log($"{gameObject.name} 위치 성공");
    }

    
}
