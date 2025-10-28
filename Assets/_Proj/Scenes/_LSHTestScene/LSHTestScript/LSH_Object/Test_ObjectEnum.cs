using UnityEngine;

public enum ObjectType
{
    None,
    CocoDoogy,
    Master,
    Decoration,
    Animal
}

public enum LobbyObjectState
{
    Idle = 0,
    Interact,
    Patrol,
    ReturnHome,
    CocoAndMaster
}
