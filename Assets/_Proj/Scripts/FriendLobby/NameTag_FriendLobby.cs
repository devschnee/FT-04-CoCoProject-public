using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NameTag_FriendLobby : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    [SerializeField] Image profileImage;
    [SerializeField] TMP_Text likeCountText;
    [SerializeField] ProfilePanel_FriendLobby friendProfilePanel;
    private void Awake()
    {
        SetNametagInfo();
        GetComponent<Button>().onClick.AddListener(() => friendProfilePanel.Open());
        FriendLobbyManager.Instance.FriendMaster.onMasterUpdate += SetNametagInfo;
    }

    private void SetNametagInfo()
    {
        nameText.text = FriendLobbyManager.Instance.FriendMaster.nickName;
        profileImage.sprite = DataManager.Instance.Profile.GetIcon(FriendLobbyManager.Instance.FriendMaster.profile["icon"]);
        likeCountText.text = FriendLobbyManager.Instance.FriendMaster.totalLikes.ToString();
    }
}
