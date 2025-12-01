using Firebase.Auth;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSceneManager : MonoBehaviour
{

    [SerializeField] private Image titleBackground;
    [SerializeField] private RectTransform titleLogo;
    [SerializeField] private TextMeshProUGUI proceedText;
    [SerializeField] private TextMeshProUGUI logMessage;
    [SerializeField] private CanvasGroup loginMethods;
    

    public float logoFadeTime = 2f;
    public float logoWaitTime = 1f;
    public float logoMoveDuration = 3f;
    public float loginMethodsFadeTime = 1f;
    public Vector2 logoTargetPos = new(0, 155);

    void Start()
    {
        StartCoroutine(IntroCoroutine());
        //FirebaseManager.Instance.onLog += OnLog;
    }


    
    IEnumerator IntroCoroutine()
    {
        yield return new WaitUntil(()=>FirebaseManager.Instance != null && FirebaseManager.Instance.IsInitialized);
        while (titleBackground.color.r < Color.white.r)
        {
            titleBackground.color += Color.white * (Time.deltaTime * 1 / logoFadeTime);
            yield return null;
        }
        titleBackground.color = Color.white;
        yield return new WaitForSeconds(logoWaitTime);

        float progress = 0;
        Vector2 logoStartPos = titleLogo.anchoredPosition;
        while (progress <= 1)
        {
            Color logoColor = new(1, 1, 1, progress);
            titleLogo.anchoredPosition = Vector2.Lerp(logoStartPos, logoTargetPos, progress);
            titleLogo.GetComponent<Image>().color = logoColor;
            progress += (Time.deltaTime * (1 / logoMoveDuration));
            yield return null;
        }
        titleLogo.anchoredPosition = logoTargetPos;
        titleLogo.GetComponent<Image>().color = Color.white;

        if (!FirebaseManager.Instance.IsSignedIn)
        {
            StartCoroutine(LoginMethodsCoroutine());
        }
        StartCoroutine(SceneTransitCoroutine());
        StartCoroutine(ProceedTextBlinkCoroutine());

    }
    IEnumerator LoginMethodsCoroutine()
    {
        loginMethods.alpha = 0;
        float coroutineDur = 0;
        while (coroutineDur < loginMethodsFadeTime)
        {
            loginMethods.alpha += (1 / loginMethodsFadeTime) * Time.deltaTime;
            coroutineDur += Time.deltaTime;
            yield return null;
        }
        loginMethods.alpha = 1;
        loginMethods.interactable = true;

        //FirebaseManager.Instance.IsSignedIn을 기다림.
        yield return new WaitUntil(() => FirebaseManager.Instance.IsSignedIn);
        loginMethods.alpha = 0;
        loginMethods.interactable = false;
        loginMethods.gameObject.SetActive(false);
    }

    //잠시 닫아둠: 씬 전환 코루틴은 구글로그인 기능 구현 후 부활 예정.
    IEnumerator SceneTransitCoroutine()
    {
        var touch = Touchscreen.current;
        yield return new WaitWhile(() => UserData.Local == null);
        yield return new WaitUntil(() =>
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.isPressed)
                    return true;
            }
            return false;
        });

        //TODO: 튜토리얼 씬 구성 후, UserData.Local.passedTutorials를 제대로 대입해주어야 함.
        //모든 튜토리얼을 끝냈을 때만 메인을 직접 로드.
        //if (UserData.Local.passedTutorials >= 2)

        if (UserData.Local.passedTutorials >= 3)
            if (!UserData.Local.master.nickName.IsNullOrEmpty())
                 { SceneManager.LoadScene("Main"); }
            else { SceneManager.LoadScene("Nickname"); }
        else
            SceneManager.LoadScene("Chapter0_StageScene");
                //모든 튜토리얼을 끝내지 못했다면 튜토리얼로 로드.
                //else if (UserData.Local.passedTutorials < 2)

                //SceneManager.LoadScene("Tutorial");
    }

    //잠시 닫아둠: 씬 전환 코루틴과 동일한 이유.
    IEnumerator ProceedTextBlinkCoroutine()
    {
        float alphaMod = 0;
        bool isDescend = false;
        yield return new WaitWhile(() => UserData.Local == null);
        while (true)
        {
            if (!isDescend)
            {
                alphaMod += Time.deltaTime;
                if (alphaMod >= 1) isDescend = true;
                proceedText.alpha = alphaMod;
                yield return null;
            }
            else
            {
                alphaMod -= Time.deltaTime;
                proceedText.alpha = alphaMod;
                if (alphaMod <= 0) isDescend = false;
                yield return null;
            }
        }
    }

    public void ToMainScene()
    {
        SceneManager.LoadScene("Main");
    }
        //else
            //SceneManager.LoadScene("튜토리얼1");


        /* 튜토리얼씬 매니저가 따로 있을 수도 있고 뭐 없을 수도 있긴 한데
           튜토리얼씬2 매니저의 클리어 타이밍에 UserData.Local.isTutorialPlayed = true;해준 다음에,
           UserData.Local.Save(); 호출시켜주면 유저는 튜토리얼스테이지 2개를 무조건 통과해야만 하게 되고,
           튜토리얼스테이지 2개를 모두 돌파한 유저는 무조건 메인 씬으로 들어가게 됨. */
 
    public void OnLog(string msg)
    {
        logMessage.alpha = 1;
        logMessage.text = msg;
    }

    public async void OnGuestLoginClick() => await FirebaseManager.Instance.SignInAnonymously();

    public async void OnGoogleLoginClick() => await FirebaseManager.Instance.GoogleLogin();
}
