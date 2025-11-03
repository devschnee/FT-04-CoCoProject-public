using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CocoDoogyBehaviour : BaseLobbyCharacterBehaviour
{
    private int currentWaypointIndex; // 웨이포인트 이동 인덱스
    private bool isMoving;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        currentWaypointIndex = 0;
        
    }

    private void Update()
    {
        //StartCoroutine(Moving());
        LetsGoCoco(InLobbyManager.Instance.cocoWaypoints);
        charAnim.MoveAnim(charAgent.ValueOfMagnitude());
    }

    private void LetsGoCoco(Transform[] waypoints)
    {
        if (agent.enabled && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            Debug.Log($"현재 인덱스 : {currentWaypointIndex}");
            Debug.Log($"웨이포인트 총 : {waypoints.Length}");
            currentWaypointIndex++;
            if (currentWaypointIndex < waypoints.Length)
            {
                charAgent.MoveToPoint(waypoints[currentWaypointIndex]);
                if (currentWaypointIndex == waypoints.Length - 1)
                {
                    charAgent.WaitAndMove(waypoints[currentWaypointIndex]);
                    currentWaypointIndex = 0;
                }
                if (waypoints[currentWaypointIndex] == null)
                {
                    Debug.Log("웨이포인트 없음");
                }
            }
        }
    }
    private int GetClosestWaypointIndex()
    {
        float minDistance = 1000f;
        int closestIndex = 0;

        for (int i = 0; i < InLobbyManager.Instance.cocoWaypoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, InLobbyManager.Instance.cocoWaypoints[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    private void MoveToNextWaypoint()
    {
        if (InLobbyManager.Instance.cocoWaypoints == null || InLobbyManager.Instance.cocoWaypoints.Length == 0) return;
        if (agent.isStopped) charAgent.AgentIsStop(false);
        charAgent.MoveToPoint(InLobbyManager.Instance.cocoWaypoints[currentWaypointIndex]);

    }

    private void MoveRandomPosition()
    {
        Vector3 randomDir = Random.insideUnitSphere * moveRadius;
        randomDir += transform.position;

        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, moveRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private IEnumerator Moving()
    {
        isMoving = true;
        if (isMoving)
        {
            LetsGoCoco(InLobbyManager.Instance.cocoWaypoints);
            yield return null;
            
        }
    } 
    // 인터페이스 영역

    public override void OnCocoAnimalEmotion()
    {

    }

    public override void OnCocoMasterEmotion()
    {
        
    }

    public override void OnLobbyEndDrag(Vector3 position)
    {
        base.OnLobbyEndDrag(position);
        currentWaypointIndex = GetClosestWaypointIndex();
        MoveToNextWaypoint();
    }

    public override void OnLobbyInteract()
    {
        base.OnLobbyInteract();
        AudioEvents.Raise(SFXKey.CocodoogyFootstep, pooled: true, pos: transform.position);
    }

    public override void InNormal()
    {
        base.InNormal();
    }

    public override void InUpdate()
    {
        
    }

    public override void StartScene()
    {
        
    }

    public override void ExitScene()
    {
        
    }
}
