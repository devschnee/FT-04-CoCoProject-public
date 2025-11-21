using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class MasterBehaviour : BaseLobbyCharacterBehaviour
{
    public LMasterUniqueState UniqueState { get; private set; }

    protected override void InitStates()
    {
        IdleState = new LMasterIdleState(this, fsm);
        MoveState = new LMasterMoveState(this, fsm, charAgent);
        InteractState = new LMasterInteractState(this, fsm, charAnim);
        ClickSate = new LMasterClickState(this, fsm, charAnim);
        DragState = new LMasterDragState(this, fsm);
        EditState = new LMasterEditState(this, fsm);
        StuckState = new LMasterStuckState(this, fsm);
    }

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (fsm != null && fsm.CurrentState == MoveState) fsm.ChangeState(IdleState);
    }
    protected override void Start()
    {
        base.Start();
    }
    protected override void Update()
    {
        base.Update();
    }

    // 드래그 엔드 성공 시
    public void PlayStun()
    {
        anim.Play("Stunned");
    }
    public void ChangeUniqueState()
    {
        fsm.ChangeState(UniqueState);
    }

    //코코두기 상호작용
    public void EndMasterInteractState()
    {
        fsm.ChangeState(IdleState);
    }
    public void ChangeMasterInteractState()
    {
        fsm.ChangeState(InteractState);
    }
    public void EndCocoInteractState()
    {
        LobbyCharacterManager.Instance.GetCoco()?.EndCocoInteractState();
    }

    // 인터페이스 영역
    public override void OnLobbyBeginDrag(Vector3 position)
    {
        base.OnLobbyBeginDrag(position);
    }
    public override void OnLobbyDrag(Vector3 position)
    {
        base.OnLobbyDrag(position);
    }
    public override void OnLobbyEndDrag(Vector3 position)
    {
        base.OnLobbyEndDrag(position);
    }
    public override void OnLobbyClick()
    {
        base.OnLobbyClick();
    }
    public override void OnLobbyPress()
    {
        base.OnLobbyPress();
    }
    public override void InNormal()
    {
        base.InNormal();
    }
    public override void InEdit()
    {
        base.InEdit();
    }
    public override void InitWaypoint()
    {
        base.InitWaypoint();
        MoveState = null;
        MoveState = new LMasterMoveState(this, fsm, charAgent);
    }
    public override void Register()
    {
        base.Register();
    }
    public override void Unregister()
    {
        base.Unregister();
    }
    public override void Init()
    {
        base.Init();
    }
    public override void PostInit()
    {
        base.PostInit();
    }
    public override void LoadInit()
    {
        UniqueState = new LMasterUniqueState(this, fsm);
        base.LoadInit();
        agent.avoidancePriority = 10;
    }
    public override void FinalInit()
    {
        base.FinalInit();
    }
}
