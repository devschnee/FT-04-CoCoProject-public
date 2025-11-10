using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class LCocoDoogyInteractState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;

    public LCocoDoogyInteractState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
    }

    public override void OnStateEnter()
    {
        if (!agent.enabled) agent.enabled = true;
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        Debug.Log("코코두기 Interact 진입");
        owner.StartCoroutine(WaitAndFinish());
    }
    public override void OnStateUpdate()
    {
        if (owner.IsDestroyed()) owner.StopAllCoroutines();
    }
    public override void OnStateExit()
    {
        owner.StopAllCoroutines();
        agent.isStopped = false;
    }

    private IEnumerator WaitAndFinish()
    {
        yield return new WaitForSeconds(3f);
        owner.EndInteract(0);
        fsm.ChangeState(owner.IdleState);
        yield break;
    }
}

public class LMasterInteractState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;

    public LMasterInteractState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
    }

    public override void OnStateEnter()
    {
        if (!agent.enabled) agent.enabled = true;
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        Debug.Log("마스터 Interact 진입");
        owner.StartCoroutine(WaitAndFinish());
    }
    public override void OnStateUpdate()
    {
        if (owner.IsDestroyed()) owner.StopAllCoroutines();
    }
    public override void OnStateExit()
    {
        owner.StopAllCoroutines();
        agent.isStopped = false;
    }

    private IEnumerator WaitAndFinish()
    {
        yield return new WaitForSeconds(3f);
        owner.EndInteract(0);
        fsm.ChangeState(owner.IdleState);
        yield break;
    }
}

public class LAnimalInteractState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;

    public LAnimalInteractState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
    }

    public override void OnStateEnter()
    {
        if (!agent.enabled) agent.enabled = true;
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        Debug.Log($"{owner.gameObject.name} Interact 진입");
        owner.StartCoroutine(WaitAndFinish());
    }
    public override void OnStateUpdate()
    {
        if (owner.IsDestroyed()) owner.StopAllCoroutines();
    }
    public override void OnStateExit()
    {
        owner.StopAllCoroutines();
        agent.isStopped = false;
    }

    private IEnumerator WaitAndFinish()
    {
        yield return new WaitForSeconds(3f);
        owner.EndInteract(0);
        fsm.ChangeState(owner.IdleState);
        yield break;
    }
}
