using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// 코코두기가 안드로이드와 특정 범위 내에 있거나 랜덤 값으로
// 뽑힌 동물들에게 다가갔을때 로비 매니저에게 이벤트 호출, 로비매니저는
// 상호작용 실행.

public class CocoDoogyBehaviour : BaseLobbyCharacterBehaviour
{
    public Vector3 LastDragEndPos { get; private set; }
    public bool IsDragged { get; private set; }
    public bool IsCMInteracted { get; private set; }
    public bool IsCAInteracted { get; private set; }
    public bool TimeToGoHome { get; private set; } // 코코두기 마지막 집가는 루틴이면 상호작용 막기
    private bool isInteracting;

    protected override void InitStates()
    {
        IdleState = new LCocoDoogyIdleState(this, fsm);
        MoveState = new LCocoDoogyMoveState(this, fsm, charAgent, Waypoints);
        InteractState = new LCocoDoogyInteractState(this, fsm, charAnim);
        ClickSate = new LCocoDoogyClickState(this, fsm, charAnim);
        DragState = new LCocoDoogyDragState(this, fsm);
        EditState = new LCocoDoogyEditState(this, fsm);
        StuckState = new LCocoDoogyStuckState(this, fsm);
    }

    protected override void Awake()
    {
        base.Awake();
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        if (fsm != null && fsm.CurrentState == MoveState) fsm.ChangeState(IdleState);
        IsDragged = false;
        LastDragEndPos = Waypoints[0].transform.position;
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
    }

    // 코코두기 상호작용 부분
    /// <summary>
    /// 코코두기의 상호작용. 0 = 마스터 상호작용 함, 1 = 동물 상호작용 함, 2 = 둘다 리셋 false로
    /// </summary>
    /// <param name="i"></param>
    public void SetCharInteracted(int i)
    {
        if (i == 0)
        {
            IsCMInteracted = true;
        }
        else if (i == 1)
        {
            IsCAInteracted = true;  
        }
        else if (i == 2)
        {
            IsCMInteracted = false;
            IsCAInteracted = false;
        }
    }
    public void SetTimeToGoHome(bool which)
    {
        if (which) TimeToGoHome = true;
        else TimeToGoHome = false;
    }
    public override void ChangeStateToIdleState()
    {
        base.ChangeStateToIdleState();
        ResetInteracting();
    }
    public override void ChangeStateToInteractState()
    {
        base.ChangeStateToInteractState();
    }
    public void SetLastDragEndPos(Vector3 pos)
    {
        LastDragEndPos = pos;
    }
    public void SetIsDragged(bool which)
    {
        IsDragged = which;
    }
    public void ResetInteracting()
    {
        isInteracting = false;
    }
    public void ChangeOtherCharStateToInteractState(string name)
    {
        if (LobbyCharacterManager.Instance)
        {
            if (name == "Master")
            {
                LobbyCharacterManager.Instance.GetMaster()?.ChangeStateToInteractState();
            }
            else if (name == "Animal")
            {
                LobbyCharacterManager.Instance.GetAnimal()?.ChangeStateToInteractState();
            }
        }
        if (LobbyCharacterManager_Friend.Instance)
        {
            if (name == "Master")
            {
                LobbyCharacterManager_Friend.Instance.GetMaster()?.ChangeStateToInteractState();
            }
            else if (name == "Animal")
            {
                LobbyCharacterManager_Friend.Instance.GetAnimal()?.ChangeStateToInteractState();
            }
        }
    }
    public void ChangeAnimalStateToInteractState()
    {
        if (LobbyCharacterManager.Instance)
        LobbyCharacterManager.Instance.GetAnimal()?.ChangeStateToInteractState();
        if (LobbyCharacterManager_Friend.Instance)
        LobbyCharacterManager_Friend.Instance.GetAnimal()?.ChangeStateToInteractState();
    }
    // 인터페이스 영역
    /// <summary>
    /// 코코두기와 동물들 상호작용
    /// </summary>
    public void OnCocoAnimalEmotion()
    {
        if(!(fsm.CurrentState == MoveState) || IsCAInteracted == true || TimeToGoHome) return;
        if (isInteracting == false)
        {
            (InteractState as LCocoDoogyInteractState).SetCAM(0, true);
            fsm.ChangeState(InteractState);
            isInteracting = true;
        }
        else return;
    }
    /// <summary>
    /// 코코두기와 마스터 상호작용
    /// </summary>
    public void OnCocoMasterEmotion()
    {
        if (LobbyCharacterManager.Instance)
        {
            bool masterGoHome = LobbyCharacterManager.Instance.GetMaster().TimeToGoHome;
            if (!(fsm.CurrentState == MoveState) || masterGoHome == true || IsCMInteracted == true || TimeToGoHome) return;
            if (isInteracting == false)
            {
                (InteractState as LCocoDoogyInteractState).SetCAM(1, true);
                fsm.ChangeState(InteractState);
                isInteracting = true;
            }
            else return;
        }
        if (LobbyCharacterManager_Friend.Instance)
        {
            bool masterGoHome = LobbyCharacterManager_Friend.Instance.GetMaster().TimeToGoHome;
            if (!(fsm.CurrentState == MoveState) || masterGoHome == true || IsCMInteracted == true || TimeToGoHome) return;
            if (isInteracting == false)
            {
                (InteractState as LCocoDoogyInteractState).SetCAM(1, true);
                fsm.ChangeState(InteractState);
                isInteracting = true;
            }
            else return;
        }
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
    public override void InitWaypoint()
    {
        base.InitWaypoint();
        MoveState = null;
        MoveState = new LCocoDoogyMoveState(this, fsm, charAgent, Waypoints);
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
        base.LoadInit();
        isInteracting = false;
        IsCMInteracted = false;
        IsCAInteracted = false;
        TimeToGoHome = false;
        agent.avoidancePriority = 20;
    }
    public override void FinalInit()
    {
        base.FinalInit();
    }

}
