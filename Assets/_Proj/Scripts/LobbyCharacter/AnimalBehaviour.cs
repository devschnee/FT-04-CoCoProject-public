using System;
using UnityEngine;

[Serializable]
public class AnimalPositionEntry
{
    public string objectName;
    public Vector3 objectPos;
}
public class AnimalBehaviour : BaseLobbyCharacterBehaviour, IQuestBehaviour
{
    public Transform TargetDeco { get; set; }
    [SerializeField] AnimalType animalType;
    
    protected override void InitStates()
    {
        IdleState = new LAnimalIdleState(this, fsm);
        MoveState = new LAnimalMoveState(this, fsm, charAgent);
        InteractState = new LAnimalInteractState(this, fsm, charAnim);
        ClickSate = new LAnimalClickState(this, fsm, charAnim);
        DragState = new LAnimalDragState(this, fsm);
        EditState = new LAnimalEditState(this, fsm);
        StuckState = new LAnimalStuckState(this, fsm);
    }

    protected override void Awake()
    {
        base.Awake();
        Register();
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        if (LobbyCharacterManager.Instance)
        {
            if (!LobbyCharacterManager.Instance.IsInitMode) 
                agent.avoidancePriority = UnityEngine.Random.Range(50, 70);
            ETCEvent.OnSaveAnimalPositions += HandleSetAnimalPos;
            ETCEvent.OnDeleteAnimalPosition += HandleDeleteAnimalPos;
        }
        if (LobbyCharacterManager_Friend.Instance)
        {
            if (!LobbyCharacterManager_Friend.Instance.IsInitMode) 
                agent.avoidancePriority = UnityEngine.Random.Range(50, 70);
            ETCEvent.OnSaveAnimalPositions += HandleSetAnimalPos;
            ETCEvent.OnDeleteAnimalPosition += HandleDeleteAnimalPos;
        }
    }
    protected override void Start()
    {
        base.Start();
    }
    protected override void Update()
    {
        base.Update();
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        ETCEvent.OnSaveAnimalPositions -= HandleSetAnimalPos;
        ETCEvent.OnDeleteAnimalPosition -= HandleDeleteAnimalPos;
    }

    // 코코두기 상호작용
    public override void ChangeStateToIdleState()
    {
        base.ChangeStateToIdleState();
    }
    public override void ChangeStateToInteractState()
    {
        base.ChangeStateToInteractState();
    }

    private void HandleSetAnimalPos()
    {
        SettingManager.Instance.SetAnimalPosition(gameObject.name, gameObject.transform.position);
        SettingManager.Instance.SaveSettings();
    }

    private void HandleDeleteAnimalPos(Transform t)
    {
        SettingManager.Instance.RemoveAnimalPosition(t.name);
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
        QuestManager.Instance.Handle(this);
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
        gameObject.layer = LayerMask.NameToLayer("InLobbyObject");
        base.Init();
    }
    public override void PostInit()
    {
        base.PostInit();
        TargetDeco = null;
    }
    public override void LoadInit()
    {
        base.LoadInit();

        string name = gameObject.name;

        //if (SettingManager.Instance.TryGetAnimalPosition(name, out Vector3 pos))
        //{
        //    if (agent != null) agent.Warp(pos);
        //    else transform.position = pos;
        //}
    }
    public override void FinalInit()
    {
        base.FinalInit();
    }
}
