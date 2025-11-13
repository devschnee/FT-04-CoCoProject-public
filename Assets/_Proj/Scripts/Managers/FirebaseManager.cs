using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
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
    private DatabaseReference UserDataRef => DB.RootReference.Child($"userData");
    private DatabaseReference CurrentUserDataRef => UserDataRef.Child(Auth.CurrentUser.UserId);

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
        #region 기존 방법.
        try
        {

            callback?.Invoke($"Looking for mapdata from DB by {mapName}...");
            var snapshot = await MapDataRef.Child(mapName).GetValueAsync();
            if (snapshot.Exists)
            {
                callback?.Invoke($"{mapName} data Found!");
                MapData data = JsonConvert.DeserializeObject<MapData>(snapshot.GetRawJsonValue());

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

    #region Firebase Auth Functions
    //Firebase Auth 관련 기능.

    /// <summary>
    /// 익명 로그인 기능 테스트용 함수.
    /// </summary>
    /// <param name="onSuccess"></param>
    /// <param name="onFailure"></param>
    /// <returns></returns>
    public async Task SignInAnonymouslyTest(Action<FirebaseUser> onSuccess, Action<FirebaseException> onFailure) => await SignInAnonymously(onSuccess, onFailure);



    /// <summary>
    /// 익명 로그인 기능.
    /// </summary>
    /// <param name="onSuccess">성공 시의 FirebaseUser를 매개변수로 삼아 호출할 콜백 함수</param>
    /// <param name="onFailure">실패 발생되는 FirebaseException을 매개변수로 삼아 호출할 콜백 함수</param>
    /// <returns></returns>
    private async Task SignInAnonymously(Action<FirebaseUser> onSuccess, Action<FirebaseException> onFailure)
    {
        try
        {
            var result = await Auth.SignInAnonymouslyAsync();
            //SaveUserData(result.User);
            onSuccess?.Invoke(result.User);

            //이곳에 이 익명유저의 uid를 키로 하는 UserData 자식의 값을 가져오도록 함.
            await FetchCurrentUserData();
            //가져온 값이 존재하면 UserData.Local에 대입, 만약 없다면 UserData.Local을 new()해줌... 맞나?
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

        userRef.SetRawJsonValueAsync(JsonConvert.SerializeObject(userData));
    }

    /// <summary>
    /// 로그인된 익명 유저의 인증 정보를 구글 계정과 연결하는 함수.
    /// </summary>
    /// <param name="idToken"></param>
    /// <param name="accessToken"></param>
    /// <param name="onSuccess"></param>
    /// <param name="onFailure"></param>
    /// <returns></returns>
    private async Task LinkAnonymousToGoogle(string idToken, string accessToken, Action<FirebaseUser> onSuccess, Action<FirebaseException> onFailure)
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

    #endregion


    #region Firebase Realtime DB Functions
    //1. DB에서 UserData를 가져오는 처리.
    public async Task FetchCurrentUserData()
    {
        if (Auth.CurrentUser == null || !Auth.CurrentUser.IsValid()) return;
        try
        {
            DataSnapshot snapshot = await CurrentUserDataRef.GetValueAsync();
            if (snapshot.Exists)
            {
                Debug.Log($"{Auth.CurrentUser.UserId}: 유저데이터 탐색 성공.");
                //UserData snapshotUserData = JsonConvert.DeserializeObject<UserData>(snapshot.GetRawJsonValue());
                UserData snapshotUserData = snapshot.GetRawJsonValue().FromJson<UserData>();
                Debug.Log($"유저데이터 Json->인스턴스 변환 성공.");
                UserData.SetLocal(snapshotUserData);
                Debug.Log($"UserData.Local로 저장 성공.");
            }
            else
            {
                Debug.Log($"{Auth.CurrentUser.UserId}: 유저데이터가 존재하지 않음.");
                UserData newUser = new();
                //string userDataJson = JsonConvert.SerializeObject(newUser);
                string userDataJson = newUser.ToJson();
                await CurrentUserDataRef.SetRawJsonValueAsync(userDataJson);
                Debug.Log($"{Auth.CurrentUser.UserId}: 파이어베이스 DB에 유저데이터 저장함.");
                UserData.SetLocal(newUser);
                Debug.Log($"UserData.Local로 저장 성공.");
            }
        }
        catch (FirebaseException fe)
        {
            Debug.LogError($"{Auth.CurrentUser.UserId}: 유저데이터 가져오는 도중 오류 발생함. {fe.Message}");
        }
    }

    //public async Task<IUserDataCategory> FetchUserData(string uid, IUserDataCategory category)
    //{
    //    try
    //    {

    //    }
    //}

    //2. DB에 로컬 유저데이터를 저장하는 처리 (전체를 JSON으로)
    public async Task UpdateLocalUserData() => await UpdateUserData(Auth.CurrentUser.UserId, UserData.Local);
    
        
    //2-1. DB에 로컬 유저데이터의 한 카테고리만 저장하는 처리
    public async Task UpdateLocalUserDataCategory(IUserDataCategory category) => await UpdateUserDataCategory(Auth.CurrentUser.UserId, category);

    private async Task UpdateUserData(string uid, UserData data)
    {
        //if (Auth.CurrentUser == null || !Auth.CurrentUser.IsValid()) return;
        try
        {
            string userDataJson = JsonConvert.SerializeObject(data);
            await UserDataRef.Child(uid).SetRawJsonValueAsync(userDataJson);
            Debug.Log($"{uid}: {data.master.nickName}의 정보를 DB에 저장.");
        }
        catch (FirebaseException fe)
        {
            Debug.LogError($"{uid}: {data.master.nickName}의 정보 DB에 업로드 도중 오류 발생함. {fe.Message}");
        }
    }
    private async Task UpdateUserDataCategory(string uid, IUserDataCategory category)
    {
        //if (Auth.CurrentUser == null || !Auth.CurrentUser.IsValid()) return;

        
        try
        {
            string userDataJson = category.ToJson();
            await UserDataRef.Child(uid).Child(category.GetType().ToString().ToLower()).SetRawJsonValueAsync(userDataJson);
            Debug.Log($"{uid}: {category.GetType()}카테고리만 뽑아 DB에 저장.");
        }
        catch (FirebaseException fe)
        {
            Debug.LogError($"{uid}: {category.GetType()}카테고리만 뽑아 DB에 업로드 도중 오류 발생함. {fe.Message}");
        }
    }
    #endregion

}
