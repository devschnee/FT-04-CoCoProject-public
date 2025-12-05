using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileIconSelector : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform slotParent;
    [SerializeField] private ProfileIconSlot iconSlotPrefab;  // 아이콘 전용 슬롯
    [SerializeField] private Button applyButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private Button profileInfoIconBtn;
    [SerializeField] private Button rootCloseBtn;

    private ProfilePanelController _panel;
    private int _selectedId = -1;
    private Sprite _selectedSprite;

    private void Awake()
    {
        if (root) root.SetActive(false);
        //LSH 추가 1125
        if (applyButton) applyButton.onClick.AddListener(() => {Apply(); AudioEvents.Raise(UIKey.Normal, 2);});
        if (closeButton) closeButton.onClick.AddListener(()=> {Close(); AudioEvents.Raise(UIKey.Normal, 1);});
    }

    public void Open(ProfilePanelController panel)
    {
        // 이 패널이 열렸을 때 프로필 패널 최상위 오브젝트들에 배치된 버튼들이 눌리지 않도록.
        profileInfoIconBtn.enabled = false;
        rootCloseBtn.enabled = false;

        _panel = panel;
        _selectedId = -1;
        _selectedSprite = null;

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (!cg)
            cg = root.AddComponent<CanvasGroup>();

        cg.alpha = 0f; // 첫 렌더를 완전히 숨김
        cg.interactable = false;
        cg.blocksRaycasts = false;

        if (root) root.SetActive(false);

        if (titleText) titleText.text = "프로필 선택";

        Rebuild();
        Canvas.ForceUpdateCanvases();

        if (root) root.SetActive(true);
        StartCoroutine(ResetScrollNextFrame());
        UIPopupAnimator.Open(gameObject);

        cg.alpha = 1f; // 완전히 정상 렌더된 UI만 보여줌
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    public void Rebuild()
    {
        foreach (Transform t in slotParent)
            Destroy(t.gameObject);

        // icon 타입만 가져옴
        IReadOnlyList<ProfileEntry> profileIcons = _panel.profileService.GetByType(ProfileType.icon);

        foreach (var p in profileIcons)
        {
            var slot = Instantiate(iconSlotPrefab, slotParent);
            bool unlocked = _panel.IsIconUnlocked(p.id);

            //12.05mj 해금된 아이콘 중에서 새 아이콘인지 체크
            bool isNew = ProfileRedDotManager.IsNewIcon(p.id);
            slot.Bind(p.id, p.icon, unlocked, isNew, OnSlotSelected);
        }
    }

    private void OnSlotSelected(int id, Sprite icon, ProfileIconSlot slot)
    {
        _selectedId = id;
        _selectedSprite = icon;

        // 12.05mj 이 아이콘은 이제 “본 것”으로 처리 → 빨간점 제거
        ProfileRedDotManager.MarkSeenIcon(id);

        slot.SetNewDotVisible(false);

        foreach (Transform t in slotParent)
        {
            var s = t.GetComponent<ProfileIconSlot>();
            if (s != null) s.SetSelected(s == slot);
        }
    }

    private void Apply()
    {
        if (_selectedId < 0) return;

        // 저장
        //_panel.SaveEquipped(ProfileType.icon, _selectedId);
        //UserData.Local.master[ProfileType.icon] = _selectedId;
        //UserData.Local.master.Save();
        // 패널/외부에 알림
        //_panel.NotifyProfileIconChanged(_selectedSprite, _selectedId);
        _panel.EquipProfileIcon(_selectedId);

        // 12.05mj 이 아이콘은 이제 “본 것”으로 처리 → 빨간점 제거
        ProfileRedDotManager.MarkSeenIcon(_selectedId);

        Close();
    }

    public void Close()
    {
        if (root) root.SetActive(false);
        if (_panel != null)

        _panel = null;
        _selectedId = -1;
        _selectedSprite = null;
        UIPopupAnimator.Close(gameObject);
        profileInfoIconBtn.enabled = true;
        rootCloseBtn.enabled = true;
    }

    public IEnumerator ResetScrollNextFrame()
    {
        yield return null;
        if (!scroll) yield break;

        scroll.verticalNormalizedPosition = 1f;

        float contentH = scroll.content.rect.height;
        float viewH = ((RectTransform)scroll.viewport).rect.height;
        bool canScroll = contentH > viewH + 2f;

        scroll.vertical = canScroll;
        if (scroll.verticalScrollbar)
            scroll.verticalScrollbar.gameObject.SetActive(canScroll);
    }
}