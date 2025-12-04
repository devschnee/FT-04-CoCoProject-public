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

    bool IsFollowing => UserData.Local.likes.followings.Contains(FriendLobbyManager.Instance.Uid);

    private void Awake()
    {
        
    }
    public async void Start()
    {
        SetLikeButton();
        Recolor();
    }
    private void SetLikeButton()
    {
        likeButton.interactable = !isAwait;
        likeButton.onClick.AddListener(async() => await ToggleLike());


    }
    public async Task ToggleLike()
    {
        isAwait = true;
        likeButton.interactable = !isAwait;

        await FirebaseManager.Instance.FollowPlayer_Outbound(FriendLobbyManager.Instance.Uid, !IsFollowing);

        isAwait = false;
        Recolor();
    }

    public async void Recolor()
    {
        likeButton.interactable = !isAwait;
        likeButtonImage.color = IsFollowing ? Color.white : new Color(0,0,0,0.5f);
    }

    public void ReturnToMyLobby()
    {
        Destroy(FriendLobbyManager.Instance.gameObject);
        SceneManager.LoadScene("Main");
    }
}
