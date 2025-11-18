using UnityEngine;
using UnityEngine.UI;

// TODO : 구글 로그인 기능 여기에 추가
public class OptionPanelController : MonoBehaviour
{
    public OptionPanelController Instance;

    [Header("Popups")]
    [SerializeField] private AccountLinkAskPopup accountLinkAskPopup;
    [SerializeField] private LogoutPopup logoutPopup;
    [SerializeField] private GameInfoPopup gameInfoPopup;
    [SerializeField] private GameQuitPopup gameQuitPopup;

    [SerializeField] private GameObject dim;

    [Header("LinkPopups")]
    public GameObject accountLinkSuccessPopup;
    public GameObject accountLinkFailPopup;

    [SerializeField] private Button btnLinkAccount;
    [SerializeField] private Button btnLogout;
    [SerializeField] private Button btnGameInfo;
    [SerializeField] private Button btnGameQuit;

    void Awake()
    {
        if (accountLinkAskPopup) accountLinkAskPopup.gameObject.SetActive(false);
        if (logoutPopup) logoutPopup.gameObject.SetActive(false);
        if (gameInfoPopup) gameInfoPopup.gameObject.SetActive(false);
        if (gameQuitPopup) gameQuitPopup.gameObject.SetActive(false);
        if(dim) dim.SetActive(false);
        if(dim) dim.SetActive(false);

        if (btnLinkAccount) btnLinkAccount.onClick.AddListener(() => accountLinkAskPopup.Open());
        if (btnLogout) btnLogout.onClick.AddListener(() => logoutPopup.Open());
        if (btnGameInfo) btnGameInfo.onClick.AddListener(() => gameInfoPopup.Open());
        if (btnGameQuit) btnGameQuit.onClick.AddListener(() => gameQuitPopup.Open());
    }

    public void LinkAccountOpen()
    {
        accountLinkAskPopup.Open();
    }

    public void GameInfoOpen()
    {
        gameInfoPopup.Open();
    }

    private void LogoutOpen()
    {
        logoutPopup.Open();
    }

    public void GameQuitOpen()
    {
        gameQuitPopup.Open();
    }

    public void TryGoogleLogin()
    {
        Debug.Log("구글 로그인 시도!");
        // TODO : 구글 로그인
    }

    // 로그인 성공/실패 콜백
    public void OnGoogleLoginSuccess()
    {
        accountLinkSuccessPopup.SetActive(true);
    }

    public void OnGoogleLoginFail()
    {
        accountLinkFailPopup.SetActive(true);
    }
}
