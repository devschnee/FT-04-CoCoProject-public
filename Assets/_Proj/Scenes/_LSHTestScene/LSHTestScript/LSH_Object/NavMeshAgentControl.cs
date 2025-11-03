using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshAgentControl
{
    // NavMeshAgent �κ�
    private readonly NavMeshAgent agent;
    private float moveSpeed; // �̵� �ӵ�
    private float angularSpeed; // �� �ӵ�
    private float acceleration; // ���ӵ�

    // �� ��
    private float moveRadius; // �̵� ����
    private float waitTime; // ��ǥ ���� ���� �� ��� �ð�
    private float timer;
    private Transform transform;

    public NavMeshAgentControl(NavMeshAgent agent, float moveSpeed, float angularSpeed, float acceleration, float moveRadius, float waitTime, Transform transform)
    {
        this.agent = agent;
        this.moveSpeed = moveSpeed;
        this.angularSpeed = angularSpeed;
        this.acceleration = acceleration;
        this.moveRadius = moveRadius;
        this.waitTime = waitTime;
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

    public void WaitAndMove(Transform point)
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {

            timer += Time.deltaTime;
            if (timer >= waitTime)
            {
                MoveToPoint(point);
                timer = 0;
            }
        }
    }

    /// <summary>
    /// 이동
    /// </summary>
    /// <param name="point"></param>
    public void MoveToPoint(Transform point)
    {
        Vector3 pos = point.position;
        float speed = Random.Range(2f, 4f);
        agent.SetDestination(pos);
        agent.speed = speed;
        //agent.acceleration = Random.Range(speed, 20f);
        agent.stoppingDistance = Random.Range(0, 0.5f);
    }

    public void MoveToLastPoint(Transform point)
    {
        Vector3 lastPos = point.position;
        agent.SetDestination(lastPos);
        agent.speed = 2f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0f;
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
    public void AgentIsStop(bool which)
    {
        if (which == true) agent.isStopped = true;
        else agent.isStopped = false;
    }
    public void EnableAgent(bool which)
    {
        if (which == true) agent.enabled = true;
        else agent.enabled = false;
    }
    
}
