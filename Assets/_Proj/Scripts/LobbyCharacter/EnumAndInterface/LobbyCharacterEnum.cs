using UnityEngine;

public enum CharacterType
{
    None,
    CocoDoogy,
    Master,
    Decoration,
    Animal
}

public enum LobbyCharacterState
{
    Idle = 0,
    Move,
    Stuck,
    Recovering,
    Dragging,
    Animation,
    CocoOther,
}
