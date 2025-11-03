using NUnit.Framework;
using System;
using Unity.AI.Navigation;
using UnityEngine;
using System.Collections.Generic;
// Surface���� : ������ ���⿡ �� ���� �ְ����� ���߿� �����ϰڽ���.
// [Serializable]
// public class  NavMeshSaveData
// {
//     public List<NavMeshObjectData> nObj = new List<NavMeshObjectData>();
// }
// [Serializable]
// public class NavMeshObjectData
// {
//     public string prefabName;
//     public Vector3 position;
//     public Quaternion rotation;
//     public Vector3 scale;
// }
// //

public class InLobbyManager : MonoBehaviour
{
    [SerializeField] TestScriptableCharacter[] charDatabase;
    [SerializeField] GameObject plane;
    [SerializeField] EditController editController;
    
    private NavMeshSurface planeSurface;
    public Transform[] cocoWaypoints;

    public bool isEditMode { get; private set; } // 에딧컨트롤러에서 받아오기
    private int originalLayer; // 평상 시 레이어
    private int editableLayer; // 편집모드 시 레이어
    

    public static InLobbyManager Instance { get; private set; }
    private List<ILobbyState> lobbyCharacter = new(); // 맵에 활성화 된 캐릭터들 모음

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        planeSurface = plane.GetComponent<NavMeshSurface>();

        if (editController == null) editController = FindFirstObjectByType<EditController>();
        isEditMode = false;

        originalLayer = LayerMask.NameToLayer("InLobbyObject");
        editableLayer = LayerMask.NameToLayer("Editable");

    }

    private void Start() // 깨끗한 프리팹에 붙여주는 방법이 인게임 아웃게임 전환에서 좋지 않을 깝숑
    {
        planeSurface.BuildNavMesh();
        foreach (var data in charDatabase)
        {
            GameObject gObj;
            gObj = Instantiate(data.prefab, cocoWaypoints[0].position, Quaternion.identity);
            switch (data.type)
            {
                case CharacterType.None:
                    break;
                case CharacterType.CocoDoogy:
                    gObj.AddComponent<CocoDoogyBehaviour>();
                    break;
                case CharacterType.Master:
                    gObj.AddComponent<MasterBehaviour>();
                    break;
                case CharacterType.Decoration:
                    break;
                case CharacterType.Animal:
                    gObj.AddComponent<AnimalBehaviour>();
                    break;
            }
            gObj.tag = data.type.ToString();
            gObj.layer = LayerMask.NameToLayer("InLobbyObject");
            var meta = gObj.GetComponent<GameObjectData>();
            meta.Initialize(data);
        }

        foreach (var lC in lobbyCharacter)
        {
            if (lC == null) Debug.Log($"{lC} null");
            if (lC != null)
            {
                lC.StartScene();
            }
        }
    }

    private void Update()
    {
        bool current = editController.IsEditMode;
        Debug.Log($"current 상태 : {current}");
        if (current != isEditMode)
        {
            isEditMode = current;
            if (isEditMode)
            {
                foreach (var lC in lobbyCharacter)
                {
                    if (lC != null)
                    {
                        lC.InEdit();
                        var mono = lC as BaseLobbyCharacterBehaviour;
                        //gObj.InEdit();
                        mono.gameObject.layer = editableLayer;
                    }
                }
                Debug.Log("편집모드 진입");
            }
            else if (!isEditMode)
            {
                planeSurface.BuildNavMesh();
                foreach (var lC in lobbyCharacter)
                {
                    if (lC != null)
                    {
                        lC.InNormal();
                        var mono = lC as BaseLobbyCharacterBehaviour;
                        mono.gameObject.layer = originalLayer;
                        //lC.InUpdate();
                    }
                }
                Debug.Log("일반보드 진입");
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var lC in lobbyCharacter)
        {
            if (lC != null)
            {
                lC.ExitScene();
            }
        }
    }

    // 등록 및 삭제
    public void RegisterLobbyChar(ILobbyState gObj)
    {
        lobbyCharacter.Add(gObj);
        Debug.Log($"{gObj} 등록됨");
    }
    public void UnregisterLobbyChar(ILobbyState gObj)
    {
        lobbyCharacter.Remove(gObj);
    }
}
