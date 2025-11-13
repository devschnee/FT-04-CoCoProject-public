using UnityEngine;
using UnityEngine.AI;

public class LCocoDoogyClickState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;
    private readonly LobbyCharacterAnim charAnim;
    public LCocoDoogyClickState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm, LobbyCharacterAnim charAnim) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
        this.charAnim = charAnim;
    }

    public override void OnStateEnter()
    {
        Debug.Log("Click 진입");
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        // 애니메이션 이벤트 쪽에서 ChangeState해줌
        charAnim.InteractionAnim();

        AudioEvents.Raise(SFXKey.CocodoogyFootstep, pooled: true, pos: owner.transform.position);
    }
    
    public override void OnStateUpdate() { }

    public override void OnStateExit()
    {
        charAnim.DefaultAnimSpeed();
        if (agent.enabled && agent.isStopped) agent.isStopped = false;
    }
}

public class LMasterClickState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;
    private readonly LobbyCharacterAnim charAnim;
    public LMasterClickState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm, LobbyCharacterAnim charAnim) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
        this.charAnim = charAnim;
    }

    public override void OnStateEnter()
    {
        Debug.Log("Click 진입");
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        // 애니메이션 이벤트 쪽에서 ChangeState해줌
        charAnim.ClickMaster();

        // 소리 수정하슈
        AudioEvents.Raise(SFXKey.CocodoogyFootstep, pooled: true, pos: owner.transform.position);
    }
    
    public override void OnStateUpdate() { }

    public override void OnStateExit()
    {
        charAnim.DefaultAnimSpeed();
        if (agent.enabled && agent.isStopped) agent.isStopped = false;
        
    }
}

public class LAnimalClickState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;
    private readonly LobbyCharacterAnim charAnim;
    public LAnimalClickState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm, LobbyCharacterAnim charAnim) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
        this.charAnim = charAnim;
    }

    public override void OnStateEnter()
    {
        Debug.Log("Click 진입");
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        // 애니메이션 이벤트 쪽에서 ChangeState해줌
        charAnim.InteractionAnim();

        // 소리 수정하슈
        AudioEvents.Raise(SFXKey.CocodoogyFootstep, pooled: true, pos: owner.transform.position);
    }
    
    public override void OnStateUpdate() { }

    public override void OnStateExit()
    {
        charAnim.DefaultAnimSpeed();
        if (agent.enabled && agent.isStopped) agent.isStopped = false;
    }
}
