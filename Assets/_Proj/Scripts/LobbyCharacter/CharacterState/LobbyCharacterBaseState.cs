using UnityEngine;

public abstract class LobbyCharacterBaseState
{
    protected BaseLobbyCharacterBehaviour owner;
    protected LobbyCharacterFSM fsm;

    protected LobbyCharacterBaseState(BaseLobbyCharacterBehaviour owner, LobbyCharacterFSM fsm)
    {
        this.owner = owner;
        this.fsm = fsm;
    }

    public virtual void OnStateEnter()
    {
        Debug.Log($"{owner.gameObject.name} : {fsm.CurrentState} 진입");
    }
    public abstract void OnStateUpdate();
    public abstract void OnStateExit();
}
