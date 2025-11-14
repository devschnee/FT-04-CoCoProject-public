using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneManager : MonoBehaviour
{
    public void ToMainScene()
    {
        //if (UserData.Local.isTutorialPlayed)
            SceneManager.LoadScene("Main");
        //else
            //SceneManager.LoadScene("튜토리얼1");


        /* 튜토리얼씬 매니저가 따로 있을 수도 있고 뭐 없을 수도 있긴 한데
           튜토리얼씬2 매니저의 클리어 타이밍에 UserData.Local.isTutorialPlayed = true;해준 다음에,
           UserData.Local.Save(); 호출시켜주면 유저는 튜토리얼스테이지 2개를 무조건 통과해야만 하게 되고,
           튜토리얼스테이지 2개를 모두 돌파한 유저는 무조건 메인 씬으로 들어가게 됨. */
    }
}
