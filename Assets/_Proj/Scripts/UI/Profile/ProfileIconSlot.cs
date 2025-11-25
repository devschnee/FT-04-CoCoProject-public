using UnityEngine;
using UnityEngine.UI;

public class ProfileIconSlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private GameObject badgeIcon;
    [SerializeField] private Button button;

    private int _itemId;
    private Sprite _icon;
    private System.Action<int, Sprite, ProfileIconSlot> _onClick;

    private readonly Color ACTIVE_COLOR = Color.white;
    private readonly Color INACTIVE_COLOR = new(.1f, .1f, .1f, .8f);

    public void Bind(int itemId, Sprite icon, System.Action<int, Sprite, ProfileIconSlot> onClick)
    {
        _itemId = itemId;
        _icon = icon;
        _onClick = onClick;

        if (iconImage)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        SetSelected(false);

        //TODO: 프로필 해금 보관소 만들기...
        //SetActive(false);

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke(_itemId, _icon, this));
        }
    }

    private void SetActive(bool isUnlocked)
    {
        button.interactable = isUnlocked;
        iconImage.color = isUnlocked ? ACTIVE_COLOR : INACTIVE_COLOR;
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrame)
            selectedFrame.SetActive(selected);

        if (badgeIcon)
            badgeIcon.SetActive(selected);
    }
}