using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FirebaseManager_FORTEST : MonoBehaviour
{
    public static FirebaseManager_FORTEST Instance { get; private set; }
    private FirebaseApp App { get; set; }
    private FirebaseDatabase DB { get; set; }
    private DatabaseReference MapDataRef => DB.RootReference.Child($"mapData");
    private DatabaseReference MapMetaRef => DB.RootReference.Child($"mapMeta");

    public StageManager stageManager;

    public bool IsInitialized { get; private set; }
    public MapData currentMapData;
    async void Start()
    {
        DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
        
        //TODO: 실제 게임용 파이어베이스로 바꾸게 될 경우 아래는 필요하지 않게 됨. 테스트용 파이어베이스 참조를 위한 코드임.
        //var options = new AppOptions()
        //{
        //    ApiKey = "AIzaSyCwkcOr1bVZRgdHsx773b6rO2drpjy1dyY",
        //    DatabaseUrl = new("https://doogymapeditor-default-rtdb.asia-southeast1.firebasedatabase.app/"),
        //    ProjectId = "doogymapeditor",
        //    StorageBucket = "doogymapeditor.firebasestorage.app",
        //    MessageSenderId = "236130748269",
        //    AppId = "1:236130748269:web:34a94137f83bef839dfc64"
        //};

        //App = FirebaseApp.Create(options);

        if (status == DependencyStatus.Available)
        {

            //초기화 성공
            Debug.Log($"파이어베이스 초기화 성공");
            App = FirebaseApp.DefaultInstance;
            DB = FirebaseDatabase.DefaultInstance;
        }
        else
        {
            Debug.LogWarning($"파이어베이스 초기화 실패, 파이어베이스 앱 상태: {status}");
        }
    }


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }



    public async Task<List<string>> FetchMapNamesFromFirebase()
    {

        List<string> allMaps = new();
        try
        {
            var snapshot = await MapMetaRef.Child("maps").GetValueAsync();
            if (snapshot.Exists)
            {
                foreach (var map in snapshot.Children)
                {
                    allMaps.Add(map.Key);
                }

            }
            return allMaps;
        }
        catch (FirebaseException fe)
        {
            Debug.LogError(fe.Message);
            return null;
        }

    }

    public async Task<MapData> LoadMapFromFirebase(string mapName, Action<string> callback = null)
    {
        
        try
        {
            
            callback?.Invoke($"Looking for mapdata from DB by {mapName}...");
            var snapshot = await MapDataRef.Child(mapName).GetValueAsync();
            if (snapshot.Exists)
            {
                callback?.Invoke($"{mapName} data Found!");
                MapData data = JsonUtility.FromJson<MapData>(snapshot.GetRawJsonValue());
                return data;
            }
            else
            {
                throw new Exception("No such map data exists.");
            }

        }
        catch (FirebaseException fe)
        {

            callback?.Invoke(fe.Message);
            Debug.LogError(fe.Message);
            return null;
        }
        catch (Exception ee)
        {
            callback?.Invoke(ee.Message);
            Debug.LogError(ee.Message);
            return null;
        }
    }

    public async Task Temp(string id)
    {
        currentMapData =  await LoadMapFromFirebase(id);
    }
}
