using UnityEngine;
using UnityEngine.AI;

public class ObjectNavMeshAgentController
{
    // NavMeshAgent 부분
    private readonly NavMeshAgent agent;
    private float moveSpeed; // 이동 속도
    private float angularSpeed; // 턴 속도
    private float acceleration; // 가속도

    // 그 외
    private float moveRadius; // 이동 범위
    private float waitTime; // 목표 지점 도달 후 대기 시간
    private float timer;
    private Transform transform;
    public Transform[] waypoints { private get; set; }

    public ObjectNavMeshAgentController(NavMeshAgent agent, float moveSpeed, float angularSpeed, float acceleration, float moveRadius, float waitTime, float timer, Transform transform)
    {
        this.agent = agent;
        this.moveSpeed = moveSpeed;
        this.angularSpeed = angularSpeed;
        this.acceleration = acceleration;
        this.moveRadius = moveRadius;
        this.waitTime = waitTime;
        this.timer = timer;
        this.transform = transform;
    }

    public float ValueOfMagnitude()
    {
        return agent.velocity.magnitude;
    }

    public void MoveValueChanged()
    {
        if (agent.speed != moveSpeed) agent.speed = moveSpeed;
        if (agent.angularSpeed != angularSpeed) agent.angularSpeed = angularSpeed;
        if (agent.acceleration != acceleration) agent.acceleration = acceleration;
        
    }

    public void WaitAndMove()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {

            timer += Time.deltaTime;
            if (timer >= waitTime)
            {
                MoveRandomPosition();
                timer = 0;
            }
        }
    }

    public void AgentStop()
    {
        agent.isStopped = true;
    }

    public void MoveRandomPosition()
    {
        Vector3 randomDir = Random.insideUnitSphere * moveRadius;
        randomDir += transform.position;

        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, moveRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
