using UnityEngine;
using UnityEngine.AI;

public class RandomWalker : MonoBehaviour, ILobbyInteractable, ILobbyDraggable, ILobbyPressable
{
    [SerializeField] float moveRadius = 10f; // 이동 범위
    [SerializeField] float waitTime = 2f; // 목표 지점 도달 후 대기 시간
    [SerializeField] float moveSpeed = 3.5f; // 이동 속도
    [SerializeField] float angularSpeed = 120f; // 턴 속도
    [SerializeField] float acceleration = 8f; // 가속도

    private NavMeshAgent agent;
    private Animator anim;
    private LobbyCharacterAnim animationController;
    private float timer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        animationController = new LobbyCharacterAnim(anim);
        agent.speed = moveSpeed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;
    }

    private void Start()
    {
        MoveToRandomPosition();
    }

    // 아마 로비에 생성되는 순간에는 Enable이 맞을지도?
    //private void OnEnable()
    //{
    //    MoveToRandomPosition();
    //}

    private void Update()
    {
        if (agent.speed != moveSpeed) agent.speed = moveSpeed;
        if (agent.angularSpeed != angularSpeed) agent.angularSpeed = angularSpeed;
        if (agent.acceleration != acceleration) agent.acceleration = acceleration;

        float speed = agent.velocity.magnitude;
        animationController.MoveAnim(speed);

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

    public void MoveToRandomPosition()
    {
        Vector3 randomDir = Random.insideUnitSphere * moveRadius;
        randomDir += transform.position;

        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, moveRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    public void OnLobbyInteract()
    {
        anim.Play("Jump");
    }

    public void OnLobbyBeginDrag(Vector3 position)
    {
        throw new System.NotImplementedException();
    }

    public void OnLobbyDrag(Vector3 position)
    {
        throw new System.NotImplementedException();
    }

    public void OnLobbyEndDrag(Vector3 position)
    {
        throw new System.NotImplementedException();
    }

    public void OnLobbyPress()
    {
        throw new System.NotImplementedException();
    }
}

