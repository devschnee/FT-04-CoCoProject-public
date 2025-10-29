using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshAgentControl
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

    public NavMeshAgentControl(NavMeshAgent agent, float moveSpeed, float angularSpeed, float acceleration, float moveRadius, float waitTime, float timer, Transform transform)
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

    public void NewWrap()
    {
        agent.Warp(transform.position);
        Debug.Log($"{agent.Warp(transform.position)}");
    }

    // 이동
    public void MoveToPoint(Transform point)
    {
        Vector3 pos = point.position;
        float speed = Random.Range(3f, 7f);
        agent.SetDestination(pos);
        agent.speed = speed;
        //agent.acceleration = Random.Range(speed, 20f);
        agent.stoppingDistance = Random.Range(0, 0.5f);
    }

    public void MoveToLastPoint(Transform point)
    {
        Vector3 lastPos = point.position;
        agent.SetDestination(lastPos);
        agent.speed = 3f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0f;
    }

    public void LetsGoCoco(ref int currentIndex, Transform[] waypoints)
    {
        Debug.Log($"에이전트 상태 : {agent.pathPending}");
        if (agent.enabled && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentIndex++;
            Debug.Log($"1현재 인덱스 값 : {currentIndex}");
            Debug.Log($"1현재 웨이포인트 길이 : {waypoints.Length}");
            if (currentIndex < waypoints.Length)
            {
                if (waypoints[currentIndex] == null)
                {
                    Debug.Log("2다음 포인트 없는뎁숑");
                }
                MoveToPoint(waypoints[currentIndex]);
            }
            else if (currentIndex > waypoints.Length)
            {
                currentIndex = 0;
                MoveToPoint(waypoints[currentIndex]);
            }

        }
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

    //
    public void AgentIsStop(bool stop)
    {
        if (stop) agent.isStopped = true;
        else agent.isStopped = false;
    }
    public void EnableAgent(bool which)
    {
        if (which == true) agent.enabled = true;
        else agent.enabled = false;
    }
}
