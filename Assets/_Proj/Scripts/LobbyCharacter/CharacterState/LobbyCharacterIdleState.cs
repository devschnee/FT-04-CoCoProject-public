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
        base.OnStateEnter();
        if (!agent.enabled) agent.enabled = true;
        agent.Warp(owner.transform.position);

        owner.StartCoroutine(WaitThenMove());
    }

    public override void OnStateUpdate()
    {
        if (owner.IsDestroyed() || !owner.isActiveAndEnabled) owner.StopAllCoroutines();
        if (LobbyCharacterManager.Instance.IsEditMode && fsm.CurrentState != owner.EditState)
        {
            fsm.ChangeState(owner.EditState);
        }
    }

    public override void OnStateExit()
    {
        owner.StopCoroutine(WaitThenMove());
    }
    
    private IEnumerator WaitThenMove()
    {
        yield return new WaitForSeconds(3f);
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
        base.OnStateEnter();
        if (!agent.enabled) agent.enabled = true;
        agent.Warp(owner.transform.position);

        owner.StartCoroutine(WaitThenMove());
    }

    public override void OnStateUpdate()
    {
        if (owner.IsDestroyed() || !owner.isActiveAndEnabled) owner.StopAllCoroutines();
        if (LobbyCharacterManager.Instance.IsEditMode && fsm.CurrentState != owner.EditState)
        {
            fsm.ChangeState(owner.EditState);
        }
    }

    public override void OnStateExit()
    {
        owner.StopCoroutine(WaitThenMove());
    }
    
    private IEnumerator WaitThenMove()
    {
        yield return new WaitForSeconds(3f);
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
        base.OnStateEnter();
        if (!agent.enabled) agent.enabled = true;
        agent.Warp(owner.transform.position);
        
        owner.StartCoroutine(WaitThenMove());
    }

    public override void OnStateUpdate()
    {
        if (owner.IsDestroyed() || !owner.isActiveAndEnabled) owner.StopAllCoroutines();
        if (LobbyCharacterManager.Instance.IsEditMode && fsm.CurrentState != owner.EditState)
        {
            fsm.ChangeState(owner.EditState);
        }
    }

    public override void OnStateExit()
    {
        owner.StopCoroutine(WaitThenMove());
    }
    
    private IEnumerator WaitThenMove()
    {
        yield return new WaitForSeconds(3f);
        fsm.ChangeState(owner.MoveState);
        yield break;
    }
}
