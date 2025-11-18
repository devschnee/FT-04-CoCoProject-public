using UnityEngine;
using UnityEngine.AI;

public class LCocoDoogyEditState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;
    private readonly Animator anim;

    public LCocoDoogyEditState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
        anim = owner.GetComponent<Animator>();
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        if (agent.enabled) agent.enabled = false;
        anim.Play("Idle_A");
    }

    public override void OnStateUpdate() { }

    public override void OnStateExit()
    {
        agent.enabled = true;
        agent.isStopped = false;
    }

}

public class LMasterEditState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;
    private readonly Animator anim;

    public LMasterEditState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
        anim = owner.GetComponent<Animator>();
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        if (agent.enabled) agent.enabled = false;
        anim.Play("Idle_A");
    }

    public override void OnStateUpdate() { }

    public override void OnStateExit()
    {
        agent.enabled = true;
        agent.isStopped = false;
    }
}

public class LAnimalEditState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;
    private readonly Animator anim;

    public LAnimalEditState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
        anim = owner.GetComponent<Animator>();
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        if (agent.enabled) agent.enabled = false;
        anim.Play("Idle_A");
    }

    public override void OnStateUpdate() { }

    public override void OnStateExit()
    {
        agent.enabled = true;
        agent.isStopped = false;
    }
}
