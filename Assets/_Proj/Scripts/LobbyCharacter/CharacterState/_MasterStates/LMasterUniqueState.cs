using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class LMasterUniqueState : LobbyCharacterBaseState
{
    // 마스터는 Dragend면 startPoint로 가고 매니저에게 자기 도착했으면 이벤트 쏴서 사라지기
    private readonly NavMeshAgent agent;
    private Transform startPoint;
    private bool isComplete = false;
    private bool oneShot = false;

    public LMasterUniqueState(MasterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
        startPoint = owner.Waypoints[0].transform;
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
        if (!agent.enabled) agent.enabled = true;
        if (agent.enabled && agent.isStopped) agent.isStopped = false;
        (owner as MasterBehaviour).SetTimeToGoHome(true);
        isComplete = false;
        oneShot = false;
        owner.StartCoroutine(WaitAndMove());
    }
    public override void OnStateUpdate()
    {
        if (owner.IsDestroyed()) owner.StopAllCoroutines();
        if (isComplete && !oneShot)
        {
            // 로비매니저에게 자신을 SetActive false 해달라고 이벤트 쏘기
            if (LobbyCharacterManager.Instance) LobbyCharacterManager.RaiseCharacterEvent(owner);
            else if (LobbyCharacterManager_Friend.Instance) LobbyCharacterManager_Friend.RaiseCharacterEvent(owner);
            // 이벤트 쏘면 oneShot true로 일회성이기 때문 업데이트여서 여러번 이벤트가 되려나?
            oneShot = true;
        }
    }
    public override void OnStateExit()
    {
        isComplete = false;
        oneShot = false;
        owner.StopAllCoroutines();
    }

    public void UniqueStateAnim()
    {
        owner.StartCoroutine(WaitAndMove());
    }
    private IEnumerator WaitAndMove()
    {
        WaitForSeconds wait = new WaitForSeconds(2f);
        yield return wait;

        agent.SetDestination(startPoint.position);
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }
        yield return wait;
        
        isComplete = true;
        yield break;
    }
}
