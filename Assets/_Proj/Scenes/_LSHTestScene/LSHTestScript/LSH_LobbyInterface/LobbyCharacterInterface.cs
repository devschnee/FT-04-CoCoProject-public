using UnityEngine;

// 클릭, 드래그
public interface ILobbyInteractable
{
    void OnLobbyInteract();
}
public interface ILobbyDraggable
{
    void OnLobbyBeginDrag(Vector3 position);
    void OnLobbyDrag(Vector3 position);
    void OnLobbyEndDrag(Vector3 position);
}
public interface ILobbyPressable
{
    void OnLobbyPress();
}

// 코코두기와 아이들 상호작용
public interface ILobbyCharactersEmotion
{
    void OnCocoMasterEmotion();
    void OnCocoAnimalEmotion();
}

// 일반모드, 편집모드 시 전환
public interface ILobbyState
{
    void Register(); // 로비에 소환되면 로비매니저에게 등록 요청

    void Unregister(); // 로비에 삭제되면 로비매니저에게 삭제 요청

    void InNormal();
    void InEdit();
    void InUpdate();
    void StartScene();
    void ExitScene();
}