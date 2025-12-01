using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// NavMeshAgent 이용 시 고려해야할 것 isStopped, pathPending, hasPath 

[RequireComponent(typeof(UserInteractionHandler), typeof(Draggable), typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public abstract class BaseLobbyCharacterBehaviour : MonoBehaviour, ILobbyInteractable, ILobbyDraggable, ILobbyPressable, ILobbyState
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

    public List<LobbyWaypoint> Waypoints { get; protected set; }
    public float YValue { get; protected set; } // 생성 시 y축 얻고 드래그 시 해당 값 고정
    public float StuckTime { get; protected set; } // 멈칫둠칫
    public bool CMSetIsActive { get; protected set; }
    public bool hasBeenInit = false;

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

    protected virtual void Awake() { }

    protected virtual void OnEnable()
    {
        if (LobbyCharacterManager.Instance)
        {
            if (!LobbyCharacterManager.Instance.IsInitMode && !hasBeenInit)
            {
                Debug.Log($"{gameObject.name} : 내가 먼저 되냐?");
                StartCoroutine(InitMode());
            }
            if (fsm != null && LobbyCharacterManager.Instance.IsEditMode) fsm.ChangeState(EditState);
            if (fsm != null && !LobbyCharacterManager.Instance.IsEditMode) fsm.ChangeState(IdleState);
        }
        if (LobbyCharacterManager_Friend.Instance)
        {
            if (!LobbyCharacterManager_Friend.Instance.IsInitMode && !hasBeenInit)
            {
                Debug.Log($"{gameObject.name} : 내가 먼저 되냐?");
                StartCoroutine(InitMode());
            }
            if (fsm != null && LobbyCharacterManager_Friend.Instance.IsEditMode) fsm.ChangeState(EditState);
            if (fsm != null && !LobbyCharacterManager_Friend.Instance.IsEditMode) fsm.ChangeState(IdleState);
        }
    }

    protected virtual void Start() { }

    protected virtual void Update()
    {
        // Walk Run 애니메이션
        if (hasBeenInit)
        {
            //(charAnim != null)
            charAnim?.MoveAnim(charAgent.ValueOfMagnitude());
            //(fsm != null)
            fsm.UpdateState();
            //charAgent.MoveValueChanged();
        }
    }

    protected virtual void OnDisable()
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
    
    public void SetCocoMasterIsActive(bool which)
    {
        CMSetIsActive = which;
    }
    protected void StopMoving()
    {
        //fsm.ChangeState(IdleState);
        StopAllCoroutines();
        if (agent != null && agent.isActiveAndEnabled) agent.ResetPath();
    }

    public LobbyCharacterBaseState GetCurrentState()
    {
        var currentState = fsm.CurrentState;
        return currentState;
    }

    // 애니메이션 이벤트 마지막 부분에
    public void FromAToB()
    {
        StartCoroutine(EndAnim());
    }
    protected IEnumerator EndAnim()
    {
        yield return new WaitForSeconds(1f);
        fsm.ChangeState(IdleState);
    }
    /// <summary>
    /// 상호작용 상태로 만들어버리기
    /// </summary>
    public virtual void ChangeStateToInteractState()
    {
        fsm.ChangeState(InteractState);
    }
    /// <summary>
    /// 평상 시 상태로 만들어버리기
    /// </summary>
    public virtual void ChangeStateToIdleState()
    {
        fsm.ChangeState(IdleState);
    }

    // 인터페이스 영역
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
        yield return new WaitForSeconds(0.1f);
        fsm.ChangeState(DragState);
        yield break;
    }

    /// <summary>
    /// ILobbyState, 일반모드 진입 시
    /// </summary>
    public virtual void InNormal()
    {
        // 추가. MoveState로 각 포인트 지점으로 가는 행동을 하는데 MoveState도 초기화를 해야하나?
        fsm.ChangeState(IdleState);
    }

    /// <summary>
    /// ILobbyState, 편집모드 진입 시
    /// </summary>
    public virtual void InEdit()
    {
        fsm.ChangeState(EditState);
    }

    public virtual void InitWaypoint()
    {
        if (LobbyCharacterManager.Instance)
        {
            Waypoints = LobbyCharacterManager.Instance.GetWaypointsForChar(); // 다시 웨이포인트 얻기
        }
        if (LobbyCharacterManager_Friend.Instance)
        {
            Waypoints = LobbyCharacterManager_Friend.Instance.GetWaypointsForChar(); // 다시 웨이포인트 얻기
        }
    }
    /// <summary>
    /// ILobbyState, 오브젝트 생성 시 등록
    /// </summary>
    public virtual void Register()
    {
        if (!(LobbyCharacterManager.Instance || LobbyCharacterManager_Friend.Instance))
        {
            Debug.LogWarning("로비인터페이스 못 찾음");
            return;
        }
        if (LobbyCharacterManager.Instance)
        LobbyCharacterManager.Instance.RegisterLobbyChar(this);
        if (LobbyCharacterManager_Friend.Instance)
        LobbyCharacterManager_Friend.Instance.RegisterLobbyChar(this);
        Debug.Log($"{this} 등록");
    }

    /// <summary>
    /// ILobbyState, 오브젝트 삭제 시 취소
    /// </summary>
    public virtual void Unregister()
    {
        if (!(LobbyCharacterManager.Instance || LobbyCharacterManager_Friend.Instance))
        {
            Debug.LogWarning("로비인터페이스 못 찾음");
            return;
        }
        if (gameObject.CompareTag("CocoDoogy") || gameObject.CompareTag("Master")) return;
        if (LobbyCharacterManager.Instance)
            LobbyCharacterManager.Instance.UnregisterLobbyChar(this);
        if (LobbyCharacterManager_Friend.Instance)
            LobbyCharacterManager_Friend.Instance.UnregisterLobbyChar(this);
        Debug.Log($"{this} 삭제");
    }

    /// <summary>
    /// 처음 생성 시 초기화 Awake 같은
    /// </summary>
    public virtual void Init()
    {
        Debug.Log($"{gameObject.name} : 아니 내가 먼저 되는데?");

        if (LobbyCharacterManager.Instance)
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
            charAnim = new LobbyCharacterAnim(this, anim);
            MainCam = Camera.main;
            fsm = new LobbyCharacterFSM(null);
        }
        if (LobbyCharacterManager_Friend.Instance)
        {
            {
                gameObject.layer = LayerMask.NameToLayer("InLobbyObject");
            }
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponent<Animator>();
            Waypoints = LobbyCharacterManager_Friend.Instance.Waypoints;
            charAgent = new NavMeshAgentControl(agent, moveSpeed, angularSpeed, acceleration, moveRadius);
            charAnim = new LobbyCharacterAnim(this, anim);
            MainCam = Camera.main;
            fsm = new LobbyCharacterFSM(null);
        }
    }
    
    /// <summary>
    /// Init 후 초기화 Awake 다음에 올
    /// </summary>
    public virtual void PostInit()
    {
        InitStates();
    }
    /// <summary>
    /// PostInit 후 초기화 Start 같은
    /// </summary>
    public virtual void LoadInit()
    {
        agent.height = 1f;
        agent.angularSpeed = 160f;
        YValue = transform.position.y;
        Debug.Log($"{gameObject.name} yValue : {YValue}, agent height : {agent.height}");
        StuckTime = 2.5f;
        hasBeenInit = true;
    }
    /// <summary>
    /// LoadInit 후 초기화
    /// </summary>
    public virtual void FinalInit()
    {
        if (LobbyCharacterManager.Instance)
        {
            if (isActiveAndEnabled && !LobbyCharacterManager.Instance.IsEditMode) fsm.ChangeState(IdleState);
            else if(isActiveAndEnabled && LobbyCharacterManager.Instance.IsEditMode) fsm.ChangeState(EditState);
        }
        if (LobbyCharacterManager_Friend.Instance)
        {
            if (isActiveAndEnabled && !LobbyCharacterManager_Friend.Instance.IsEditMode) fsm.ChangeState(IdleState);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    protected IEnumerator InitMode()
    {
        Init();
        yield return null;
        PostInit();
        yield return null;
        LoadInit();
        yield return null;
        FinalInit();
        yield break;
    }
}
