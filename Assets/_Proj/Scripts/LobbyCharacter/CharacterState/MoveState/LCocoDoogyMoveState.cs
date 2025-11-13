using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LCocoDoogyMoveState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;
    private readonly NavMeshAgentControl charAgent;
    private LCocoDoogyRouteManager route;
    //private int savedIndex = -1;

    public LCocoDoogyMoveState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm, NavMeshAgentControl charAgent, List<LobbyWaypoint> waypoints) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
        this.charAgent = charAgent;
        route = new LCocoDoogyRouteManager(waypoints);
        //AgentControl = owner.GetComponent<NavMeshAgentControl>();
    }

    public override void OnStateEnter()
    {
        if (!agent.enabled) agent.enabled = true;
        if (agent.enabled && agent.isStopped) agent.isStopped = false;
        //Debug.Log($"dd{waypoints.Length}");
        if (agent == null) Debug.Log($"agent null");
        //if (agent == null || waypoints.Length == 0) return;

        Debug.Log("코코두기 Move 진입");
        // if (savedIndex != -1) // 1
        // {
        //     agent.SetDestination(waypoints[savedIndex].position);
        //     savedIndex = -1;
        // }
        // else agent.SetDestination(waypoints[currentIndex].position);
        //agent.SetDestination(waypoints[currentIndex].position);

        //owner.EndRoutine();
    }

    public override void OnStateUpdate()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            //fsm.ChangeState(new LCocoDoogyInteractState(owner, fsm, waypoints));
            //fsm.ChangeState();
        }
        // 이동 중 멈춤 감지
        if (!agent.isStopped && agent.velocity.sqrMagnitude < 0.01f)
        {
            owner.StuckTimeA += Time.deltaTime;
            if (owner.StuckTimeA > owner.StuckTimeB)
            {
                fsm.ChangeState(owner.StuckState);
            }
        }
        else
        {
            owner.StuckTimeA = 0f;
        }

        Vector3 randomDir = owner.transform.position + Random.insideUnitSphere * 1f;
        randomDir.y = owner.YValue;
        agent.SetDestination(owner.transform.position + randomDir);
    }

    public override void OnStateExit()
    {
        //savedIndex = currentIndex; // 1
        //owner.StopAllCoroutines();
        agent.ResetPath();
    }

}
