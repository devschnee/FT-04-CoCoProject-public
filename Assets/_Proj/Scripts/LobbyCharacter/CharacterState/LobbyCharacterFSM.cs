using UnityEngine;

public class LobbyCharacterFSM
{
    private LobbyCharacterBaseState currentState;
    public LobbyCharacterBaseState CurrentState => currentState;
    
    public LobbyCharacterFSM(LobbyCharacterBaseState initState)
    {
        currentState = initState;
        ChangeState(currentState);
    }

    public void ChangeState(LobbyCharacterBaseState nextState)
    {
        //if (nextState == currentState) return;

        currentState?.OnStateExit(); // 다음 state 가기 전에 저장 용도로?
        currentState = nextState;
        currentState?.OnStateEnter();
    }

    public void UpdateState()
    {
        currentState?.OnStateUpdate();
    }

}
