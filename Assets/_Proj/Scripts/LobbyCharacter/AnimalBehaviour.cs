using System.Collections;
using UnityEngine;

public class AnimalBehaviour : BaseLobbyCharacterBehaviour
{
    public Transform TargetDeco { get; set; }
    
    protected override void InitStates()
    {
        IdleState = new LAnimalIdleState(this, fsm);
        MoveState = new LAnimalMoveState(this, fsm, charAgent);
        InteractState = new LAnimalInteractState(this, fsm);
        ClickSate = new LAnimalClickState(this, fsm, charAnim);
        DragState = new LAnimalDragState(this, fsm);
        EditState = new LAnimalEditState(this, fsm);
        StuckState = new LAnimalStuckState(this, fsm);
    }

    protected override void Awake()
    {
        gameObject.tag = "Animal";
        gameObject.layer = LayerMask.NameToLayer("InLobbyObject");
        base.Awake();
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        TargetDeco = null;
        agent.avoidancePriority = Random.Range(70, 90);
    }
    protected override void Start()
    {
        base.Start();
    }
    protected override void Update()
    {
        base.Update();
    }

    // 인터페이스 영역
    public override void OnCocoAnimalEmotion()
    {
        if (!agent.isStopped) agent.isStopped = true;

    }
    public override void OnCocoMasterEmotion() { }
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
    }
    public override void PostInit()
    {
        base.PostInit();
    }
}
