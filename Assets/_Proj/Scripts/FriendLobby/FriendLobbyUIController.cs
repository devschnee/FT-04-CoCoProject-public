using Firebase.Auth;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FriendLobbyUIController : MonoBehaviour
{
    [SerializeField] Button likeButton;
    [SerializeField] Image likeButtonImage;
    bool isAwait = false;

    private void Awake()
    {
        likeButton.onClick.AddListener(async() => await ToggleLike());
        Recolor();
    }
    public async Task ToggleLike()
    {
        isAwait = true;
        likeButton.interactable = !isAwait;
        await FirebaseManager.Instance.ToggleFollowPlayer(FriendLobbyManager.Instance.Uid);
        Recolor();
    }

    public async void Recolor()
    {
        var friendLikes = await FirebaseManager.Instance.DownloadUserDataCategory(FriendLobbyManager.Instance.Uid, UserDataDirtyFlag.Likes) as UserData.Likes;
        isAwait = false;
        likeButton.interactable = !isAwait;
        var isOn = friendLikes.followers.Contains(FirebaseAuth.DefaultInstance.CurrentUser.UserId);
        likeButtonImage.color = isOn ? Color.white : new Color(0,0,0,0.5f);
    }

    public void ReturnToMyLobby()
    {
        Destroy(FriendLobbyManager.Instance.gameObject);
        SceneManager.LoadScene("Main");
    }
}
