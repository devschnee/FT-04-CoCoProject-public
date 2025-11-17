using UnityEngine;
using UnityEngine.UI;

public class GameQuitPopup : MonoBehaviour
{
    [SerializeField] private Button yesBtn;
    [SerializeField] private Button noBtn;
    [SerializeField] private GameObject dim;

    public void Open()
    {
        dim.SetActive(true);
        gameObject.SetActive(true);
    }

    public void OnQuitYes()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        dim.SetActive(false);
    }
}
