using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 코코두기
/// </summary>
public class LCocoDoogyIdleState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;

    public LCocoDoogyIdleState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
    }

    public override void OnStateEnter()
    {
        if (!agent.enabled) agent.enabled = true;
        agent.Warp(owner.transform.position);
        Debug.Log("코코두기 Idle 진입");
        owner.StartCoroutine(WaitThenMove());
    }

    public override void OnStateUpdate() { }

    public override void OnStateExit()
    {
        owner.StopAllCoroutines();
    }
    
    private IEnumerator WaitThenMove()
    {
        yield return new WaitForSeconds(Random.Range(1.5f, 3f));
        fsm.ChangeState(owner.MoveState);
        yield break;
    }
}

/// <summary>
/// 마스터
/// </summary>
public class LMasterIdleState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;

    public LMasterIdleState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
    }

    public override void OnStateEnter()
    {
        if (!agent.enabled) agent.enabled = true;
        agent.Warp(owner.transform.position);
        Debug.Log("마스터 Idle 진입");
        owner.StartCoroutine(WaitThenMove());
    }

    public override void OnStateUpdate() { }

    public override void OnStateExit()
    {
        owner.StopAllCoroutines();
    }
    
    private IEnumerator WaitThenMove()
    {
        yield return new WaitForSeconds(Random.Range(1.5f, 3f));
        fsm.ChangeState(owner.MoveState);
        yield break;
    }
}

/// <summary>
/// 동물
/// </summary>
public class LAnimalIdleState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;

    public LAnimalIdleState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
    }

    public override void OnStateEnter()
    {
        if (!agent.enabled) agent.enabled = true;
        agent.Warp(owner.transform.position);
        Debug.Log($"{owner.gameObject.name} Idle 진입");
        owner.StartCoroutine(WaitThenMove());
    }

    public override void OnStateUpdate() { }

    public override void OnStateExit()
    {
        owner.StopAllCoroutines();
    }
    
    private IEnumerator WaitThenMove()
    {
        yield return new WaitForSeconds(Random.Range(1.5f, 3f));
        fsm.ChangeState(owner.MoveState);
        yield break;
    }
}
