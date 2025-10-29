using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 공용 : 코코두기, 깡통, 생명체들
// 데코 : 장식 구조물들 (집 아님)

public class CatBehaviour : MonoBehaviour, IInteractable, IDraggable, ILongPressable
{
    [Header("NavMeshAgent")]
    [SerializeField] float moveSpeed = 3.5f; // 이동 속도
    [SerializeField] float angularSpeed = 120f; // 턴 속도
    [SerializeField] float acceleration = 8f; // 가속도
    [Header("Move")]
    [SerializeField] float moveRadius = 10f; // 랜덤 이동 최대 범위
    [SerializeField] float waitTime = 2f; // 목표 지점 도달 후 대기 시간
    private float timer; //

    [SerializeField] EditController editController; 

    //[SerializeField]private Transform[] waypoints; // 코코두기 전용
    private int currentWaypoinIndex = 0;

    private ObjectAnimationControl oAC; 
    private NavMeshAgentControl oNMAC;
    private Camera mainCam;
    private Vector3 originalPos;

    private bool isDragging = false;

    private int originalLayer;
    private int editableLayer;
    private bool isEditMode;

    private bool yCaptured = false;
    private float yValue;
    private int mainPlaneMask;

    private bool isInteracting = false;
    private bool isReturningToPath = false;


    private void Awake()
    {
        // 공용
        var agent = GetComponent<NavMeshAgent>();
        var anim = GetComponent<Animator>();
        oAC = new ObjectAnimationControl(anim);
        oNMAC = new NavMeshAgentControl(agent, moveSpeed, angularSpeed, acceleration, moveRadius, waitTime, timer, transform);
        mainCam = Camera.main;
        Debug.Log($"Main Cam = {mainCam.name}");

        originalLayer = LayerMask.NameToLayer("InLobbyObject");
        editableLayer = LayerMask.NameToLayer("Editable");
        isEditMode = false;
        //
        // 코코두기
        //if (InLobbyManager.Instance != null) // 코코두기. 깡통은 다른 웨이포인트로 해야하나?
        //{
        //    for (int i = 0; i < InLobbyManager.Instance.waypoints.Length; i++)
        //    {
        //        waypoints[i] = InLobbyManager.Instance.waypoints[i];
        //    }
        //}
        //else if (InLobbyManager.Instance == null) Debug.Log("없어");
        //
        // 깡통

        //

        if (editController == null) editController = FindFirstObjectByType<EditController>();

    }

    private void Update()
    {
        bool current = editController.IsEditMode;
        Debug.Log($"편집모드 상태 : {current}");
        if (current != isEditMode)
        {
            isEditMode = current;
            gameObject.layer = isEditMode ? editableLayer : originalLayer;
        }

        if (isEditMode) 
        {
            // 편집모드일 시 행동 정지, 오브젝트 간이 이동 및 상호작용 정지
            gameObject.layer = LayerMask.NameToLayer("Editable");
            oAC.StopAnim();
            oNMAC.AgentIsStop(true);
            Debug.Log($"{gameObject.name} : 편집모드 상태다");
        }
        else
        {
            oNMAC.LetsGoCoco(ref currentWaypoinIndex, InLobbyManager.Instance.waypoints);
            oAC.MoveAnim(oNMAC.ValueOfMagnitude());
        }

        //oNMAC.MoveValueChanged();
        //oNMAC.WaitAndMove();
    }
    private void LateUpdate()
    {
        if (!yCaptured)
        {
            yValue = transform.position.y;
            yCaptured = true;
            return;
        }
    }
    private void OnEnable()
    {
        gameObject.transform.position = InLobbyManager.Instance.waypoints[0].position;

    }
    private void OnDisable() // 인벤토리나 뭐 씬 넘어가면 처리할 것들
    {
        
    }

    public void OnDragStart(Vector3 position)// IBeginDragHandler 쪽으로 넘어감
    {
        if (editController.IsEditMode) return;

        // 나중에 bool 변수(메인 캐릭터 등등) 리턴.
        originalPos = transform.position;
        isDragging = true;
        oAC.StopAnim();
        oNMAC.AgentIsStop(true);
        oNMAC.EnableAgent(false);
    }

    public void OnDrag(Vector3 position)// IDragHandler, IEndDragHandler 쪽으로 넘어감
    {
        if (!isDragging) return;
        // 나중에 bool 변수(메인 캐릭터 등등) 리턴.
        Ray ray = mainCam.ScreenPointToRay(position);
        mainPlaneMask = 1 << LayerMask.NameToLayer("MainPlaneLayer");

        #region 나중에 삭제할 것
        //RaycastHit[] CanMovePlace = Physics.RaycastAll(ray, 100f, mainPlaneMask);
        //// 나중에 큐브로 만든 로비 바닥에서 첫 번째 판만 탐지하게 방어 추가하기
        //if (CanMovePlace.Length > 0)
        //{
        //    foreach (var hit in CanMovePlace)
        //    {
        //        //if (hit.collider.gameObject.layer == mainPlaneMask) continue;
        //        //if (hit.collider == this.GetComponent<Collider>()) continue;
        //        // if (!hit.collider.gameObject.layer.) continue;
        //        if (!hit.collider.CompareTag("MainPlane")) continue;

        //        Vector3 pos = hit.point;
        //        pos.y = yValue;
        //        transform.position = pos;
        //    }
        //}
        #endregion

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mainPlaneMask))
        {
            //if (!hit.collider.CompareTag("MainPlane")) return;

            Vector3 pos = hit.point;
            pos.y = yValue; // 평면 y 고정
            transform.position = pos;
        }
    }

    public void OnDragEnd(Vector3 position)
    {
        // 나중에 bool 변수(메인 캐릭터 등등) 리턴.

        isDragging = false;
        NavMeshHit navHit;
        bool onNavMesh = NavMesh.SamplePosition(transform.position, out navHit, 0.5f, NavMesh.AllAreas);
        if (!onNavMesh) // 잘못된 위치면 원위치 복귀
        {
            transform.position = originalPos;
            Debug.Log($"{gameObject.name} : NavMesh 밖");
        }
        else
        {
            transform.position = navHit.position;
            Debug.Log($"{gameObject.name} : NavMesh 위에 있음");
        }
        oNMAC.EnableAgent(true);
        oNMAC.NewWrap();
        currentWaypoinIndex = GetClosestWaypointIndex();
        MoveToNextWaypoint();
    }

    public void OnInteract()// IPointerClickHandler으로 넘어감
    {
        if (editController.IsEditMode) return;

        // PointerClick 쪽으로 감
        // 애니메이션 재생, 사운드 각각 한줄 쓱
        Debug.Log($"클릭클릭");
        oAC.PlaySpinAmin();
    }

    public void OnLongPress()
    {
        if (editController.IsEditMode) return;

        Debug.Log($"편집모드 진입");
        oNMAC.AgentIsStop(true);
        oAC.StopAnim();
    }

    private int GetClosestWaypointIndex()
    {
        float minDistance = 1000f;
        int closestIndex = 0;

        for (int i = 0; i < InLobbyManager.Instance.waypoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, InLobbyManager.Instance.waypoints[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    private void MoveToNextWaypoint()
    {
        if (InLobbyManager.Instance.waypoints == null || InLobbyManager.Instance.waypoints.Length == 0) return;
        oNMAC.AgentIsStop(false);
        oNMAC.MoveToPoint(InLobbyManager.Instance.waypoints[currentWaypoinIndex]);

    }

    IEnumerator WaitCoco(float index)
    {
        WaitForSeconds wait = new WaitForSeconds(index);
        yield return wait;
    }
}
