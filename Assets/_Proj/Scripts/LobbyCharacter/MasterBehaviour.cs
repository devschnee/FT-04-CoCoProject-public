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
        InteractState = new LMasterInteractState(this, fsm);
        ClickSate = new LMasterClickState(this, fsm, charAnim);
        DragState = new LMasterDragState(this, fsm);
        EditState = new LMasterEditState(this, fsm);
        StuckState = new LMasterStuckState(this, fsm);
    }

    protected override void Awake()
    {
        gameObject.tag = "Master";
        base.Awake();
        //currentDecoIndex = 0;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }
    protected override void Start()
    {
        base.Start();
        UniqueState = new LMasterUniqueState(this, fsm);
    }
    protected override void Update()
    {
        base.Update();
    }

    public void PlayStun()
    {
        anim.Play("Stunned");
    }
    public void ChangeUniqueState()
    {
        fsm.ChangeState(UniqueState);
    }

    // 인터페이스 영역
    public override void OnCocoAnimalEmotion()
    {
        
    }
    public override void OnCocoMasterEmotion()
    {

    }
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
        agent.avoidancePriority = 10;
    }
    public override void PostInit()
    {
        base.PostInit();
    }
}
