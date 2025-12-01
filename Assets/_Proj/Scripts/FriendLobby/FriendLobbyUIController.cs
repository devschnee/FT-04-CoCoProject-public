using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FriendLobbyUIController : MonoBehaviour
{




    public void ReturnToMyLobby()
    {
        Destroy(FriendLobbyManager.Instance.gameObject);
        SceneManager.LoadScene("Main");
    }
}
