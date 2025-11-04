using UnityEngine;
using UnityEngine.UI;

public class StageUIManager : MonoBehaviour
{
    [Header("Button")]
    public Button OptionOpenButton;
    public Button OptionCloseButton;
    public Button RetryButton;
    public Button QuitButton;

    [Header("OptionObject")]
    public GameObject OptionImg;

    void Awake()
    {
        OptionOpenButton.onClick.AddListener(OptionOpen);
        OptionCloseButton.onClick.AddListener(OptionClose);
        RetryButton.onClick.AddListener(Retry);
        QuitButton.onClick.AddListener(Quit);
        OptionImg.SetActive(false);
    }

    void OptionOpen()
    {
        OptionImg.SetActive(true);
        OptionOpenButton.gameObject.SetActive(false);
    }

    void OptionClose()
    {
        OptionImg.SetActive(false);
        OptionOpenButton.gameObject.SetActive(true);
    }

    void Retry()
    {
        print("다시하지롱");
        //
    }

    void Quit()
    {
        print("나가지롱");
    }
}