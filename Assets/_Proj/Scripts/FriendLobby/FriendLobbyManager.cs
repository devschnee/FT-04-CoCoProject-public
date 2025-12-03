using UnityEngine;
using UnityEngine.SceneManagement;
public class FriendLobbyManager : MonoBehaviour, IQuestBehaviour
{
    public UserData.Master FriendMaster { get; private set; }
    public UserData.Lobby FriendLobby { get; private set; }
    public UserData Friend { get; private set;  }
    public string Uid { get; private set; }
    public static FriendLobbyManager Instance { get; private set; }
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

    public void Init(UserData friend, string friendUid)
    {
        QuestManager.Instance.Handle(this);
        Friend = friend;
        FriendMaster = friend.master;
        FriendLobby = friend.lobby;
        Uid = friendUid;
    }
}
