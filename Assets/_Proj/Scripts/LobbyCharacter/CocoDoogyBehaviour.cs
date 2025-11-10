using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

// 코코두기가 안드로이드와 특정 범위 내에 있거나 랜덤 값으로
// 뽑힌 동물들에게 다가갔을때 로비 매니저에게 이벤트 호출, 로비매니저는
// 상호작용 실행.

public class CocoDoogyBehaviour : BaseLobbyCharacterBehaviour
{
    private int currentWaypointIndex; // 웨이포인트 이동 인덱스

    protected override void InitStates()
    {
        IdleState = new LCocoDoogyIdleState(this, fsm);
        MoveState = new LCocoDoogyMoveState(this, fsm, ref waypoints, ref currentWaypointIndex);
        InteractState = new LCocoDoogyInteractState(this, fsm);
        ClickSate = new LCocoDoogyClickState(this, fsm, charAnim);
        DragState = new LCocoDoogyDragState(this, fsm);
        EditState = new LCocoDoogyEditState(this, fsm);
        StuckState = new LCocoDoogyStuckState(this, fsm);
    }

    protected override void Awake()
    {
        gameObject.tag = "CocoDoogy";
        //gameObject.layer = LayerMask.NameToLayer("InLobbyObject");
        base.Awake();
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        currentWaypointIndex = 0;
        // for (int i = 0; i < InLobbyManager.Instance.cocoWaypoints.Length; i++)
        // {
        //     waypoints[i] = InLobbyManager.Instance.cocoWaypoints[i];
        // }
    }
    protected override void Start()
    {
        base.Start();
    }
    protected override void Update()
    {
        base.Update();
    }

    // 가자 멍뭉아
    // private IEnumerator LetsGoCoco(Transform[] waypoints)
    // {
    //     Debug.Log($"시작 인덱스 값 : {currentWaypointIndex}");
    //     isMoving = true;
    //     while (isMoving)
    //     {
    //         if (currentWaypointIndex == waypoints.Length - 1)
    //         {
    //             //charAgent.MoveToTransPoint(waypoints[currentWaypointIndex]);
    //             charAgent.CocoMoveToRandomTransPoint(waypoints[currentWaypointIndex]);
    //             currentWaypointIndex = 0;
    //         }
    //         if (currentWaypointIndex == 0)
    //         {
    //             charAgent.MoveToLastPoint(waypoints[currentWaypointIndex]);
    //             yield return waitFS;
    //         }
    //         currentWaypointIndex++;
    //         Debug.Log($"현재 인덱스 값 : {currentWaypointIndex}");
    //         //charAgent.MoveToTransPoint(waypoints[currentWaypointIndex]);
    //         charAgent.CocoMoveToRandomTransPoint(waypoints[currentWaypointIndex]);
    //         yield return waitU;

    //     }
    // }


    // protected override void HandleMove()
    // {
    //     agent.enabled = true;
    //     if (agent.enabled && agent.isStopped) agent.isStopped = false;
    //     if (!agent.hasPath)
    //     {
    //         if (currentWaypointIndex < InLobbyManager.Instance.cocoWaypoints.Length - 1)
    //         {
    //             //charAgent.CocoMoveToRandomTransPoint(InLobbyManager.Instance.cocoWaypoints[currentWaypointIndex]);
    //             Vector3 pos = InLobbyManager.Instance.cocoWaypoints[currentWaypointIndex].position;
    //             agent.SetDestination(pos);
    //         }
    //         else if (currentWaypointIndex == InLobbyManager.Instance.cocoWaypoints.Length)
    //         {
    //             ChangeState(LobbyCharacterState.Idle);
    //         }

    //     }

    //     // 거의 도착한 경우
    //     if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
    //     {
    //         if (agent.velocity.sqrMagnitude < 0.01f)
    //         {
    //             if (currentWaypointIndex < InLobbyManager.Instance.cocoWaypoints.Length - 1)
    //             {
    //                 currentWaypointIndex++;
    //             }
    //         }
    //     }

    //     // 이동 중 멈춤 감지
    //     if (!agent.isStopped && agent.velocity.sqrMagnitude < 0.01f)
    //     {
    //         stuckTimeA += Time.deltaTime;
    //         if (stuckTimeA > stuckTimeB)
    //         {
    //             ChangeState(LobbyCharacterState.Stuck);
    //         }
    //     }
    //     else
    //     {
    //         stuckTimeA = 0f;
    //     }
    // }

    // IEnumerator RecoverAndMove()
    // {
    //     yield return new WaitForSeconds(3f);

    //     if (agent.enabled && agent.isStopped) agent.isStopped = false;
    //     Vector3 pos = InLobbyManager.Instance.cocoWaypoints[currentWaypointIndex].position;
    //     agent.SetDestination(pos);
    //     //charAgent.CocoMoveToRandomTransPoint(InLobbyManager.Instance.cocoWaypoints[currentWaypointIndex]);
    //     stuckTimeB = 0f;

    //     ChangeState(LobbyCharacterState.Move);
    // }
    // 드래그나 편집모드 성공적으로 끝날 시 본인 위치 기준 가장 가까운 포인트 찾기 
    private int GetClosestWaypointIndex()
    {
        float minDistance = float.MaxValue;
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

    // 인터페이스 영역
    public override void OnCocoAnimalEmotion()
    {
        
    }
    public override void OnCocoMasterEmotion()
    {

    }
    public override void OnLobbyBeginDrag(Vector3 position)
    {
        base.OnLobbyBeginDrag(position);
    }
    public override void OnLobbyDrag(Vector3 position)
    {
        base.OnLobbyDrag(position);
    }
    public override void OnLobbyEndDrag(Vector3 position)
    {
        base.OnLobbyEndDrag(position);
        currentWaypointIndex = GetClosestWaypointIndex();
        //StartCoroutine(LetsGoCoco(InLobbyManager.Instance.cocoWaypoints));
    }
    public override void OnLobbyClick()
    {
        base.OnLobbyClick();
    }
    public override void OnLobbyPress()
    {
        base.OnLobbyPress();
    }
    public override void InNormal()
    {
        currentWaypointIndex = GetClosestWaypointIndex();
        base.InNormal();
        //StartCoroutine(LetsGoCoco(InLobbyManager.Instance.cocoWaypoints));
    }
    public override void InEdit()
    {
        base.InEdit();
    }
    public override void Register()
    {
        base.Register();
    }
    public override void Unregister()
    {
        base.Unregister();
    }
    public override void Init()
    {
        base.Init();
        agent.avoidancePriority = 60;
    }
    public override void PostInit()
    {
        base.PostInit();
    }

}
