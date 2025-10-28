using UnityEngine;

// 사용자가 오브젝트와 상호작용
public interface IInteractable
{
    void OnInteract();
}

public interface IDraggable
{
    void OnDragStart(Vector3 position);
    void OnDrag(Vector3 position);
    void OnDragEnd(Vector3 position);
}

public interface ILongPressable
{
    void OnLongPress();
}
//

// 오브젝트들 서로 상호작용
public interface ILobbyInteracte
{
    void OnLobbyInteract();
    void OnCocoAndMasterInteract(string animStateName);
}
