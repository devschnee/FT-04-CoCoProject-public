using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class LAnimalMoveState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;
    private readonly NavMeshAgentControl charAgent;
    private float decoDetectRadius = 20f;
    private Transform targetDeco;

    public LAnimalMoveState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm, NavMeshAgentControl charAgent) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
        this.charAgent = charAgent;
        targetDeco = (owner as AnimalBehaviour).TargetDeco;
        //AgentControl = owner.GetComponent<NavMeshAgentControl>();
    }

    public override void OnStateEnter()
    {
        if (!agent.enabled) agent.enabled = true;
        if (agent.enabled && agent.isStopped) agent.isStopped = false;

        Debug.Log($"{owner.gameObject.name} Move 진입");

        //owner.EndRoutine();
        owner.StartCoroutine(Move());
    }
    public override void OnStateUpdate()
    {
        if (owner.gameObject.IsDestroyed()) owner.StopAllCoroutines();
        
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
    }
    public override void OnStateExit()
    {
        owner.StopAllCoroutines();
    }
    
    private IEnumerator Move()
    {
        while (true)
        {
            FindNearestDeco();
            if (targetDeco != null)
            {
                charAgent.MoveToRandomTransPoint(targetDeco);
            }
            else
            {
                charAgent.MoveToRandomTransPoint(owner.transform);
            }

            if (!agent.hasPath) charAgent.MoveToRandomTransPoint(owner.transform);
            yield return (owner as AnimalBehaviour).WaitU;
        }
    }
    private void FindNearestDeco()
    {
        // 나중에 로비 안에 활성화 된 데코 리스트가 있으면 대체
        GameObject[] decos = GameObject.FindGameObjectsWithTag("Decoration");
        float nearestDist = float.MaxValue;
        Transform nearest = null;

        foreach (GameObject deco in decos)
        {
            float dist = Vector3.Distance(owner.transform.position, deco.transform.position);
            if (dist < decoDetectRadius && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = deco.transform;
            }
        }
        targetDeco = nearest;
    }
}
