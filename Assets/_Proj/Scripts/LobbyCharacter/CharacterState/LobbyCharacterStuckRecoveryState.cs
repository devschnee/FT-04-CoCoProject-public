using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class LCocoDoogyStuckState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;

    public LCocoDoogyStuckState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
    }

    public override void OnStateEnter()
    {
        Debug.Log($"코코두기 Stuck&Recovery 진입");
        owner.StartCoroutine(RecoverAndMove());
    }

    public override void OnStateUpdate()
    {
        if (owner.gameObject.IsDestroyed()) owner.StopAllCoroutines();
    }

    public override void OnStateExit()
    {
        owner.StopAllCoroutines();
    }


    private IEnumerator RecoverAndMove()
    {
        yield return new WaitForSeconds(2f);

        if (agent.enabled && agent.isStopped) agent.isStopped = false;
        Vector3 randomDir = Random.insideUnitSphere * 0.3f;
        randomDir.y = owner.YValue;
        agent.SetDestination(owner.transform.position + randomDir);
        
        owner.StuckTimeA = 0f;
        if (agent.hasPath)
        {
            yield return null;
        }
        fsm.ChangeState(owner.MoveState);
        yield break;
    }
}

public class LMasterStuckState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;

    public LMasterStuckState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
    }

    public override void OnStateEnter()
    {
        Debug.Log($"마스터 Stuck&Recovery 진입");
        owner.StartCoroutine(RecoverAndMove());
    }

    public override void OnStateUpdate()
    {
        if (owner.gameObject.IsDestroyed()) owner.StopAllCoroutines();
    }

    public override void OnStateExit()
    {
        owner.StopAllCoroutines();
    }


    private IEnumerator RecoverAndMove()
    {
        yield return new WaitForSeconds(2f);

        if (agent.enabled && agent.isStopped) agent.isStopped = false;
        Vector3 randomDir = Random.insideUnitSphere * 0.3f;
        randomDir.y = owner.YValue;
        agent.SetDestination(owner.transform.position + randomDir);
        
        owner.StuckTimeA = 0f;

        fsm.ChangeState(owner.MoveState);
        yield break;
    }
}

public class LAnimalStuckState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;

    public LAnimalStuckState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
    }

    public override void OnStateEnter()
    {
        Debug.Log($"{owner.gameObject.name} Stuck&Recovery 진입");
        owner.StartCoroutine(RecoverAndMove());
    }

    public override void OnStateUpdate()
    {
        if (owner.gameObject.IsDestroyed()) owner.StopAllCoroutines();
    }

    public override void OnStateExit()
    {
        owner.StopAllCoroutines();
    }


    private IEnumerator RecoverAndMove()
    {
        yield return new WaitForSeconds(2f);

        if (agent.enabled && agent.isStopped) agent.isStopped = false;
        Vector3 randomDir = Random.insideUnitSphere * 0.3f;
        randomDir.y = owner.YValue;
        agent.SetDestination(owner.transform.position + randomDir);
        
        owner.StuckTimeA = 0f;

        fsm.ChangeState(owner.MoveState);
        yield break;
    }
}
