using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// NavMeshAgent 이용 시 고려해야할 것 isStopped, pathPending, hasPath 

[RequireComponent(typeof(GameObjectData), typeof(UserInteractionHandler), typeof(Draggable))]
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public abstract class aaTestaa : MonoBehaviour, ILobbyInteractable, ILobbyDraggable, ILobbyPressable, ILobbyState
{
    [Header("NavMeshAgent")]
    [SerializeField] protected float moveSpeed = 3.5f;
    [SerializeField] protected float angularSpeed = 120f;
    [SerializeField] protected float acceleration = 8f;
    [Header("Move")]
    [SerializeField] protected float moveRadius = 8f; // 웨이포인트에서 범위
    [SerializeField] protected float waitTime = 2f; // 대기 시간


    protected NavMeshAgent agent;
    protected Animator anim;
    protected LobbyCharacterFSM fsm;
    protected NavMeshAgentControl charAgent; // Agent 파츠
    protected LobbyCharacterAnim charAnim; // 애니 파츠
    protected Camera mainCam;
    protected Vector3 originalPos; // 드래그 시 시작 포지션 저장
    protected bool isDragging = false;
    protected float yValue; // 생성 시 y축 얻고 드래그 시 해당 값 고정
    //protected bool yCaptured = false; // yValue가 값을 얻었는지 판단
    protected int mainPlaneMask;

    protected bool isMoving;
    protected WaitUntil waitU;
    protected WaitForSeconds waitFS;
    protected LobbyCharacterState currentState;
    protected int interactionState;

    protected Transform[] waypoints;

    // Stuck
    protected float stuckTimeA;
    protected float stuckTimeB = 2;

    public bool IsCMRoutineComplete { get; protected set; }
    public bool IsCARoutineComplete { get; private set; }

    /// <summary>
    /// FSM 초기 상태를 각 자식놈들이 정의
    /// </summary>
    /// <returns></returns>
    public abstract LobbyCharacterBaseState CreateInitialState();
    public abstract LobbyCharacterBaseState CreateMoveState();
    public abstract LobbyCharacterBaseState CreateInteractState();
    public abstract LobbyCharacterBaseState CreateDraggableState(Vector3 pos);
    public abstract LobbyCharacterBaseState CreatePressableState();

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        agent.height = 1f;
        // agent
        charAgent = new NavMeshAgentControl(agent, moveSpeed, angularSpeed, acceleration, moveRadius);
        // charAnim
        charAnim = new LobbyCharacterAnim(this, anim);
        mainCam = Camera.main;
        mainPlaneMask = LayerMask.NameToLayer("MainPlaneLayer");
        waitU = new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= 0.5f);
        waitFS = new WaitForSeconds(waitTime);
    }

    protected virtual void OnEnable()
    {
        Register();
        // if (!yCaptured)
        // {
        //     yValue = transform.position.y;
        //     Debug.Log($"yValue : {yValue}");
        //     yCaptured = true;
        //     return;
        // }
    }

    protected virtual void Start()
    {
        currentState = LobbyCharacterState.Idle;
        if (gameObject.CompareTag("Animal"))
        {
            ChangeState(LobbyCharacterState.Move);
        }
        else
        {
            ChangeState(currentState);
        }
        // 뉴 fsm
        fsm = new LobbyCharacterFSM(CreateInitialState());
    }

    protected void Update()
    {
        charAnim.MoveAnim(charAgent.ValueOfMagnitude());
        if (!agent.hasPath) Debug.Log($"{gameObject.name} No path");
        else if (agent.pathStatus == NavMeshPathStatus.PathInvalid) Debug.Log($"{gameObject.name} Invalid path");
        else if (agent.isStopped) Debug.Log($"{gameObject.name} Agent is stopped");
        else if (agent.enabled == false) Debug.Log($"{gameObject.name} Agent doesn't enable");
        // 구버전
        switch (currentState)
        {
            case LobbyCharacterState.Idle:
                HandleIdle();
                break;
            case LobbyCharacterState.Move:
                HandleMove();
                break;
            case LobbyCharacterState.Stuck:
                HandleStuck();
                break;
            case LobbyCharacterState.Recovering:
                break;
            case LobbyCharacterState.Dragging:
                HandleInteraction();
                break;
            case LobbyCharacterState.Animation:
                HandleAnimation();
                break;
            case LobbyCharacterState.CocoOther:
                HandleCocoOther();
                break;
        }
        // 뉴 fsm
        fsm.UpdateState();
        //charAgent.MoveValueChanged();
    }

    protected void OnDisable() // 코코두기와 마스터는 리스트에서 지우면 안됩니다. 이 부분은 로비 틀이 제대로 만들어 지면 해결 예정
    {
        // 씬전환 시에는 return 시키고 그것이 아니라면 초기화 해야함 // 구버전
        switch (gameObject.tag)
        {
            case "CocoDoogy":
                StopMoving();
                ChangeState(LobbyCharacterState.Idle);
                break;
            case "Master":
                StopMoving();
                ChangeState(LobbyCharacterState.Idle);
                break;
            case "Animal":
                Unregister();
                StopMoving();
                ChangeState(LobbyCharacterState.Idle);
                break;
            default: throw new Exception("누구세요?");
        }
    }
    protected void StopMoving()
    {
        if (isMoving == true)
        {
            isMoving = false;
            StopAllCoroutines();
            if (agent != null && agent.isActiveAndEnabled) agent.ResetPath();
        }
    }

    protected abstract void HandleIdle();

    protected abstract void HandleMove();

    protected abstract void HandleStuck();

    protected abstract void HandleInteraction();

    protected abstract void HandleAnimation();

    protected abstract void HandleCocoOther();

    // 구버전
    protected abstract void ChangeState(LobbyCharacterState newState);

    // 뉴 fsm
    /// <summary>
    /// 코코두기 상호작용 루틴 끝
    /// </summary>
    public void EndRoutine(int i)
    {
        if (i == 0)
        {
            IsCMRoutineComplete = true;
        }
        else if (i == 1)
        {
            IsCARoutineComplete = true;   
        }
    }
    /// <summary>
    /// 코코두기 상호작용 루틴 리셋
    /// </summary>
    public void ResetRoutine(int i)
    {
        if (i == 0)
        {
            IsCMRoutineComplete = false;
        }
        else if (i == 1)
        {
            IsCARoutineComplete = false;   
        }
    }

    // 애니메이션 시 에이전트 제어 근데 이곳에 쓰는게 맞나.
    public void FromAToB()
    {
        // 구버전
        ChangeState(LobbyCharacterState.Move);
        // 신버전
        StartCoroutine(EndAnim());
    }
    protected IEnumerator EndAnim()
    {
        yield return waitFS;
        // 상태 전환
        if (gameObject.GetComponent<CocoDoogyBehaviour>()) fsm.ChangeState(CreateMoveState());
        else if (gameObject.GetComponent<MasterBehaviour>()) fsm.ChangeState(CreateMoveState());
        else if (gameObject.GetComponent<AnimalBehaviour>()) fsm.ChangeState(CreateMoveState());
        yield break;
    }


    // 인터페이스 영역
    /// <summary>
    /// 코코두기와 동물들 상호작용
    /// </summary>
    public virtual void OnCocoAnimalEmotion() { }

    /// <summary>
    /// 코코두기와 마스터 상호작용
    /// </summary>
    public virtual void OnCocoMasterEmotion() { }

    /// <summary>
    /// ILobbyDraggable, 드래그 시작
    /// </summary>
    /// <param name="position"></param>
    public void OnLobbyBeginDrag(Vector3 position)
    {
        ChangeState(LobbyCharacterState.Dragging);

        originalPos = transform.position;
        //StopMoving();
        isDragging = true;
        charAnim.StopAnim();
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        agent.enabled = false;
        fsm.ChangeState(CreateDraggableState(position));
    }

    /// <summary>
    /// ILobbyDraggable, 드래그 중
    /// </summary>
    /// <param name="position"></param>
    public void OnLobbyDrag(Vector3 position)
    {
        interactionState = 1;
        if (!isDragging) return;
        Ray ray = mainCam.ScreenPointToRay(position);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mainPlaneMask))
        {
            //if (!hit.collider.CompareTag("MainPlane")) return;

            Vector3 pos = hit.point;
            pos.y = yValue;
            transform.position = pos;
        }
        fsm.ChangeState(CreateDraggableState(position));
    }

    /// <summary>
    /// ILobbyDraggable, 드래그 끝
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
        agent.enabled = true;
        agent.Warp(transform.position);
        interactionState = 0;
        Debug.Log($"iState : {interactionState}");
        ChangeState(LobbyCharacterState.Move);
        fsm.ChangeState(CreateInitialState());
    }

    /// <summary>
    /// ILobbyInteractable, 터치 시 상호작용
    /// </summary>
    public virtual void OnLobbyClick()
    {
        ChangeState(LobbyCharacterState.Animation);
        Debug.Log($"Click");
    }

    /// <summary>
    /// ILobbyPressable, 누를 시 상호작용
    /// </summary>
    public void OnLobbyPress()
    {
        ChangeState(LobbyCharacterState.Dragging);

        Debug.Log($"Press");
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        charAnim.StopAnim();
    }

    /// <summary>
    /// ILobbyState, 일반모드 진입 시
    /// </summary>
    public virtual void InNormal()
    {
        if (!agent.enabled) agent.enabled = true;
        if (agent.enabled && agent.isStopped) agent.isStopped = false;
        agent.Warp(transform.position);
    }

    /// <summary>
    /// ILobbyState, 편집모드 진입 시
    /// </summary>
    public void InEdit()
    {
        charAnim.StopAnim();
        //StopMoving();
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        if (!agent.enabled) agent.enabled = false;
    }

    /// <summary>
    /// ILobbyState, 로비 씬 스타트 시
    /// </summary>
    public abstract void StartScene();

    /// <summary>
    /// ILobbyState, 로비 씬 나갈 시
    /// </summary>
    //public abstract void ExitScene();

    /// <summary>
    /// ILobbyState, 오브젝트 생성 시 등록
    /// </summary>
    public void Register()
    {
        if (LobbyCharacterManager.Instance == null)
        {
            Debug.LogWarning("로비인터페이스 못 찾음");
            return;
        }
        LobbyCharacterManager.Instance.RegisterLobbyChar(this);
        Debug.Log($"{this} 등록");
    }
    
    /// <summary>
    /// ILobbyState, 오브젝트 삭제 시 취소
    /// </summary>
    public void Unregister()
    {
        if (LobbyCharacterManager.Instance == null)
        {
            Debug.LogWarning("로비인터페이스 못 찾음");
            return;
        }
        if (gameObject.CompareTag("CocoDoogy") || gameObject.CompareTag("Master")) return;
        LobbyCharacterManager.Instance.UnregisterLobbyChar(this);
        Debug.Log($"{this} 삭제");
    }

    public void Init()
    {
        throw new NotImplementedException();
    }

    public void PostInit()
    {
        throw new NotImplementedException();
    }

    public void LoadInit()
    {
        throw new NotImplementedException();
    }

    public void FinalInit()
    {
        throw new NotImplementedException();
    }

    public void InitWaypoint()
    {
        throw new NotImplementedException();
    }
}