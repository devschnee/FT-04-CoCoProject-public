using UnityEngine;
using UnityEngine.SceneManagement;

public class LogoutPopup : MonoBehaviour
{

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // 로그아웃 '확인' 시, 타이틀 화면으로
    public void CloseAndGotoTitle()
    {
        gameObject.SetActive(false);
        SceneManager.LoadScene("Title");
    }
}
