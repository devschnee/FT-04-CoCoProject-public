using UnityEngine;

public interface IInteractable
{
    void OnInteract();
}

public interface IDraggable
{
    void OnDrag(Vector3 position);
}

public interface ILongPressable
{
    void OnLongPress();
}

