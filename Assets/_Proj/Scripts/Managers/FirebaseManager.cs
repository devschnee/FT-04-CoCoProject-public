using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Google;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;


public class FirebaseManager : MonoBehaviour, IQuestBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    private FirebaseApp App { get; set; }
    private FirebaseDatabase DB { get; set; }

    private GoogleSignInConfiguration configuration;

    private string GoogleAPI = "236130748269-ulqjqtfe33lt6mp346qc3ggm20tk3ocp.apps.googleusercontent.com";
    private bool isGoogleReady;
    private FirebaseAuth Auth { get; set; }
    public bool IsSignedIn => Auth.CurrentUser != null;
    public bool IsGuest => Auth.CurrentUser != null && Auth.CurrentUser.IsAnonymous;
    
    private DatabaseReference MapDataRef => DB.RootReference.Child($"mapData");
    private DatabaseReference MapMetaRef => DB.RootReference.Child($"mapMeta");
    private DatabaseReference UserDataRef => DB.RootReference.Child($"userData");
    private DatabaseReference CurrentUserDataRef => UserDataRef.Child(Auth.CurrentUser.UserId);

    public Action<string> onLog;
    public StageManager stageManager;

    public bool IsInitialized { get; private set; }
    public MapData currentMapData;
    public string selectStageID;

    //public async Task<List<(string uid, string nickName)>> GetAllUserNames()
    //{
    //    try
    //    {
    //            List<(string uid, string nickName)> allNameList = new();
    //        var snapshot = await AllNamesRef.GetValueAsync();
    //        if (snapshot.Exists)
    //        {
    //            List<(string nickName, string uid)> allNameFromFirebase = JsonConvert.DeserializeObject<List<(string nickName, string uid)>>(snapshot.GetRawJsonValue());
    //            allNameFromFirebase.ForEach((x) => allNameList.Add((x.uid,x.nickName)));
    //        }
    //            return allNameList;
    //    } catch (FirebaseException fe)
    //    {
    //        Debug.LogError(fe.Message);
    //        return null;
    //    }
    //}

   


    [Range(1f,10f)]
    public float heartbeatInterval = 5f;

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
            Debug.Log($"IsInitialized == {IsInitialized}");

            //if (Auth.CurrentUser == null) await SignInAnonymously((x)=>Debug.Log("자동으로 익명로그인"));

            Debug.Log($"FirebaseManager.Auth.CurrentUser == null? {Auth.CurrentUser == null}");
            if (Auth.CurrentUser != null)
            {
                
                if (Auth.CurrentUser.IsAnonymous)
                    await SignInAnonymously();
                else await GoogleLogin();

                //확인 결과 Auth.CurrentUser는 마지막 로그인 방법 그대로 남아있고, 추가로 로그인해줄 필요가 없었음.

            }

            
            Debug.Log($"[파이어베이스 인증]로컬에 남아있는 유저 아이디 : {Auth.CurrentUser?.UserId}");
            Debug.Log($"[파이어베이스 인증]현재 유저의 표기 이름 : {Auth.CurrentUser?.DisplayName}");
            //GetAllUserNames = await GetAllUserNamesAsync();
            //StartCoroutine(TestLog());

        }
        else
        {

            Debug.LogWarning($"파이어베이스 초기화 실패, 파이어베이스 앱 상태: {status}");
        }
    }
    //IEnumerator TestLog()
    //{
    //    while (true)
    //    {
    //        yield return null;
    //        onLog?.Invoke(Auth.CurrentUser.ProviderId);
    //    }
    //}

    

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

    public async Task<MapData> LoadMapFromFirebaseByMapID(string mapName, Action<string> callback = null)
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id">스테이지 ID가 들어오게 해야 함.</param>
    /// <returns></returns>
    public async Task FindMapDataByStageID(string id)
    {
        string mapName = DataManager.Instance.Stage.GetData(id).map_id;
        selectStageID = id;
        currentMapData = await LoadMapFromFirebaseByMapID(mapName);
    }

    #region Firebase Auth Functions
    //Firebase Auth 관련 기능.

    /// <summary>
    /// 익명 로그인 기능 함수(public).
    /// </summary>
    /// <param name="onSuccess"></param>
    /// <param name="onFailure"></param>
    /// <returns></returns>
    public async Task SignInAnonymously(Action<FirebaseUser> onSuccess = null, Action<FirebaseException> onFailure = null) => await SignInAnonymouslyInternal(onSuccess, onFailure);

    //public void aa()
    //{
    //    Firebase.Auth.Credential credential =
    //    Firebase.Auth.GoogleAuthProvider.GetCredential(GoogleAPI, null);
    //    Auth.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWith(task =>
    //    {
    //        if (task.IsCanceled)
    //        {
    //            Debug.LogError("SignInAndRetrieveDataWithCredentialAsync was canceled.");
    //            return;
    //        }
    //        if (task.IsFaulted)
    //        {
    //            Debug.LogError("SignInAndRetrieveDataWithCredentialAsync encountered an error: " + task.Exception);
    //            return;
    //        }

    //        Firebase.Auth.AuthResult result = task.Result;
    //        Debug.LogFormat("User signed in successfully: {0} ({1})",
    //            result.User.DisplayName, result.User.UserId);
    //    });
    //}

    /// <summary>
    /// 익명 로그인 기능.
    /// </summary>
    /// <param name="onSuccess">성공 시의 FirebaseUser를 매개변수로 삼아 호출할 콜백 함수</param>
    /// <param name="onFailure">실패 발생되는 FirebaseException을 매개변수로 삼아 호출할 콜백 함수</param>
    /// <returns></returns>
    private async Task SignInAnonymouslyInternal(Action<FirebaseUser> onSuccess, Action<FirebaseException> onFailure)
    {
        try
        {
            var result = await Auth.SignInAnonymouslyAsync();
            //SaveUserData(result.User);
            onSuccess?.Invoke(result.User);

            //이곳에 이 익명유저의 uid를 키로 하는 UserData 자식의 값을 가져오도록 함.
            await FetchCurrentUserData();
            //가져온 값이 존재하면 UserData.Local에 대입, 만약 없다면 UserData.Local을 new()해줌... 맞나?
            //로그인이므로 로그인용 하트비트 전송
            await SendHeartbeatAsync(true);
        }
        catch (FirebaseException ex)
        {
            onFailure?.Invoke(ex);
        }
    }

    public async Task<bool> GoogleLogin(Action succeedCallback = null, Action failCallback = null)
    {
        try
        {
            if (!isGoogleReady)
            {
                GoogleSignIn.Configuration = new GoogleSignInConfiguration
                {
                    RequestIdToken = true,
                    WebClientId = GoogleAPI,
                    RequestEmail = true
                };

                isGoogleReady = true;
            }
            //GoogleSignIn.Configuration = new GoogleSignInConfiguration
            //{
            //    RequestIdToken = true,
            //    WebClientId = GoogleAPI
            //};
            //GoogleSignIn.Configuration.RequestEmail = true;

            GoogleSignInUser signIn = await GoogleSignIn.DefaultInstance.SignIn();

            if (signIn == null)
            {
                Debug.Log($"구글 로그인 취소.");
                failCallback?.Invoke();
                return false;
            }
            //TaskCompletionSource<FirebaseUser> signInCompleted = new TaskCompletionSource<FirebaseUser>();

            //구글로 로그인한 유저가 잡힌 상황.

            Credential credential = Firebase.Auth.GoogleAuthProvider.GetCredential(signIn.IdToken, null);
            if (credential == null)
            {
                Debug.Log($"Credential을 받지 못함.");
                failCallback?.Invoke();
                return false;
            }
            else
            {

                FirebaseUser user = await Auth.SignInWithCredentialAsync(credential);
                await FetchCurrentUserData();
                Debug.Log($"파이어베이스 구글 로그인 완료.");
                QuestManager.Instance.Handle(this, "GoogleLogin");
                succeedCallback?.Invoke();
                return true;
                            //user = Auth.CurrentUser;
                            //Username.text = user.DisplayName;
                            //UserEmail.text = user.Email;

                //StartCoroutine(LoadImage(CheckImageUrl(user.PhotoUrl.ToString())));
                        }

        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            failCallback?.Invoke();
            return false;
        }
    }

    //public async Task fdasf()
    //{
    //    try
    //    {

    //        Firebase.Auth.Credential credential =
    //        Firebase.Auth.GoogleAuthProvider.GetCredential(GoogleAuthProvider.ProviderId, googleAccessToken);
    //        await Auth.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWith(task => {
    //            if (task.IsCanceled)
    //            {
    //                Debug.LogError("SignInAndRetrieveDataWithCredentialAsync was canceled.");
    //                return;
    //            }
    //            if (task.IsFaulted)
    //            {
    //                Debug.LogError("SignInAndRetrieveDataWithCredentialAsync encountered an error: " + task.Exception);
    //                return;
    //            }

    //            Firebase.Auth.AuthResult result = task.Result;
    //            Debug.Log($"User signed in successfully: {result.User.DisplayName} ({result.User.UserId})");
    //        });
    //    } 
    //    catch (FirebaseException fe)
    //    {
    //        Debug.Log(fe.Message);
    //    }
    //}

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
        }
        catch (FirebaseException fe)
        {
            onFailure?.Invoke(fe);
        }
    }

    #endregion


    #region Firebase Realtime DB Functions
    //1. DB에서 UserData를 가져오는 처리.
    public async Task FetchCurrentUserData()
    {
        if (Auth.CurrentUser == null /*|| !Auth.CurrentUser.IsValid()*/) return;
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

                //추가: null병합-플레이 중 로그인을 할 수도 있으니 지금까지의 플레이 내용을 보존하면서 구글 연동이 되도록 함.
                UserData newUser = UserData.Local != null ? UserData.Local : new();
                //string userDataJson = JsonConvert.SerializeObject(newUser);
                UserData.SetLocal(newUser);
                string userDataJson = newUser.ToJson();
                await CurrentUserDataRef.SetRawJsonValueAsync(userDataJson);
                Debug.Log($"{Auth.CurrentUser.UserId}: 파이어베이스 DB에 유저데이터 저장함.");
                Debug.Log($"UserData.Local로 저장 성공.");
            }
                StartCoroutine(HeartbeatCoroutine());
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
    /// <summary>
    /// 로컬의 유저데이터 전체를 파이어베이스에 업로드합니다.
    /// </summary>
    /// <returns></returns>
    public async Task UpdateLocalUserData() => await UpdateUserData(Auth.CurrentUser.UserId, UserData.Local);


    //2-1. DB에 로컬 유저데이터의 한 카테고리만 저장하는 처리
    /// <summary>
    /// 로컬의 유저데이터 중 한 카테고리만 뽑아서 파이어베이스에 업로드합니다.
    /// </summary>
    /// <param name="category">데이터 카테고리 객체(예: UserData.Local.inventory)</param>
    /// <returns></returns>
    public async Task UploadLocalUserDataCategory(IUserDataCategory category) => await UploadUserDataCategory(Auth.CurrentUser.UserId, category);

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

    public async Task<UserData> DownloadUserData(string uid)
    {
        try
        {
            var data = await UserDataRef.Child(uid).GetValueAsync();
            if (data.Exists)
            {
                return
                    data.GetRawJsonValue().FromJson<UserData>();
                    


            }
            else
            {
                return
                    new UserData();
            }
        }
        catch (FirebaseException fe)
        {
            Debug.LogError(fe.Message);
            return new UserData();
        }
    }

    public async Task<IUserDataCategory> DownloadUserDataCategory(string uid, UserDataDirtyFlag flag)
    {
        string nodeString = "";
        switch (flag)
        {

            case UserDataDirtyFlag.Master:
                nodeString = "master";
                break;
            case UserDataDirtyFlag.Goods:
                nodeString = "goods";
                break;
            case UserDataDirtyFlag.Inventory:
                nodeString = "inventory";
                break;
            case UserDataDirtyFlag.Lobby:
                nodeString = "lobby";
                break;
            case UserDataDirtyFlag.EventArchive:
                nodeString = "eventArchive";
                break;
            case UserDataDirtyFlag.Friends:
                nodeString = "friends";
                break;
            case UserDataDirtyFlag.Codex:
                nodeString = "codex";
                break;
            case UserDataDirtyFlag.Quest:
                nodeString = "quest";
                break;
            case UserDataDirtyFlag.Likes:
                nodeString = "likes";
                break;
            default:
                break;
        }

        if (nodeString.IsNullOrEmpty())
        {
            return null;
        }

        try
        {
            var category = await UserDataRef.Child(uid).Child(nodeString).GetValueAsync();
            if (category.Exists)
            {
                return
                    nodeString == "master" ? category.GetRawJsonValue().FromJson<UserData.Master>() :
                    nodeString == "goods" ? category.GetRawJsonValue().FromJson<UserData.Goods>() :
                    nodeString == "inventory" ? category.GetRawJsonValue().FromJson<UserData.Inventory>() :
                    nodeString == "lobby" ? category.GetRawJsonValue().FromJson<UserData.Lobby>() :
                    nodeString == "eventArchive" ? category.GetRawJsonValue().FromJson<UserData.EventArchive>() :
                    nodeString == "friends" ? category.GetRawJsonValue().FromJson<UserData.Friends>() :
                    nodeString == "codex" ? category.GetRawJsonValue().FromJson<UserData.Codex>() :
                    nodeString == "quest" ? category.GetRawJsonValue().FromJson<UserData.Quest>() :
                    nodeString == "likes" ? category.GetRawJsonValue().FromJson<UserData.Likes>() :
                    null;

               
            }
            else
            {
                return
                    nodeString == "master" ? new UserData.Master() :
                    nodeString == "goods" ? new UserData.Goods() :
                    nodeString == "inventory" ? new UserData.Inventory() :
                    nodeString == "lobby" ? new UserData.Lobby() :
                    nodeString == "eventArchive" ? new UserData.EventArchive() :
                    nodeString == "friends" ? new UserData.Friends() :
                    nodeString == "codex" ? new UserData.Codex() :
                    nodeString == "quest" ? new UserData.Quest() :
                    nodeString == "likes" ? new UserData.Likes() :
                    null;
            }
        }
        catch (FirebaseException fe)
        {
            Debug.LogError(fe.Message);
            return null;
        }
    }



    private async Task CleanLocalDirtyFlag()
    {
        UserDataDirtyFlag[] flagEnums = (UserDataDirtyFlag[])Enum.GetValues(typeof(UserDataDirtyFlag));
        foreach (var e in flagEnums)
        {
            if (e == UserDataDirtyFlag.All) continue;

            if (UserData.Local.flag.HasFlag(e))
            {
                IUserDataCategory category = await DownloadUserDataCategory(Auth.CurrentUser.UserId, e);
                UserData.Local.SetCategory(category);
            }


        }
        var flag = JsonConvert.SerializeObject(UserData.Local.flag);
        _ = CurrentUserDataRef.Child("flag").SetRawJsonValueAsync(flag);
    }

    public async Task<List<(string uid, string nickName)>> GetAllUserNamesAsync()
    {
        try
        {
            var allData = await AllNamesRef.GetValueAsync();
            if (allData.Exists)
            {
                List<(string uid, string nickName)> allDataList = new(); 
                
                var allNamesDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(allData.GetRawJsonValue());
                foreach (var name in allNamesDict)
                {
                    allDataList.Add((name.Value,name.Key));

                    Debug.Log($"[FirebaseManager]\n{name.Value}:{name.Key}");
                }
                allDataList.Remove(("DONOTREMOVE", "COCODOOGY"));
                allDataList.Remove(("DONOTREMOVE", "DEFAULT"));

                foreach(var data in allDataList)
                {
                    Debug.LogWarning($"[AllNames]\n{data.uid}:{data.nickName}");
                }
                if (Auth.CurrentUser != null)
                {
                    var me = allDataList.Find(x => (x == (Auth.CurrentUser.UserId, Auth.CurrentUser.DisplayName)));
                    allDataList.Remove(me);
                }
                return allDataList;
            }
            else return null;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            return null;
        }
    }

    private async Task UploadUserDataCategory(string uid, IUserDataCategory category)
    {
        if (Auth.CurrentUser == null || !Auth.CurrentUser.IsValid()) return;
        string categoryName = category is UserData.Master ? "master" :
                              category is UserData.Inventory ? "inventory" :
                              category is UserData.Goods ? "goods" :
                              category is UserData.Lobby ? "lobby" :
                              category is UserData.EventArchive ? "eventArchive" :
                              category is UserData.Friends ? "friends" :
                              category is UserData.Codex ? "codex" :
                              category is UserData.Progress ? "progress" :
                              category is UserData.Preferences ? "preferences" :
                              category is UserData.Quest ? "quest" :
                              category is UserData.Likes ? "likes" :
                              "UndefinedNode";

        try
        {
            string userDataJson = category.ToJson();
            await UserDataRef.Child(uid).Child(categoryName).SetRawJsonValueAsync(userDataJson);
            Debug.Log($"{uid}: {categoryName}카테고리만 뽑아 DB에 저장.");
        }
        catch (FirebaseException fe)
        {
            Debug.LogError($"{uid}: {categoryName}카테고리만 뽑아 DB에 업로드 도중 오류 발생함. {fe.Message}");
        }
    }
    #endregion

    /// <summary>
    /// 일정 시간마다 서버로 하트비트를 날립니다. 로그아웃시 자동으로 멈추며, 로그인할 때 시작됩니다.
    /// </summary>
    /// <returns></returns>
    IEnumerator HeartbeatCoroutine()
    {
        WaitUntil localUserDataExists = new WaitUntil(() => UserData.Local != null);
        while (true)
        {
            yield return localUserDataExists;
            if (UserData.Local == null)
                yield break;
            _ = SendHeartbeatAsync();

            if (heartbeatInterval < 1f) heartbeatInterval = 5f;


            yield return new WaitForSeconds(heartbeatInterval);

        }
    }

    private async Task SendHeartbeatAsync(bool isLogin = false)
    {
        if (Auth.CurrentUser == null || !Auth.CurrentUser.IsValid() || UserData.Local == null) return;
        long now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

        await ResolveEnergyRecovery(now);
        

        if (isLogin)
        {
            UserData.Local.master.lastLoginAt = now;
            QuestManager.Instance.Handle(this, "Login");
        }
        UserData.Local.master.lastActiveTime = now;
        try
        {
            if (isLogin) await CurrentUserDataRef.Child("master").Child("lastLoginAt").SetValueAsync(now);
            await CurrentUserDataRef.Child("master").Child("lastActiveTime").SetValueAsync(now);
            //서버에 마지막 활동 시간 기록.


            //기록하고 나서 더티플래그를 확인함.
            DataSnapshot snapshot = await CurrentUserDataRef.Child("flag").GetValueAsync();
            if (snapshot.Exists)
            {
                UserDataDirtyFlag flag = JsonConvert.DeserializeObject<UserDataDirtyFlag>(snapshot.GetRawJsonValue());
                Debug.Log($"{Auth.CurrentUser.UserId}: DB에 하트비트 기록하며 확인한 더티플래그: {flag}");
                
                //TODO: flag의 getter, setter 설정으로 플래그를 설정하는 시점에 필요한 처리가 자동으로 일어날 수 있도록 구성하자.
                //안됨. UserData.Local만 다룰 것이 아니고 (특히 친구 기능에서) 친구의 UserData를 임시로 만들어서 핸들링할 때 문제가 될 것임.
                UserData.Local.flag = flag;
                if (flag != 0) await CleanLocalDirtyFlag();
            }
            
        }
        catch (FirebaseException fe)
        {
            Debug.LogError($"{Auth.CurrentUser.UserId}: 하트비트 보내는 중 오류 발생: {fe.Message}");
        }
    }
    async Task ResolveEnergyRecovery(long now)
    {
        long lastActiveTime = UserData.Local.master.lastActiveTime;
        long lastEnergyAcquiredTime = UserData.Local.master.lastEnergyTime;

        //로컬 유저데이터에 기록된 '에너지'가 5 이상일 경우 아래는 호출하지 않음.
        if (UserData.Local.goods[GoodsType.energy] >= 5)
        {
            lastEnergyAcquiredTime = now;
            UserData.Local.master.lastEnergyTime = lastEnergyAcquiredTime;
            await UploadLocalUserDataCategory(UserData.Local.master);
            return;
        }

        //접속 종료로부터 30분이 지났다면 에너지 회복, 아닐시 해당 시간만큼 회복시간 차감
        //마지막 활동 시간 => 사실상 로그아웃한 시간.

        //마지막으로 에너지 받은 시간보다 현재 시간이 1800 이상 더 클 경우'마다'

        while (lastEnergyAcquiredTime < now - 1800 && UserData.Local.goods[GoodsType.energy] < 5)
        {
            UserData.Local.goods[GoodsType.energy]++;
            lastEnergyAcquiredTime += 1800;
            UserData.Local.master.lastEnergyTime = lastEnergyAcquiredTime;
        } //<=이건 왜냐면, 5보다 크거나 같아진 상황에서 계속 '현재 시간'을 기록해야 딱 하나 쓰고 바로 타이머가 30분부터 돌아가게 되기 때문에.

        
        await UploadLocalUserDataCategory(UserData.Local.goods);
        await UploadLocalUserDataCategory(UserData.Local.master);
    }
    public void SignOut() 
    {
        if (!IsGuest)
            GoogleSignIn.DefaultInstance.SignOut();
            Auth.SignOut();


        UserData.Clear();
    }




    #region 친구 기능 관련 API

    public async Task Friend_OutboundDeleteAsync(string receiverUid)
    {
        if (receiverUid.IsNullOrEmpty()) return;

        if (!UserData.Local.friends.friendList.ContainsKey(receiverUid)) return;
        try
        {
            DataSnapshot receiverDataSnapshot = await UserDataRef.Child(receiverUid).GetValueAsync();
            if (receiverDataSnapshot.Exists)
            {
                //친구 데이터를 받아와서...
                UserData friendData = receiverDataSnapshot.GetRawJsonValue().FromJson<UserData>();
                //해당 데이터의 더티플래그 중 Friends를 켜주고(인바운드 할 수 있도록)
                friendData.flag |= UserDataDirtyFlag.Friends;
                friendData.friends ??= new();
                friendData.friends.friendList ??= friendData.friends.friendList = new();
                if (friendData.friends.friendList.ContainsKey(Auth.CurrentUser.UserId))
                    friendData.friends.friendList.Remove(Auth.CurrentUser.UserId);
                

                //Task를 await하지 않는 이유: 작업을 대기하는 사이에 잘못된 처리가 될 가능성이 높음.
                string friendFlagJson = JsonConvert.SerializeObject(friendData.flag);
                _ = UserDataRef.Child(receiverUid).Child("flag").SetRawJsonValueAsync(friendFlagJson);
                _ = UploadUserDataCategory(receiverUid, friendData.friends);
            }
        }
        catch (FirebaseException fe)
        {
            throw fe;
        }

        UserData.Local.friends.friendList.Remove(receiverUid);
        UserData.Local.friends.onFriendsUpdate?.Invoke();
        await UploadLocalUserDataCategory(UserData.Local.friends);
    }

    public async Task Friend_OutboundAsync(string receiverUid, UserData.Friends.FriendInfo.FriendState receiverState)
    {
        if (receiverUid.IsNullOrEmpty()) return;

        //처음으로 친구 요청을 보낸 상황.
        if (receiverState == UserData.Friends.FriendInfo.FriendState.RequestReceived)
        {
            try
            {
                //아웃바운드 대상을 먼저 핸들링.
                DataSnapshot receiverDataSnapshot = await UserDataRef.Child(receiverUid).GetValueAsync();
                if (receiverDataSnapshot.Exists)
                {
                    //친구 데이터를 받아와서...
                    UserData friendData = receiverDataSnapshot.GetRawJsonValue().FromJson<UserData>();
                    //해당 데이터의 더티플래그 중 Friends를 켜주고(인바운드 할 수 있도록)
                    friendData.flag |= UserDataDirtyFlag.Friends;
                    friendData.friends ??= new();
                    friendData.friends.friendList ??= friendData.friends.friendList = new();
                    friendData.friends.friendList.TryAdd(Auth.CurrentUser.UserId, new()
                    {
                        requestTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        state = receiverState
                    });

                    //Task를 await하지 않는 이유: 작업을 대기하는 사이에 잘못된 처리가 될 가능성이 높음.
                    string friendFlagJson = JsonConvert.SerializeObject(friendData.flag);
                    _ = UserDataRef.Child(receiverUid).Child("flag").SetRawJsonValueAsync(friendFlagJson);
                    _ = UploadUserDataCategory(receiverUid, friendData.friends);
                }

            }
            catch (FirebaseException fe)
            {
                throw fe;
            }
            if (UserData.Local.friends == null) UserData.Local.friends = new();
            if (UserData.Local.friends.friendList == null) UserData.Local.friends.friendList = new();
            if (!UserData.Local.friends.friendList.TryAdd
                (receiverUid, new() { requestTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    state = UserData.Friends.FriendInfo.FriendState.RequestSent}))
            {
                UserData.Local.friends.friendList[receiverUid].state = UserData.Friends.FriendInfo.FriendState.RequestSent;
            }
            UserData.Local.friends.onFriendsUpdate?.Invoke();
            await UploadLocalUserDataCategory(UserData.Local.friends);

            //UserData.Friends.FriendInfo myInfo = new() { requestTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), state = UserData.Friends.FriendInfo.FriendState.RequestReceived};
            //string myInfoJson = JsonConvert.SerializeObject(myInfo);

            //await UserDataRef.Child(receiverUid).Child("friends").Child("friendList").Child(Auth.CurrentUser.UserId).SetRawJsonValueAsync(myInfoJson);
        }

        //친구 요청을 수락한 상황
        if (receiverState == UserData.Friends.FriendInfo.FriendState.Friend)
        {
            try
            {
                //아웃바운드 대상을 먼저 핸들링.
                DataSnapshot receiverDataSnapshot = await UserDataRef.Child(receiverUid).GetValueAsync();
                if (receiverDataSnapshot.Exists)
                {
                    //친구 데이터를 받아와서...
                    UserData friendData = receiverDataSnapshot.GetRawJsonValue().FromJson<UserData>();
                    //해당 데이터의 더티플래그 중 Friends를 켜주고(인바운드 할 수 있도록)
                    friendData.flag |= UserDataDirtyFlag.Friends;
                    friendData.friends ??= new();
                    friendData.friends.friendList ??= friendData.friends.friendList = new();
                    if (!friendData.friends.friendList.TryAdd(Auth.CurrentUser.UserId, new()
                    {
                        requestTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        state = receiverState
                    }))
                    {
                        friendData.friends.friendList[Auth.CurrentUser.UserId].state = receiverState;
                    }
                    if (UserData.Local.friends == null) UserData.Local.friends = new();
                    if (UserData.Local.friends.friendList == null) UserData.Local.friends.friendList = new();
                    if (!UserData.Local.friends.friendList.TryAdd
                        (receiverUid, new()
                        {
                            requestTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            state = receiverState
                        }))
                    {
                        UserData.Local.friends.friendList[receiverUid].state = receiverState;
                    }
                    UserData.Local.friends.onFriendsUpdate?.Invoke();
                    await UploadLocalUserDataCategory(UserData.Local.friends);

                    //Task를 await하지 않는 이유: 작업을 대기하는 사이에 잘못된 처리가 될 가능성이 높음.
                    string flagString = JsonConvert.SerializeObject(friendData.flag);
                    _ = UserDataRef.Child(receiverUid).Child("flag").SetRawJsonValueAsync(flagString);
                    _ = UploadUserDataCategory(receiverUid, friendData.friends);
                }

            }
            catch (FirebaseException fe)
            {
                throw fe;
            }
            //if (UserData.Local.friends == null) UserData.Local.friends = new();
            //if (UserData.Local.friends.friendList == null) UserData.Local.friends.friendList = new();
            //if (!UserData.Local.friends.friendList.TryAdd
            //    (receiverUid, new()
            //    {
            //        requestTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            //        state = UserData.Friends.FriendInfo.FriendState.RequestSent
            //    }))
            //{
            //    UserData.Local.friends.friendList[receiverUid].state = receiverState;
            //}
            //    await UploadLocalUserDataCategory(UserData.Local.friends);

            //UserData.Friends.FriendInfo myInfo = new() { requestTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), state = UserData.Friends.FriendInfo.FriendState.RequestReceived };
            //string myInfoJson = JsonConvert.SerializeObject(myInfo);

            //await UserDataRef.Child(receiverUid).Child("friends").Child("friendList").Child(Auth.CurrentUser.UserId).SetRawJsonValueAsync(myInfoJson);
        }
    }

    #endregion



    #region 닉네임 중복 방지 기능 관련

    //모든 닉네임을 저장할 레퍼런스: 닉네임 중복 방지를 위해 필요함.
    private DatabaseReference AllNamesRef => DB.RootReference.Child($"occupiedNames");

    public async Task<bool> CheckIfNameReservedAndReset(string newName, Action<string> displayMessage = null)
    {
        //가장 먼저 엣지케이스 처리: 쓸 수 없는 닉네임은 쳐내기
        //TODO: 나중에 정규식 넣어서 필터링할수도 있겠음
        if (newName == "DEFAULT" || newName.IsNullOrEmpty())
        {
            displayMessage?.Invoke("<color=red>사용 불가능한 닉네임입니다.</color>");

            return false;

        }

        //로그인된 상황임!!!

        //HACK: 강욱 - 1006: 닉네임 변경 기능으로도 활용할 것이기 때문에 예외처리 추가(바꿀 이름과 원래 이름이 같은 경우)
        string oldName = UserData.Local.master.nickName;
        if (oldName == newName)
        {
            displayMessage?.Invoke("<color=red>같은 이름으로는 변경할 수 없어요!</color>");
            return false;
        }
        try
        {
            displayMessage?.Invoke($"새 닉네임 {newName} 중복 검사 중...");
            //DB의 '이미 존재하는 닉네임' 노드에 새 닉네임이 있는지 검사하는 트랜잭션 수행.
            //TransactionResult transactionResult = null;
            
            var result = await AllNamesRef.Child(newName).RunTransaction(mutableData =>
            {
                
                if (mutableData.Value != null)
                {
                    //이미 mutableData가 존재: 중복 닉으로 존재한다는 뜻.
                    displayMessage?.Invoke($"<color=red>이미 사용 중인 닉네임이에요!</color>");
                    return TransactionResult.Abort();
                }

                //'닉네임'이라는 키의 '값'을 현재 유저의 uid로
                mutableData.Value = Auth.CurrentUser.UserId;

                //


                //커밋 요청 부분
                return TransactionResult.Success(mutableData);
            });

            //result.Exists == false면, 중복 닉이란 뜻임.
            if (result == null || !result.Exists)
            {
                displayMessage?.Invoke("<color=red>이미 사용 중인 닉네임이에요!</color>");
                return false;
            }
        }
        catch (Exception e)
        {
            displayMessage?.Invoke("<color=red>이미 사용 중인 닉네임이에요!</color>");
            Debug.LogError(e);
            return false;
        }

        //여기까지 내려왔으면 본격적으로 변경 시작
        displayMessage?.Invoke("닉네임 변경 작업 진행 중...");

        //0. 전체 닉네임 풀에서 원래 이름 삭제 시도

        //이 분기 필요함: 만약에 oldName이 null이거나 empty면 occupiedNames를 다 날려버리게 될 수도 있음.
        if (!oldName.IsNullOrEmpty())
        {
            try
            {
                displayMessage?.Invoke("기존 닉네임 삭제 처리 중...");
                await AllNamesRef.Child(oldName).RemoveValueAsync();
            }
            catch (FirebaseException fe)
            {
                displayMessage?.Invoke("<color=red>DB 업데이트 실패!</color>\n닉네임 등록을 취소합니다...");
                //실패했으니 닉네임 풀 롤백
                await AllNamesRef.Child(newName).RemoveValueAsync();
                await AllNamesRef.Child(oldName.IsNullOrEmpty() ? "DONOTREMOVE" : oldName).SetValueAsync(Auth.CurrentUser.UserId);
                Debug.LogError($"파이어베이스 에러: {fe.Message}");
                return false;
            }
        }

        //1. DB의 이 유저의 노드에 있는 닉네임 변경 시도
        try
        {
            displayMessage?.Invoke("DB에 새 닉네임 적용 중...");
            await CurrentUserDataRef.Child("master").Child("nickName").SetValueAsync(newName);
        }
        catch (FirebaseException fe)
        {
            displayMessage?.Invoke("<color=red>DB 업데이트 실패!</color>\n닉네임 등록을 취소합니다...");
            //실패했으니 닉네임 풀 롤백
            await AllNamesRef.Child(newName).RemoveValueAsync();
            await AllNamesRef.Child(oldName.IsNullOrEmpty() ? "DONOTREMOVE" : oldName).SetValueAsync(Auth.CurrentUser.UserId);
            Debug.LogError($"파이어베이스 에러: {fe.Message}");
            return false;
        }

        //2. 인증 정보의 DisplayName 변경 시도

        try
        {
            displayMessage?.Invoke("인증 정보에 새 닉네임 적용 중...");
            await Auth.CurrentUser.UpdateUserProfileAsync(new UserProfile { DisplayName = newName });

        }
        catch (FirebaseException fe)
        {
            displayMessage?.Invoke("<color=red>인증 정보 업데이트 실패!</color>\n닉네임 등록을 취소합니다...");
            //실패했으니 DB 롤백
            await CurrentUserDataRef.Child("master").Child("nickName").SetValueAsync(oldName);
            //실패했으니 닉네임 풀 롤백
            await AllNamesRef.Child(newName).RemoveValueAsync();
            await AllNamesRef.Child(oldName.IsNullOrEmpty() ? "DONOTREMOVE" : oldName).SetValueAsync(Auth.CurrentUser.UserId);
            Debug.LogError($"파이어베이스 에러: {fe.Message}");
            return false;
        }

        //여기까지 왔으면 모든 닉네임 목록에도 새 닉네임을 넣었고, 기존 닉네임은 지웠고,
        //DB상 이 유저의 닉네임을 설정해줬고,
        //인증 정보의 디스플레이네임도 설정해준 것임.

        //남은 처리: 로컬 유저데이터의 이름 변경해주기.
        UserData.Local.master.nickName = newName;
        displayMessage?.Invoke("<color=green>닉네임 설정 완료!</color>\n버튼을 누르면 로비로 이동합니다.");
        //TODO: 포톤에까지 접속 후에 닉네임 변경할 때의 처리 필요.
        //if (PhotonNetwork.IsConnected) PhotonNetwork.LocalPlayer.NickName = newName;

        //최종적으로 true 반환해주기!!
        return true;
    }

    #endregion


    #region 좋아요 보내기 기능 관련 API

    public async Task ToggleFollowPlayer(string uid)
    {
        try
        {
            UserData targetData = await DownloadUserData(uid);
            var targetLikes = targetData.likes ?? new();
            var targetLikesFollowers = targetLikes.followers ?? new();
            var targetMaster = targetData.master;
            int initialCount = targetMaster.totalLikes;
            int delta = 0;
            if (targetLikesFollowers.Contains(Auth.CurrentUser.UserId))
            {
                targetLikesFollowers.Remove(Auth.CurrentUser.UserId);
                delta = -1;
            }
            else
            {
                targetLikesFollowers.Add(Auth.CurrentUser.UserId);
                delta = 1;
            }

            //대상의 데이터 핸들링이 끝났음.

            var myLikes = UserData.Local.likes;
            var myLikesFollowings = myLikes.followings ?? new();

            if (targetLikesFollowers.Contains(Auth.CurrentUser.UserId))
                if (!myLikesFollowings.Contains(uid))
                    myLikesFollowings.Add(uid);

            if (!targetLikesFollowers.Contains(Auth.CurrentUser.UserId))
                if (myLikesFollowings.Contains(uid))
                    myLikesFollowings.Remove(uid);

            targetMaster.totalLikes += delta;

            var targetFlag = targetData.flag;
            targetData.flag |= UserDataDirtyFlag.Master;
            targetData.flag |= UserDataDirtyFlag.Likes;
            string flagString = JsonConvert.SerializeObject(targetData.flag);
            _ = UserDataRef.Child(uid).Child("flag").SetRawJsonValueAsync(flagString);
            _ = UploadUserDataCategory(uid, targetLikes);
            _ = UploadUserDataCategory(uid, targetMaster);
            await UploadUserDataCategory(Auth.CurrentUser.UserId, myLikes);

        }
        catch (FirebaseException fe)
        {
            Debug.LogWarning($"[좋아요 기능]: 에러 발생: {fe.Message}");
        }
    }

    #endregion






    public async Task UnlockAllStages()
    {
        try
        {
            var scores = UserData.Local.progress.scores;
            for (int i = 1; i <= 3; i++)
            {
                for (int  j = 1; j <= 10; j++)
                {
                    if (scores.ContainsKey($"stage_{i}_{j}"))
                        continue;
                    scores.TryAdd($"stage_{i}_{j}", new());
                }
            }
            await UploadLocalUserDataCategory(UserData.Local.progress);
        }
        catch (FirebaseException fe)
        {
            Debug.LogError($"모든 스테이지 해금 실패:{fe.Message}");
        }
    }
}