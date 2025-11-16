using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSceneManager : MonoBehaviour
{

    [SerializeField] private Image titleBackground;
    [SerializeField] private RectTransform titleLogo;
    [SerializeField] private TextMeshProUGUI proceedText;

    public float logoFadeTime = 2f;
    public float logoWaitTime = 1f;
    public float logoMoveDuration = 3f;
    void Start()
    {
        StartCoroutine(IntroCoroutine());
    }

    IEnumerator IntroCoroutine()
    {
        while (titleBackground.color.r < Color.white.r)
        {
            titleBackground.color += Color.white * (Time.deltaTime * 1 / logoFadeTime);
            yield return null;
        }
        titleBackground.color = Color.white;
        yield return new WaitForSeconds(logoWaitTime);

        float progress = 0;
        Vector2 logoStartPos = titleLogo.anchoredPosition;
        Vector2 logoTargetPos = new(0, 350);
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
        StartCoroutine(SceneTransitCoroutine());
        yield return ProceedTextBlinkCoroutine();
        
    }

    IEnumerator SceneTransitCoroutine()
    {
        var touch = Touchscreen.current;
        while (true)
        {
            if (touch.press.isPressed)
            {
                //if (UserData.Local.isTutorialPlayed)
                SceneManager.LoadScene("Main");
                yield break;
            }
            yield return null;
        }
    }
    IEnumerator ProceedTextBlinkCoroutine()
    {
        float alphaMod = 0;
        bool isDescend = false;
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
    
        
        //else
            //SceneManager.LoadScene("튜토리얼1");


        /* 튜토리얼씬 매니저가 따로 있을 수도 있고 뭐 없을 수도 있긴 한데
           튜토리얼씬2 매니저의 클리어 타이밍에 UserData.Local.isTutorialPlayed = true;해준 다음에,
           UserData.Local.Save(); 호출시켜주면 유저는 튜토리얼스테이지 2개를 무조건 통과해야만 하게 되고,
           튜토리얼스테이지 2개를 모두 돌파한 유저는 무조건 메인 씬으로 들어가게 됨. */
 


    
}
