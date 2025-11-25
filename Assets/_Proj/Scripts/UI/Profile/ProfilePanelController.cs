using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProfilePanelController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] public ProfileService profileService;

    [Header("Top Info (optional)")]
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text uidText;
    [SerializeField] private TMP_Text joindateText;
    [SerializeField] private TMP_Text totalLikeText;

    [Header("Category Icons (item slots)")]
    [SerializeField] private ProfileFavoriteIcon animalIcon;
    [SerializeField] private ProfileFavoriteIcon decoIcon;
    [SerializeField] private ProfileFavoriteIcon costumeIcon;
    [SerializeField] private ProfileFavoriteIcon artifactIcon;

    [Header("Popups")]
    [SerializeField] private ProfileItemSelector itemSelectorPopup;   // 아이템용 UI
    [SerializeField] private ProfileIconSelector iconSelectorPopup;   // 프로필아이콘용 UI

    // 외부(헤더)에 있는 프로필 버튼이 여기 구독해서 아이콘만 바꾸면 됨
    public System.Action<Sprite, int> OnProfileIconChanged;

    //이제 이 클래스는 파이어베이스 Auth를 전혀 몰라도 됩니다.
    //Firebase Auth가 필요한 부분은 uid 외에는 없음. 따라서 임시 변수로 돌립니다.
    //private FirebaseAuth _auth;

    private void Awake()
    {
        //_auth = FirebaseAuth.DefaultInstance;
    }

    private void OnEnable()
    {
        SetupUserInfo();
        SetupCategoryIcons();
        SetupProfileIconForIconPopup();
        UIPanelAnimator.Open(gameObject);
    }
    public void Close()
    {
        UIPanelAnimator.Close(gameObject);
    }

    private void SetupUserInfo()
    {
        //var user = _auth.CurrentUser;
        var user = UserData.Local.master;

        if (nicknameText) nicknameText.text = UserData.Local.master.nickName ?? "-";
        if (uidText) uidText.text = FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "-";
        if (joindateText)
        {
            var createdAt =  UserData.Local.master.createdAt; 
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(createdAt);
            string formatted = dateTimeOffset.ToLocalTime().ToString("yyyy/MM/dd");
            joindateText.text = formatted;
        }


    }

    private void SetupCategoryIcons()
    {
        InitCategory(ProfileType.animal, animalIcon);
        InitCategory(ProfileType.deco, decoIcon);
        InitCategory(ProfileType.costume, costumeIcon);
        InitCategory(ProfileType.artifact, artifactIcon);
    }

    private void InitCategory(ProfileType type, ProfileFavoriteIcon target)
    {
        if (!target || !profileService) return;

        target.Initialize(this, type, -1);
        
        //if (type == ProfileType.animal)
        //{
        //    int startingId = 30000;
        //    foreach(var entry in entries)
        //    {
        //        entry.Init(++startingId, profileService);
        //    }
        //}
        //if (type == ProfileType.deco)
        //{
        //    int startingId = 10000;
        //    foreach (var entry in entries)
        //    {
        //        entry.Init(++startingId, profileService);
        //    }
        //}
        //if (type == ProfileType.costume)
        //{
        //    int startingId = 20000;
        //    foreach (var entry in entries)
        //    {
        //        entry.Init(++startingId, profileService);
        //    }
        //}
        //if (type == ProfileType.artifact)
        //{
        //    int startingId = 50000;
        //    foreach (var entry in entries)
        //    {
        //        entry.Init(++startingId, profileService);
        //    }
        //}

        
        
        
        
    }

    // 아이콘 팝업 켰을 때 첫 리스트에 노출할 실제 아이콘
    private void SetupProfileIconForIconPopup()
    {

    }

    // 동물/조경/치장/유물 아이콘 눌렀을 때
    public void OpenItemPopup(ProfileType type, ProfileFavoriteIcon caller)
    {
        if (!itemSelectorPopup) return;
        itemSelectorPopup.gameObject.SetActive(true);
        itemSelectorPopup.Open(type, caller, this);
    }

    // 프로필아이콘 변경 버튼 눌렀을 때
    public void OpenProfileIconPopup()
    {
        if (!iconSelectorPopup) return;
        iconSelectorPopup.gameObject.SetActive(true);
        iconSelectorPopup.Open(this);
    }

    

    

    // 프로필 아이콘 저장(Firebase)
    // ???? 이건 등록된 프로필 아이콘을 저장할 방법을 궁리해봐야 할 듯. 프로필 아이콘은 유저가 업로드할 이미지들인지? 게임 리소스로 존재할 것인지?
    // 단, 유저가 직접 업로드하는 이미지라면 일반적인 방법으로는 프로필 아이콘 설정이 불가능함.
    //TODO: 이거 고쳐야 함. 잘못된 노드를 firebase에 만들고 있음.
    //public void SaveEquipped(ProfileType type, int itemId)
    //{
    //    var user = FirebaseAuth.DefaultInstance.CurrentUser;
    //    if (user == null) return;

    //    FirebaseDatabase.DefaultInstance
    //        .GetReference($"users/{user.UserId}/profile/equipped/{type}")
    //        .SetValueAsync(itemId);
    //}

    // 프로필 아이콘 바뀌었을 때: 패널 안 + 외부에 알림
    public void NotifyProfileIconChanged(Sprite icon, int itemId)
    {
        // 패널 안에서 다른 UI들도 바꾸고 싶으면 여기서 처리
        OnProfileIconChanged?.Invoke(icon, itemId);
    }


}