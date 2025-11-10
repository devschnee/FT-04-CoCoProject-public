using UnityEngine;
using UnityEngine.AI;

public class LMasterUniqueState : LobbyCharacterBaseState
{
    private readonly NavMeshAgent agent;

    public LMasterUniqueState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm) : base(owner, fsm)
    {
        agent = owner.GetComponent<NavMeshAgent>();
    }

    public override void OnStateEnter()
    {
        throw new System.NotImplementedException();
    }

    public override void OnStateUpdate()
    {
        throw new System.NotImplementedException();
    }

    public override void OnStateExit()
    {
        throw new System.NotImplementedException();
    }
}
