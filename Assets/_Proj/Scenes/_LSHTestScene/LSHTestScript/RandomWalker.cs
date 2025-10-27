using UnityEngine;
using UnityEngine.AI;

public class RandomWalker : MonoBehaviour
{
    public float moveRadius = 10f; // 이동 범위
    public float waitTime = 2f; // 목표 지점 도달 후 대기 시간
    public float moveSpeed = 3.5f; // 이동 속도
    public float angularSpeed = 120f; // 턴 속도
    public float acceleration = 8f; // 가속도

    private NavMeshAgent agent;
    private Animator anim;
    private float timer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        agent.speed = moveSpeed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;
    }

    void Start()
    {
        MoveToRandomPosition();
    }

    void Update()
    {
        if (agent.speed != moveSpeed) agent.speed = moveSpeed;
        if (agent.angularSpeed != angularSpeed) agent.angularSpeed = angularSpeed;
        if (agent.acceleration != acceleration) agent.acceleration = acceleration;

        float speed = agent.velocity.magnitude;
        anim.SetFloat("Speed", speed);

        // 목적지 도착했으면 일정 시간 기다렸다가 다시 이동
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            
            timer += Time.deltaTime;
            if (timer >= waitTime)
            {
                MoveToRandomPosition();
                timer = 0;
            }
        }
    }

    void MoveToRandomPosition()
    {
        Vector3 randomDir = Random.insideUnitSphere * moveRadius;
        randomDir += transform.position;

        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, moveRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}

