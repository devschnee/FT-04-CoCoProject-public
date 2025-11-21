using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

// 우선 코코두기와 마스터 먼저 상호작용하는 걸 끝내자.
public class LCocoDoogyInteractState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;
    private readonly LobbyCharacterAnim charAnim;
    private bool isCM;
    private bool isCA;

    public LCocoDoogyInteractState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm, LobbyCharacterAnim charAnim) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
        this.charAnim = charAnim;
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
        if (!agent.enabled) agent.enabled = true;
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        if (isCM)
        {
            isCM = false;
            var master = LobbyCharacterManager.Instance.GetMaster();
            owner.transform.LookAt(master.transform.position);
            charAnim.PlayCocoInterationWithMaster(master.transform.position);
            (owner as CocoDoogyBehaviour).SetCharInteracted(0);

        }
        else if (isCA)
        {
            isCA = false;
            (owner as CocoDoogyBehaviour).SetCharInteracted(1);
        }

        //owner.StartCoroutine(WaitAndFinish());
    }
    public override void OnStateUpdate()
    {
        if (owner.IsDestroyed()) owner.StopAllCoroutines();
    }
    public override void OnStateExit()
    {
        owner.StopAllCoroutines();
        (owner as CocoDoogyBehaviour).ResetInteracting();
        agent.isStopped = false;
        isCA = false;
        isCM = false;
    }

    private IEnumerator WaitAndFinish()
    {
        yield return new WaitForSeconds(1f);
        //owner.EndInteract(0);
        fsm.ChangeState(owner.IdleState);
        yield break;
    }
    /// <summary>
    /// i = 0 : CA, i = 1 : CM
    /// </summary>
    /// <param name="i"></param>
    // /// <param name="which"></param>
    public void SetCAM(int i, bool which)
    {
        if (which == true)
        {
            if (i == 0) isCA = true;
            else if (i == 1) isCM = true;
        }
        else if (which == false)
        {
            if (i == 0) isCA = false;
            else if (i == 1) isCM = false;
        }
    }
}

public class LMasterInteractState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;
    private readonly LobbyCharacterAnim charAnim;

    public LMasterInteractState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm, LobbyCharacterAnim charAnim) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
        this.charAnim = charAnim;
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
        if (!agent.enabled) agent.enabled = true;
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;

        var coco = LobbyCharacterManager.Instance.GetCoco();
        owner.transform.LookAt(coco.transform.position);
        charAnim.PlayMasterInterationWithCoco(coco.transform.position);

        //owner.StartCoroutine(WaitAndFinish());
    }
    public override void OnStateUpdate()
    {
        if (owner.IsDestroyed()) owner.StopAllCoroutines();
    }
    public override void OnStateExit()
    {
        owner.StopAllCoroutines();
        agent.isStopped = false;
    }

    private IEnumerator WaitAndFinish()
    {
        yield return new WaitForSeconds(3f);
        //owner.EndInteract(0);
        fsm.ChangeState(owner.IdleState);
        yield break;
    }
}

public class LAnimalInteractState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;
    private readonly LobbyCharacterAnim charAnim;

    public LAnimalInteractState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm, LobbyCharacterAnim charAnim) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
        this.charAnim = charAnim;
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
        if (!agent.enabled) agent.enabled = true;
        if (agent.enabled && !agent.isStopped) agent.isStopped = true;
        
        owner.StartCoroutine(WaitAndFinish());
    }
    public override void OnStateUpdate()
    {
        if (owner.IsDestroyed()) owner.StopAllCoroutines();
    }
    public override void OnStateExit()
    {
        owner.StopAllCoroutines();
        agent.isStopped = false;
    }

    private IEnumerator WaitAndFinish()
    {
        yield return new WaitForSeconds(3f);
        //owner.EndInteract(0);
        fsm.ChangeState(owner.IdleState);
        yield break;
    }
}
