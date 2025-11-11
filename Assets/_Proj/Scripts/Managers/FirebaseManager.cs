using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    private FirebaseApp App { get; set; }
    private FirebaseDatabase DB { get; set; }

    private FirebaseAuth Auth { get; set; }
    private DatabaseReference MapDataRef => DB.RootReference.Child($"mapData");
    private DatabaseReference MapMetaRef => DB.RootReference.Child($"mapMeta");

    public StageManager stageManager;

    public bool IsInitialized { get; private set; }
    public MapData currentMapData;
    public string selectStageID;

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
            DB = FirebaseDatabase.GetInstance(App);
            DB.SetPersistenceEnabled(false);

            //추가: 파이어베이스 인증 기능 활용을 위해 현재 App에서 Firebase Authentication 어플리케이션을 가져옵니다.
            Auth = FirebaseAuth.GetAuth(App);
            IsInitialized = true;
            Debug.Log($"[파이어베이스 인증]로컬에 남아있는 유저 아이디 : {Auth.CurrentUser.UserId}");
            
            
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


    //삭제: 맵에디터에서만 사용하는 코드.
    //public async Task<List<string>> FetchMapNamesFromFirebase()
    //{

    //    List<string> allMaps = new();
    //    try
    //    {
    //        var snapshot = await MapMetaRef.Child("maps").GetValueAsync();
    //        if (snapshot.Exists)
    //        {
    //            foreach (var map in snapshot.Children)
    //            {
    //                allMaps.Add(map.Key);
    //            }

    //        }
    //        return allMaps;
    //    }
    //    catch (FirebaseException fe)
    //    {
    //        Debug.LogError(fe.Message);
    //        return null;
    //    }

    //}

    public async Task<MapData> LoadMapFromFirebase(string mapName, Action<string> callback = null)
    {
        //var task = new TaskCompletionSource<MapData>();
        //callback?.Invoke($"Looking for mapdata from DB by {mapName}...");

        //MapDataRef.Child(mapName).GetValueAsync().ContinueWithOnMainThread(task => {

        //    if (task.IsFaulted)
        //    {
        //        var e = task.Exception?.Flatten().InnerException;
        //        {

        //            callback?.Invoke(e?.Message);
        //            Debug.LogError(e?.Message);
        //        }
        //    }
        //    else if (task.IsCompleted)
        //    {

        //        var snapshot = task.Result;
        //        if (snapshot.Exists)
        //        {
        //            callback?.Invoke($"{mapName} data Found!");
        //            MapData data = JsonUtility.FromJson<MapData>(snapshot.GetRawJsonValue());
        //            return data;
        //        }
        //        else
        //        {
        //            throw new Exception("No such map data exists.");
        //        }
        //    }


        //} );
        //return task.Task;
        #region 기존 방법.
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
        #endregion
    }

    public async Task FindMapDataByID(string id)
    {
        currentMapData = await LoadMapFromFirebase(id);
        selectStageID = id;
    }


    //Firebase Auth 관련 기능.
    public async Task SignInAnonymouslyTest(Action<FirebaseUser> onSuccess, Action<FirebaseException> onFailure) => await SignInAnonymously(onSuccess, onFailure);



    /// <summary>
    /// 익명 로그인 기능.
    /// </summary>
    /// <param name="onSuccess">성공 시의 FirebaseUser를 매개변수로 삼아 실행시킬 콜백 함수</param>
    /// <param name="onFailure">실패 시 catch되는 FirebaseException을 매개변수로 삼아 실행시킬 콜백 함수</param>
    /// <returns></returns>
    private async Task SignInAnonymously(Action<FirebaseUser> onSuccess, Action<FirebaseException> onFailure)
    {
        try
        {
            var result = await Auth.SignInAnonymouslyAsync();
            //SaveUserData(result.User);
            onSuccess?.Invoke(result.User);
        }
        catch (FirebaseException ex)
        {
            onFailure?.Invoke(ex);
        }
    }

    [Obsolete("유저데이터 클래스 정의를 먼저 하고 사용해야 함. 정의 및 연결 끝나면 이 Obsolete는 지우고 정식으로 사용 예정.", false)]
    /// <summary>
    /// 유저 데이터 저장 기능.
    /// </summary>
    /// <param name="user">데이터를 저장할 FirebaseUser</param>
    private void SaveUserData(FirebaseUser user)
    {
        if (user == null) return;

        var userRef = DB.GetReference("users").Child(user.UserId);
        var userData = new
        {
            uid = user.UserId,
            email = user.Email,
            isAnonymous = user.IsAnonymous,
            displayName = user.DisplayName,
            lastLogin = DateTime.UtcNow.ToString("o")
        };

        userRef.SetRawJsonValueAsync(JsonUtility.ToJson(userData));
    }

    /// <summary>
    /// 로그인된 익명 유저의 인증 정보를 구글 계정과 연결하는 함수.
    /// </summary>
    /// <param name="idToken"></param>
    /// <param name="accessToken"></param>
    /// <param name="onSuccess"></param>
    /// <param name="onFailure"></param>
    /// <returns></returns>
    public async Task LinkAnonymousToGoogle(string idToken, string accessToken, Action<FirebaseUser> onSuccess, Action<FirebaseException> onFailure)
    {
        //인증의 현재 유저가 익명 유저가 아닐 경우 리턴
        if (!Auth.CurrentUser.IsAnonymous) return;


        var credential = GoogleAuthProvider.GetCredential(idToken, accessToken);
        try
        {
            var result = await Auth.CurrentUser.LinkWithCredentialAsync(credential);
            onSuccess?.Invoke(result.User);
        } catch (FirebaseException fe)
        {
            onFailure?.Invoke(fe);
        }
    }
}
