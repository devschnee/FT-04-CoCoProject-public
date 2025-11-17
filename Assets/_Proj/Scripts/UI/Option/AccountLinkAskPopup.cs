using System.Threading.Tasks;
using UnityEngine;

public class AccountLinkAskPopup : MonoBehaviour
{
    [SerializeField] private GameObject dim;
    OptionPanelController optionPanelController;

    public void Open()
    {
        dim.SetActive(true);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        dim.SetActive(false);
    }

    public void Confirm()
    {
        // dim은 살아 있어야 함. dim은 linkSuccess/linkFail에서 close될때 false되도록 처리. success/fail의 confirm버튼에 이 스크립트의 close를 불러오도록 onclick에 연결되어 있음.
        gameObject.SetActive(false);
        optionPanelController.Instance.TryGoogleLogin();
    }
}
