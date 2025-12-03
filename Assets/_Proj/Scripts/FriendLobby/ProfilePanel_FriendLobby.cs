using Firebase.Auth;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfilePanel_FriendLobby : MonoBehaviour
{
    private UserData.Master FriendMaster => FriendLobbyManager.Instance.FriendMaster;
    private string Uid => FriendLobbyManager.Instance.Uid;
    [Header("Basic Info")]
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text uidText;
    [SerializeField] private TMP_Text joindateText;
    [SerializeField] private TMP_Text totalLikeText;

    [Header("Big Profile Image")]
    [SerializeField] private Image profileBigImage;

    [Header("Museum")]
    [SerializeField] private ProfileFavoriteIcon animalIcon;
    [SerializeField] private ProfileFavoriteIcon decoIcon;
    [SerializeField] private ProfileFavoriteIcon costumeIcon;
    [SerializeField] private ProfileFavoriteIcon artifactIcon;
    public void Open()
    {
        UIPanelAnimator.Open(gameObject);
    }

    

    private void OnEnable()
    {
        SetupFriendInfo();
        SetupFriendMuseum();
        FriendMaster.onMasterUpdate += SetupFriendInfo;
        FriendMaster.onMasterUpdate += SetupFriendMuseum;

    }

    private void OnDisable()
    {
        FriendMaster.onMasterUpdate -= SetupFriendInfo;
        FriendMaster.onMasterUpdate -= SetupFriendMuseum;
    }
    private void SetupFriendMuseum()
    {
        animalIcon.Initialize(null, ProfileType.animal, FriendMaster[ProfileType.animal]);
        decoIcon.Initialize(null, ProfileType.deco, FriendMaster[ProfileType.deco]);
        costumeIcon.Initialize(null, ProfileType.costume, FriendMaster[ProfileType.costume]);
        artifactIcon.Initialize(null, ProfileType.artifact, FriendMaster[ProfileType.artifact]);
    }

    private void SetupFriendInfo()
    {
        //var user = _auth.CurrentUser;
        

        if (nicknameText) nicknameText.text = FriendMaster.nickName ?? "-";
        if (uidText) uidText.text = Uid ?? "-";
        if (joindateText)
        {
            var createdAt = FriendMaster.createdAt;
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(createdAt);
            string formatted = dateTimeOffset.ToLocalTime().ToString("yyyy/MM/dd");
            joindateText.text = formatted;
        }
        profileBigImage.sprite = DataManager.Instance.Profile.GetIcon(FriendMaster[ProfileType.icon]);
        totalLikeText.text = $"누적 로비 좋아요  :  {FriendMaster.totalLikes}";


    }

    public void Close()
    {
        UIPanelAnimator.Close(gameObject);
    }
    
}
