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

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke(_itemId, _icon, this));
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrame)
            selectedFrame.SetActive(selected);

        if (badgeIcon)
            badgeIcon.SetActive(selected);
    }
}