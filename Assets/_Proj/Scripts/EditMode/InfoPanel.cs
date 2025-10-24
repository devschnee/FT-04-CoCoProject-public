using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text descText;
    [SerializeField] Image iconImage;
    [SerializeField] Button closeButton;

    void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Hide);
        Hide();
    }

    public void Show(string title, string desc, Sprite icon = null)
    {
        if (titleText) titleText.text = title ?? "";
        if (descText) descText.text = desc ?? "";
        if (iconImage)
        {
            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
        }
        if (root) root.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (root) root.SetActive(false);
        else gameObject.SetActive(false);
    }
}
