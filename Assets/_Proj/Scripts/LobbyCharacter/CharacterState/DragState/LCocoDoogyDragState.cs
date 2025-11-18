using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

public class LCocoDoogyDragState : LobbyCharacterBaseState, IDragState
{
    private Vector3 originalPos;
    private NavMeshAgent agent;
    private Animator anim;
    private Transform trans;
    private Camera mainCam;
    private int mainPlaneMask;
    private float yValue;
    private bool isDragging;

    public LCocoDoogyDragState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = (owner as CocoDoogyBehaviour).GetComponent<NavMeshAgent>();
        anim = (owner as CocoDoogyBehaviour).GetComponent<Animator>();
        trans = (owner as CocoDoogyBehaviour).GetComponent<Transform>();
        mainCam = (owner as CocoDoogyBehaviour).MainCam;
        mainPlaneMask = LayerMask.GetMask("MainPlane");
        yValue = (owner as CocoDoogyBehaviour).YValue;
    }
    public override void OnStateEnter()
    {
        base.OnStateEnter();
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        if (agent.enabled) agent.enabled = false;
    }
    public override void OnStateUpdate() { }
    public override void OnStateExit()
    {
        isDragging = false;
        agent.enabled = true;
        agent.isStopped = false;
        agent.Warp(trans.position);
    }

    public void OnBeginDrag(Vector3 pos)
    {
        originalPos = trans.position;
        //Debug.Log($"originalPos : {originalPos}, transPos : {trans.position}");
        isDragging = true;
        anim.Play("Idle_A");
        anim.speed = 1f;
        //StopMoving();
        //fsm.ChangeState(StopState());
    }

    public void OnDrag(Vector3 pos)
    {
        if (!isDragging) return;
        //Debug.Log($"isDragging 상태 : {isDragging}");
        Ray ray = mainCam.ScreenPointToRay(pos);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mainPlaneMask))
        {
            //if (!hit.collider.CompareTag("MainPlane")) return;
            Vector3 hitPos = hit.point;
            hitPos.y = yValue;
            //Debug.Log($"현재 hitPos : {hitPos}");
            trans.position = hitPos;
        }
    }

    public void OnEndDrag(Vector3 pos)
    {
        isDragging = false;
        //Debug.Log($"EndDrag, IsDragging : {isDragging}");
        NavMeshHit navHit;
        bool onNavMesh = NavMesh.SamplePosition(trans.position, out navHit, 0.5f, NavMesh.AllAreas);
        if (!onNavMesh)
        {
            trans.position = originalPos;
            Debug.Log($"{owner.gameObject.name} : NavMesh 없음 기존 포지션으로");
        }
        else
        {
            trans.position = navHit.position;
            (owner as CocoDoogyBehaviour).SetLastDragEndPos(navHit.position);
            (owner as CocoDoogyBehaviour).SetIsDragged(true);
            Debug.Log($"{owner.gameObject.name} : NavMesh 있음 해당 포지션으로");
        }
        fsm.ChangeState((owner as CocoDoogyBehaviour).IdleState);
    }
}

