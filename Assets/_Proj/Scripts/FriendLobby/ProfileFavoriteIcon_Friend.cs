using UnityEngine;

public class ProfileFavoriteIcon_Friend : ProfileFavoriteIcon
{
    public override void Initialize(ProfilePanelController panel, ProfileType type, int itemId)
    {
        //if (isInitialized) return;


        this.panel = panel;
        this.type = type;
        currentItemId = itemId;

        if (iconImage)
        {

            iconImage.sprite = itemId < 0 ? null : DataManager.Instance.Codex.GetCodexIcon(itemId);
            iconImage.enabled = iconImage.sprite != null;
        }

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
        isInitialized = true;
    }
    protected override void OnClick()
    {
        
    }
}
