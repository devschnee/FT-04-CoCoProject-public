using NUnit.Framework;
using System;
using Unity.AI.Navigation;
using UnityEngine;
using System.Collections.Generic;
// Surface저장 : 지금은 여기에 다 때려 넣겠지만 나중에 분할하겠슴다.
[Serializable]
public class  NavMeshSaveData
{
    public List<NavMeshObjectData> nObj = new List<NavMeshObjectData>();
}
[Serializable]
public class NavMeshObjectData
{
    public string prefabName;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}
//

public class InLobbyManager : MonoBehaviour
{
    [SerializeField] TestScriptableObject[] objectDatabase;
    [SerializeField] GameObject plane;
    
    private NavMeshSurface planeSurface;

    public Transform[] waypoints;
    public static InLobbyManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        planeSurface = plane.GetComponent<NavMeshSurface>();

    }

    void Start() // 지금은 스타트이지만 인게임이랑 합체하면 어떻게 Enable로 하나? 로비매니저가 인게임까지 딸려갈 필요는 없을테고, 씬을 로딩하는 거니.. 몰루?
    {
        foreach (var data in objectDatabase)
        {
            GameObject obj = Instantiate(data.prefab);
            var meta = obj.GetComponent<GameObjectData>();
            meta.Initialize(data);
        }
    }

    public void NewMap()
    {
        planeSurface.BuildNavMesh();
    }
}
