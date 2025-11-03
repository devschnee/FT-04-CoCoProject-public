using System;
using UnityEngine;
using UnityEngine.AI;

// 뭘 빼고 뭘 넣어야 이쁠까 
[RequireComponent(typeof(GameObjectData), typeof(UserInteractionHandler), typeof(Draggable))]
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public abstract class BaseLobbyCharacterBehaviour : MonoBehaviour, ILobbyInteractable, ILobbyDraggable, ILobbyPressable, ILobbyCharactersEmotion, ILobbyState
{
    [Header("NavMeshAgent")]
    [SerializeField] protected float moveSpeed = 3.5f;
    [SerializeField] protected float angularSpeed = 120f;
    [SerializeField] protected float acceleration = 8f;
    [Header("Move")]
    [SerializeField] protected float moveRadius = 10f; // 웨이포인트에서 범위
    [SerializeField] protected float waitTime = 2f; // 대기 시간
    //[SerializeField] protected EditController editController; // 편집모드, 로비매니저로


    protected NavMeshAgent agent;
    protected Animator anim;
    protected NavMeshAgentControl charAgent; // Agent 파츠
    protected LobbyCharacterAnim charAnim; // 애니 파츠
    protected Transform trans;
    protected Camera mainCam;
    protected Vector3 originalPos; // 드래그 시 시작 포지션 저장
    protected bool isDragging = false;

    //protected int originalLayer; // 평상 시 레이어, 로비매니저로
    //protected int editableLayer; // 편집모드 시 레이어, 로비매니저로
    //protected bool isEditMode; // 상태전환을 이걸로 퉁치나?, 로비매니저로
    protected float yValue; // 생성 시 y축 얻고 드래그 시 해당 값 고정
    protected bool yCaptured = false; // yValue가 값을 얻었는지 판단
    protected int mainPlaneMask;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        // agent
        charAgent = new NavMeshAgentControl(agent, moveSpeed, angularSpeed, acceleration, moveRadius, waitTime, trans);
        // charAnim
        charAnim = new LobbyCharacterAnim(anim);
        mainCam = Camera.main;
        //if (editController == null) editController = FindFirstObjectByType<EditController>(); // 로비매니저로

        //originalLayer = LayerMask.NameToLayer("InLobbyObject"); // 로비매니저로
        //editableLayer = LayerMask.NameToLayer("Editable"); // 로비매니저로
        mainPlaneMask = LayerMask.NameToLayer("MainPlaneLayer");
        //isEditMode = false; // 상태패턴 전환 시 수정, 로비매니저로

    }

    protected virtual void OnEnable()
    {
        Register();
        if (!yCaptured)
        {
            yValue = transform.position.y;
            Debug.Log($"yValue : {yValue}");
            yCaptured = true;
            return;
        }
    }

    protected void OnDisable() // 코코두기와 마스터는 리스트에서 지우면 안됩니다.
    {
        // 씬전환 시에는 return 시키고 그것이 아니라면 초기화 해야함
        switch (gameObject.tag)
        {
            case "CocoDoogy":
                break;
            case "Master":
                break;
            case "Animal":
                Unregister();
                break;
            default: throw new Exception("누구세요?");
        }
    }

    // 애니메이션 시 에이전트 제어 근데 이곳에 쓰는게 맞나.
    public void StopAgentBeginAnim()
    {
        agent.isStopped = true;

    }
    
    public void StartAgentEndAnim()
    {
        agent.isStopped = false;
    }


    // 인터페이스 영역
    /// <summary>
    /// 코코두기와 동물들 상호작용
    /// </summary>
    public abstract void OnCocoAnimalEmotion();
    /// <summary>
    /// 코코두기와 마스터 상호작용
    /// </summary>
    public abstract void OnCocoMasterEmotion();
    /// <summary>
    /// 드래그 시작
    /// </summary>
    /// <param name="position"></param>
    public void OnLobbyBeginDrag(Vector3 position)
    {
        if (InLobbyManager.Instance == null) return;
        if (InLobbyManager.Instance.isEditMode) return;

        originalPos = transform.position;
        isDragging = true;
        charAnim.StopAnim();
        charAgent.AgentIsStop(true);
        charAgent.EnableAgent(false);
    }
    /// <summary>
    /// 드래그 중
    /// </summary>
    /// <param name="position"></param>
    public void OnLobbyDrag(Vector3 position)
    {
        if (!isDragging) return;
        Ray ray = mainCam.ScreenPointToRay(position);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mainPlaneMask))
        {
            //if (!hit.collider.CompareTag("MainPlane")) return;

            Vector3 pos = hit.point;
            pos.y = yValue;
            transform.position = pos;
        }
    }
    /// <summary>
    /// 드래그 끝
    /// </summary>
    /// <param name="position"></param>
    public virtual void OnLobbyEndDrag(Vector3 position)
    {
        isDragging = false;
        NavMeshHit navHit;
        bool onNavMesh = NavMesh.SamplePosition(transform.position, out navHit, 0.5f, NavMesh.AllAreas);
        if (!onNavMesh)
        {
            transform.position = originalPos;
            Debug.Log($"{gameObject.name} : NavMesh 없음 기존 포지션으로");
        }
        else
        {
            transform.position = navHit.position;
            Debug.Log($"{gameObject.name} : NavMesh 있음 해당 포지션으로");
        }
        charAgent.EnableAgent(true);
        agent.Warp(transform.position);
        
    }
    /// <summary>
    /// 터치 시 상호작용
    /// </summary>
    public virtual void OnLobbyInteract()
    {
        if (InLobbyManager.Instance == null) return;
        if (InLobbyManager.Instance.isEditMode) return;

        Debug.Log($"Click");
        charAnim.InteractionAnim();
    }
    /// <summary>
    /// 누를 시 상호작용
    /// </summary>
    public void OnLobbyPress()
    {
        if (InLobbyManager.Instance == null) return;
        if (InLobbyManager.Instance.isEditMode) return;

        Debug.Log($"Press");
        charAgent.AgentIsStop(true);
        charAnim.StopAnim();
    }

    /// <summary>
    /// 오브젝트 생성 시 등록
    /// </summary>
    public void Register()
    {
        if (InLobbyManager.Instance == null)
        {
            Debug.LogWarning("로비인터페이스 못 찾음");
            return;
        }
        InLobbyManager.Instance.RegisterLobbyChar(this);
    }
    /// <summary>
    /// 오브젝트 삭제 시 취소
    /// </summary>
    public void Unregister()
    {
        if (InLobbyManager.Instance == null)
        {
            Debug.LogWarning("로비인터페이스 못 찾음");
            return;
        }
        InLobbyManager.Instance.UnregisterLobbyChar(this);
    }
    /// <summary>
    /// 일반모드 진입 시
    /// </summary>
    public virtual void InNormal()
    {
        if (!agent.enabled) charAgent.EnableAgent(true);
        if (agent.isStopped) charAgent.AgentIsStop(false);
        agent.Warp(transform.position);
    }
    /// <summary>
    /// 편집모드 진입 시
    /// </summary>
    public void InEdit()
    {
        charAnim.StopAnim();
        if (!agent.isStopped) charAgent.AgentIsStop(true);
        charAgent.EnableAgent(false);
    }

    public abstract void InUpdate();

    public abstract void StartScene();

    public abstract void ExitScene();
}
