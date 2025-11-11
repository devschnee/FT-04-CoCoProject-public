using UnityEngine;
using UnityEngine.UI;

public class FirebaseUITest : MonoBehaviour
{
    //public Button testLoginButton_Anonymous;



    public async void OnAnonymousLoginButtonClick()
    {
        if (!FirebaseManager.Instance && !FirebaseManager.Instance.IsInitialized) return;
        await FirebaseManager.Instance.SignInAnonymouslyTest((x) => Debug.Log($"파이어베이스 로그인 테스트: [성공]{x.UserId}로 로그인 성공"), x => Debug.LogWarning($"파이어베이스 로그인 테스트: [실패] - {x.Message}"));

    }
}
