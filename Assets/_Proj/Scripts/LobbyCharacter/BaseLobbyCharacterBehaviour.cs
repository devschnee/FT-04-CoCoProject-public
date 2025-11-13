using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// NavMeshAgent 이용 시 고려해야할 것 isStopped, pathPending, hasPath 

[RequireComponent(typeof(UserInteractionHandler), typeof(Draggable), typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public abstract class BaseLobbyCharacterBehaviour : MonoBehaviour, ILobbyInteractable, ILobbyDraggable, ILobbyPressable, ILobbyCharactersEmotion, ILobbyState
{
    [Header("NavMeshAgent")]
    [SerializeField] protected float moveSpeed = 3.5f;
    [SerializeField] protected float angularSpeed = 120f;
    [SerializeField] protected float acceleration = 8f;
    [Header("Move")]
    [SerializeField] protected float moveRadius = 8.5f; // Random.insideUnitSphere 범위

    protected NavMeshAgent agent;
    protected Animator anim;
    protected LobbyCharacterFSM fsm;
    protected NavMeshAgentControl charAgent; // Agent 파츠
    protected LobbyCharacterAnim charAnim; // 애니 파츠
    public Camera MainCam { get; protected set; }
    public float YValue { get; protected set; } // 생성 시 y축 얻고 드래그 시 해당 값 고정
    public bool isInitComplete = false;


    public List<LobbyWaypoint> Waypoints { get; protected set; }

    // Stuck
    public float StuckTime { get; protected set; }

    public bool IsLobbyEditMode { get; set; }
    public bool IsCMRoutineComplete { get; set; }
    public bool IsCARoutineComplete { get; set; }
    public bool IsCMInteractComplete { get; set; }
    public bool IsCAInteractComplete { get; set; }

    /// <summary>
    /// FSM 초기 상태를 각 자식들이 정의
    /// </summary>
    /// <returns></returns>
    protected abstract void InitStates();
    public LobbyCharacterBaseState IdleState { get; protected set; }
    public LobbyCharacterBaseState MoveState { get; protected set; }
    public LobbyCharacterBaseState InteractState { get; protected set; }
    public LobbyCharacterBaseState ClickSate { get; protected set; }
    public LobbyCharacterBaseState DragState { get; protected set; }
    public LobbyCharacterBaseState EditState { get; protected set; }
    public LobbyCharacterBaseState StuckState { get; protected set; }

    protected virtual void Awake()
    {
        if (LobbyCharacterManager.Instance.IsEditMode)
        {
            gameObject.layer = LayerMask.NameToLayer("Editable");
        }
        else
        {
            gameObject.layer = LayerMask.NameToLayer("InLobbyObject");
        }
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        Waypoints = LobbyCharacterManager.Instance.Waypoints;
        charAgent = new NavMeshAgentControl(agent, moveSpeed, angularSpeed, acceleration, moveRadius);
        charAnim = new LobbyCharacterAnim(anim);
        MainCam = Camera.main;
        fsm = new LobbyCharacterFSM(null);
        Register();
        InitStates();
    }

    protected virtual void OnEnable()
    {
        if (fsm != null && fsm.CurrentState == EditState) fsm.ChangeState(IdleState);
    }

    protected virtual void Start()
    {
        
    }

    protected virtual void Update()
    {
        if (charAnim != null) charAnim.MoveAnim(charAgent.ValueOfMagnitude());
        if (!agent.hasPath) Debug.Log($"{gameObject.name} No path, State : {fsm.CurrentState}");
        else if (agent.pathStatus == NavMeshPathStatus.PathInvalid) Debug.Log($"{gameObject.name} Invalid path");
        else if (agent.isStopped) Debug.Log($"{gameObject.name} Agent is stopped");
        else if (agent.enabled == false) Debug.Log($"{gameObject.name} Agent doesn't enable");
        
        if (fsm != null) fsm.UpdateState();
        //charAgent.MoveValueChanged();
    }

    protected void OnDisable()
    {
        switch (gameObject.tag)
        {
            case "CocoDoogy":
                StopMoving();
                break;
            case "Master":
                StopMoving();
                break;
            case "Animal":
                //Unregister();
                StopMoving();
                break;
            default: throw new Exception("누구세요?");
        }
    }

    protected void OnDestroy()
    {
        if (gameObject.CompareTag("Animal"))
        {
            Unregister();
        }
    }
    
    protected void StopMoving()
    {
        //fsm.ChangeState(IdleState);
        StopAllCoroutines();
        if (agent != null && agent.isActiveAndEnabled) agent.ResetPath();
    }

    /// <summary>
    /// 코코두기, 마스터 루틴 끝
    /// </summary>
    public void EndRoutine()
    {
        IsCMRoutineComplete = true;
    }
    /// <summary>
    /// 코코두기, 마스터 루틴 리셋
    /// </summary>
    public void ResetRoutine()
    {
        IsCMRoutineComplete = false;
    }
    /// <summary>
    /// 코코두기 상호작용 루틴 끝
    /// </summary>
    public void EndInteract(int i)
    {
        if (i == 0)
        {
            IsCMInteractComplete = true;
        }
        else if (i == 1)
        {
            IsCAInteractComplete = true;   
        }
    }
    /// <summary>
    /// 코코두기 상호작용 루틴 리셋
    /// </summary>
    public void ResetInteract(int i)
    {
        if (i == 0)
        {
            IsCMInteractComplete = false;
        }
        else if (i == 1)
        {
            IsCAInteractComplete = false;   
        }
    }

    // 애니메이션 시 에이전트 제어 근데 이곳에 쓰는게 맞나.
    public void FromAToB()
    {
        StartCoroutine(EndAnim());
    }
    protected IEnumerator EndAnim()
    {
        yield return new WaitForSeconds(1f);
        fsm.ChangeState(IdleState);
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
    public virtual void OnLobbyBeginDrag(Vector3 position)
    {
        if (fsm.CurrentState is IDragState drag) drag.OnBeginDrag(position);
        else fsm.ChangeState(DragState);
    }

    /// <summary>
    /// ILobbyDraggable, 드래그 중
    /// </summary>
    /// <param name="position"></param>
    public virtual void OnLobbyDrag(Vector3 position)
    {
        if (fsm.CurrentState is IDragState drag) drag.OnDrag(position);
        else fsm.ChangeState(DragState);
    }

    /// <summary>
    /// ILobbyDraggable, 드래그 끝
    /// </summary>
    /// <param name="position"></param>
    public virtual void OnLobbyEndDrag(Vector3 position)
    {
        if (fsm.CurrentState is IDragState drag) drag.OnEndDrag(position);
    }

    /// <summary>
    /// ILobbyInteractable, 터치 시 상호작용
    /// </summary>
    public virtual void OnLobbyClick()
    {
        fsm.ChangeState(ClickSate);
    }

    /// <summary>
    /// ILobbyPressable, 누를 시 상호작용
    /// </summary>
    public virtual void OnLobbyPress()
    {
        Debug.Log($"Press");
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        StartCoroutine(Pressing());
    }
    IEnumerator Pressing()
    {
        yield return new WaitForSeconds(0.06f);
        fsm.ChangeState(DragState);
        yield break;
    }

    /// <summary>
    /// ILobbyState, 일반모드 진입 시
    /// </summary>
    public virtual void InNormal()
    {
        fsm.ChangeState(IdleState);
    }

    /// <summary>
    /// ILobbyState, 편집모드 진입 시
    /// </summary>
    public virtual void InEdit()
    {
        fsm.ChangeState(EditState);
    }

    /// <summary>
    /// ILobbyState, 오브젝트 생성 시 등록
    /// </summary>
    public virtual void Register()
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
    public virtual void Unregister()
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

    /// <summary>
    /// 처음 생성 시 초기화
    /// </summary>
    public virtual void Init()
    {
        agent.height = 1f;
        agent.angularSpeed = 160f;
        YValue = transform.position.y;
        Debug.Log($"{gameObject.name} yValue : {YValue}, agent height : {agent.height}");
        IsLobbyEditMode = false;
        StuckTime = 2.5f;
    }
    /// <summary>
    /// Init() 후 초기화
    /// </summary>
    public virtual void PostInit()
    {
        fsm.ChangeState(IdleState);
        isInitComplete = true;
    }
    /// <summary>
    /// PostInit() 후 초기화
    /// </summary>
    public void LoadInit()
    {
        
    }
    /// <summary>
    /// LoadInit 후 초기화
    /// </summary>
    public void FinalInit()
    {
        
    }
}
