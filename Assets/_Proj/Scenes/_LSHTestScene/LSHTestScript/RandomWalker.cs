using UnityEngine;
using UnityEngine.AI;

public class RandomWalker : MonoBehaviour, ILobbyInteractable, ILobbyDraggable, ILobbyPressable
{
    [SerializeField] float moveRadius = 10f; // �̵� ����
    [SerializeField] float waitTime = 2f; // ��ǥ ���� ���� �� ��� �ð�
    [SerializeField] float moveSpeed = 3.5f; // �̵� �ӵ�
    [SerializeField] float angularSpeed = 120f; // �� �ӵ�
    [SerializeField] float acceleration = 8f; // ���ӵ�

    private NavMeshAgent agent;
    private Animator anim;
    private LobbyCharacterAnim animationController;
    private float timer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        animationController = new LobbyCharacterAnim(this, anim);
        agent.speed = moveSpeed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;
    }

    private void Start()
    {
        MoveToRandomPosition();
    }

    // �Ƹ� �κ� �����Ǵ� �������� Enable�� ��������?
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

        // ������ ���������� ���� �ð� ��ٷȴٰ� �ٽ� �̵�
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

    public void OnLobbyClick()
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

