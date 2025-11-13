using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneManager : MonoBehaviour
{
    public void ToMainScene()
    {
        SceneManager.LoadScene("Main");
    }
}
