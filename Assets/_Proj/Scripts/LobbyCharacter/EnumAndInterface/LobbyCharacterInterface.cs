using UnityEngine;

// 클릭, 드래그
public interface ILobbyInteractable
{
    void OnLobbyClick();
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

// 일반모드, 편집모드 시 전환
public interface ILobbyState
{
    void Init(); // 초기화 Awake 비슷한
    void PostInit(); // Init 후 초기화 Start 비슷한
    void LoadInit();
    void FinalInit();
    void Register(); // 로비에 소환되면 로비매니저에게 등록 요청
    void Unregister(); // 로비에 삭제되면 로비매니저에게 삭제 요청
    void InNormal();
    void InEdit();
    void InitWaypoint();
    //void StartScene();
    //void ExitScene();
}

// DragState에 붙일 인터페이스
public interface IDragState
{
    void OnBeginDrag(Vector3 pos);
    void OnDrag(Vector3 pos);
    void OnEndDrag(Vector3 pos);
}