using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class FriendLobbyManager : MonoBehaviour
{
    public UserData.Master FriendMaster { get; private set; }
    public UserData.Lobby FriendLobby { get; private set; }
    public UserData Friend { get; private set;  }
    public string Uid { get; private set; }
    public static FriendLobbyManager Instance { get; private set; }

    public float updateInterval = 3f;
    public float currentTimer;
    public bool isRecentlyUpdated;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    
    }


    private void Update()
    {
        currentTimer += Time.deltaTime;
        if (currentTimer > 1f && isRecentlyUpdated)
        {
            isRecentlyUpdated = false;
        }
        if (currentTimer > updateInterval)
        {
            currentTimer = 0f;
            isRecentlyUpdated = true;
            HelpMaintain();
        }
    }

    public async void HelpMaintain()
    {
        var friendMaster = await FirebaseManager.Instance.DownloadUserDataCategory(Uid, UserDataDirtyFlag.Master) as UserData.Master;
        Friend.master.totalLikes = friendMaster.totalLikes;
        Friend.master.profile = friendMaster.profile;
        Friend.master.onMasterUpdate?.Invoke();
    }

    public void Init(UserData friend, string friendUid)
    {
        Friend = friend;
        FriendMaster = friend.master;
        FriendLobby = friend.lobby;
        Uid = friendUid;
    }

}
