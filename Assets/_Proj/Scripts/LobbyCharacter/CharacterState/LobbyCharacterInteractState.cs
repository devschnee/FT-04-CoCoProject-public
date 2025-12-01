using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

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
            var master = LobbyCharacterManager.Instance ? LobbyCharacterManager.Instance.GetMaster() : LobbyCharacterManager_Friend.Instance.GetMaster();
            owner.StartCoroutine(LetsDance(master));
            (owner as CocoDoogyBehaviour).SetCharInteracted(0);

        }
        else if (isCA)
        {
            isCA = false;
            var animal = LobbyCharacterManager.Instance ? LobbyCharacterManager.Instance.GetAnimal() : LobbyCharacterManager_Friend.Instance.GetAnimal(); ;
            owner.StartCoroutine(LetsDance(animal));
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

    private IEnumerator LetsDance(BaseLobbyCharacterBehaviour who)
    {
        Vector3 lookTarget = who.transform.position;
        lookTarget.y = owner.transform.position.y;
        owner.transform.LookAt(lookTarget);

        Vector3 lookAtMe = owner.transform.position;
        lookAtMe.y = who.transform.position.y;
        who.transform.LookAt(lookAtMe);

        yield return new WaitForSeconds(0.8f);

        if (who is MasterBehaviour) charAnim.PlayCocoInterationWithMaster();
        else if (who is AnimalBehaviour) charAnim.PlayCocoInteractionWithAnimal();
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

        charAnim.PlayMasterInterationWithCoco();
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

        charAnim.PlayAnimalInteractionWithCoco();
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
}
