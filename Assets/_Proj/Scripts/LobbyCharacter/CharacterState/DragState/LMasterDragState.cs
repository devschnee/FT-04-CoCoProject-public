using UnityEngine;
using UnityEngine.AI;

// 마스터는 드래그엔드하면 UniqueState로 넘어감 => 몇 초 대기 후 집으로 가는 로직
public class LMasterDragState : LobbyCharacterBaseState, IDragState
{
    private Vector3 movePos;
    private Vector3 originalPos;
    private NavMeshAgent agent;
    private Animator anim;
    private Transform trans;
    private Camera mainCam;
    private int mainPlaneMask;
    private float yValue;
    private bool isDragging;

    public LMasterDragState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = (owner as MasterBehaviour).GetComponent<NavMeshAgent>();
        anim = (owner as MasterBehaviour).GetComponent<Animator>();
        trans = (owner as MasterBehaviour).GetComponent<Transform>();
        mainCam = (owner as MasterBehaviour).MainCam;
        mainPlaneMask = LayerMask.GetMask("MainPlane");
        yValue = (owner as MasterBehaviour).YValue;
    }
    public override void OnStateEnter()
    {
        base.OnStateEnter();
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        if (agent.enabled) agent.enabled = false;
        anim.Play("Idle_A");
        anim.speed = 1f;
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
        anim.Play("Idle_A");
        Debug.Log($"originalPos : {originalPos}, transPos : {trans.position}");
        isDragging = true;
        
        //StopMoving();
        //fsm.ChangeState(StopState());
    }

    public void OnDrag(Vector3 pos)
    {
        if (!isDragging) return;
        //Debug.Log($"isDragging 상태 : {isDragging}");
        movePos = pos;
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
            fsm.ChangeState(owner.IdleState);
        }
        else
        {
            trans.position = navHit.position;
            Debug.Log($"{owner.gameObject.name} : NavMesh 있음 해당 포지션으로");
            (owner as MasterBehaviour).PlayStun();
        }
    }
}
