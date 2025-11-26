using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class LMasterMoveState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;
    private readonly NavMeshAgentControl charAgent;
    private LMasterRouteManager route;
    private Transform startPoint; // 지금은 마스터의 포지션으로 되어있는데 로비매니저 정리되면 스타트 
    private WaitForSeconds wait = new(1f);
    private float timeToStuck = 0;

    public LMasterMoveState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm, NavMeshAgentControl charAgent) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
        this.charAgent = charAgent;
        startPoint = owner.Waypoints[0].transform;
        route = new LMasterRouteManager(startPoint);
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
        if (!agent.enabled) agent.enabled = true;
        if (agent.enabled && agent.isStopped) agent.isStopped = false;

        // 루틴 초기화
        route.RefreshDecoList();
        //owner.EndRoutine();
        owner.StartCoroutine(MoveRoutine());
        
    }
    public override void OnStateUpdate()
    {
        if (owner.gameObject.IsDestroyed()) owner.StopAllCoroutines();
        
        // 코코두기 상호작용
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            //fsm.ChangeState(new LCocoDoogyInteractState(owner, fsm, waypoints));
            //fsm.ChangeState();
        }
        // 이동 중 멈춤 감지
        if (!agent.isStopped && agent.velocity.sqrMagnitude < 0.01f)
        {
            timeToStuck += Time.deltaTime;
            if (timeToStuck > owner.StuckTime)
            {
                fsm.ChangeState(owner.StuckState);
            }
        }
        else
        {
            timeToStuck = 0f;
        }
    }
    public override void OnStateExit()
    {
        timeToStuck = 0f;
        owner.StopAllCoroutines();
        agent.ResetPath();
    }

    private IEnumerator MoveRoutine()
    {
        while (true)
        {
            Transform next = route.GetNextDeco();

            if (route.hasComplete)
            {
                // 루틴 끝
                charAgent.MoveToLastPoint(startPoint);
                (owner as MasterBehaviour).SetTimeToGoHome(true);
                while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                {
                    yield return null;
                }

                yield return wait;
                // 로비 매니저에게 나 끝났어요 호출 하면 로비매니저가 SetActive false 처리
                fsm.ChangeState(owner.EditState);
                LobbyCharacterManager.RaiseCharacterEvent(owner);

                yield break;
            }

            if (next == null)
            {
                Debug.Log("(decoList == null || decoList.Count == 0 스타트 지점으로 감");
                charAgent.MoveToLastPoint(startPoint);
                while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                {
                    yield return null;
                }

                fsm.ChangeState(owner.IdleState);
                yield break;
            }
            else
            {
                if (route.firstTime)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        charAgent.MasterMoveToRandomTransPoint(owner, startPoint);
                        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                        {
                            yield return null;
                        }
                    }

                    charAgent.MoveToLastPoint(startPoint);
                    while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                    {
                        yield return null;
                    }
                    yield return wait;

                    route.ResetFirstTime();
                    fsm.ChangeState(owner.EditState);
                    // 로비 매니저에게 나 끝났어요 호출 하면 로비매니저가 SetActive false 처리
                    LobbyCharacterManager.RaiseCharacterEvent(owner);
                    yield break;
                }
                else
                {
                    charAgent.MoveToTransPoint(next);
                    while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                    {
                        yield return null;
                    }
                }
            }
        }
    }
}
