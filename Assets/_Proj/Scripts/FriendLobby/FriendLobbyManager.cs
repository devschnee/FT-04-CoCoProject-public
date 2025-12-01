using UnityEngine;
using UnityEngine.SceneManagement;
public class FriendLobbyManager : MonoBehaviour
{
    public UserData.Master friendMaster { get; private set; }
    public UserData.Lobby friendLobby { get; private set; }

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

    public void Init(UserData.Master master, UserData.Lobby lobby)
    {
        friendMaster = master;
        friendLobby = lobby;
    }
}
