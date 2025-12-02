using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class LCocoDoogyMoveState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;
    private readonly NavMeshAgentControl charAgent;
    private LCocoDoogyRouteManager route;
    private WaitForSeconds wait = new(1f);
    private Vector3 draggedPos;
    private bool isDragged;
    private bool init = false;
    private float timeToStuck = 0;
    private int repeatTime = 0;
    private int currentIndex = 1;

    public LCocoDoogyMoveState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm, NavMeshAgentControl charAgent, List<LobbyWaypoint> waypoints) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
        this.charAgent = charAgent;
        route = new LCocoDoogyRouteManager(waypoints);
    }
    public override void OnStateEnter()
    {
        base.OnStateEnter();
        if (!agent.enabled) agent.enabled = true;
        if (agent.enabled && agent.isStopped) agent.isStopped = false;
        if (agent == null) Debug.Log($"agent null");

        isDragged = (owner as CocoDoogyBehaviour).IsDragged;
        // 드래그 엔드하면 Enter에서 두갈래로 가야하겠지?
        if (isDragged)
        {
            draggedPos = (owner as CocoDoogyBehaviour).LastDragEndPos;
            route.RearragneList(currentIndex, draggedPos);
            isDragged = false;
            (owner as CocoDoogyBehaviour).SetIsDragged(false);
        }
        else if (!init)
        {
            route.RefreshList();
            init = true;
        }
        Debug.Log($"코코두기 웨이포인트 총 길이{route.moveWaypoints.Count}");
        owner.StartCoroutine(LetsGoCoco());

        //owner.EndRoutine();
    }
    public override void OnStateUpdate()
    {
        if (owner.IsDestroyed() || owner.gameObject.activeSelf == false)
        {
            owner.StopAllCoroutines();
            agent.ResetPath();
        }
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            //fsm.ChangeState(new LCocoDoogyInteractState(owner, fsm, waypoints));
            //fsm.ChangeState();
        }
        // 이동 중 멈춤 감지
        if (!agent.isStopped && agent.velocity.sqrMagnitude < 0.01f)
        {
            timeToStuck += Time.deltaTime;
            if (timeToStuck > owner.StuckTime)
            {
                fsm.ChangeState(owner.StuckState);
            }
        }
        else
        {
            timeToStuck = 0f;
        }
    }
    public override void OnStateExit()
    {
        isDragged = false;
        (owner as CocoDoogyBehaviour).SetIsDragged(false);
        timeToStuck = 0f;
        owner.StopAllCoroutines();
        agent.ResetPath();
    }

    public void SetDraggedStartPosition(Vector3 pos)
    {
        draggedPos = pos;
    }

    private IEnumerator LetsGoCoco()
    {
        while (true)
        {
            Transform next = route.GetNextTransform(currentIndex);

            if (route.hasComplete)
            {
                // 루틴 끝
                charAgent.MoveToLastPoint(route.moveWaypoints[0].transform);
                (owner as CocoDoogyBehaviour).SetTimeToGoHome(true);
                while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                {
                    yield return null;
                }

                yield return wait;
                currentIndex = 1;
                init = false;
                Debug.Log($"init = false : {init}");
                Debug.Log($"currentIndex = 1 : {currentIndex}");
                // 로비 매니저에게 나 끝났어요 호출 하면 로비매니저가 SetActive false 처리
                fsm.ChangeState(owner.EditState);
                if (LobbyCharacterManager.Instance) LobbyCharacterManager.RaiseCharacterEvent(owner);
                else if (LobbyCharacterManager_Friend.Instance) LobbyCharacterManager_Friend.RaiseCharacterEvent(owner);

                yield break;
            }

            if (next == null)
            {
                charAgent.MoveToLastPoint(route.moveWaypoints[0].transform);
                while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                {
                    yield return null;
                }

                fsm.ChangeState(owner.IdleState);
                yield break;
            }
            else
            {
                yield return RepeatMove(next);
                currentIndex++;
                yield return null;
            }
        }
    }

    // 각 포인트 기준으로 주위를 반복 이동
    private IEnumerator RepeatMove(Transform point)
    {
        int maxTime = Random.Range(3, 6);
        //Debug.Log($"maxTime : {maxTime}");
        while (true)
        {
            if (repeatTime < maxTime)
            {
                //Debug.Log($"repeatTime : {repeatTime}");
                charAgent.CocoMoveToRandomTransPoint(owner, point);
                while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                {
                    yield return null;
                }
                repeatTime++;
                yield return null;
            }
            else if (repeatTime >= maxTime)
            {
                repeatTime = 0;
                yield break;
            }
        }
    }

}
