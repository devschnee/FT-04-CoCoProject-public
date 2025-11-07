using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class StageUIManager : MonoBehaviour
{
    public static StageUIManager Instance {  get; private set; }

    public StageManager stageManager;

    [Header("Button")]
    public Button OptionOpenButton;
    public Button OptionCloseButton;
    public Button RetryButton;
    public Button QuitButton;
    public Button ExitButton;
    public Button TreasureConfirmButton;

    [Header("Panel")]
    public GameObject OptionPanel;
    public GameObject ResultPanel;
    public GameObject TreasurePanel;
    public GameObject Overlay;

    [Header("ResultPanel")]
    public TextMeshProUGUI stageName;
    public Image[] star;
    public Image stageImage;
    public TextMeshProUGUI stageText;
    public Image[] reward;

    [Header("TreasurePanel")]
    public Image TreasureImage;
    public TextMeshProUGUI TreasureName;
    public TextMeshProUGUI TreasureType;
    public TextMeshProUGUI TreasureCount;
    public TextMeshProUGUI TreasureDesc;
    public Image CocoDoogyImage;
    public TextMeshProUGUI CocoDoogyDesc;

    public Image[] rewardIcons; // [0], [1], [2] 각각 보물 아이콘
    public Sprite collectedSprite; // 획득된 보물 아이콘
    public Sprite notCollectedSprite; // 미획득 상태 아이콘
    public Action OnTreasureConfirm;

    private string currentChapter;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        OptionOpenButton.onClick.AddListener(OptionOpen);
        OptionCloseButton.onClick.AddListener(OptionClose);
        RetryButton.onClick.AddListener(Retry);
        QuitButton.onClick.AddListener(Quit);
        ExitButton.onClick.AddListener(Exit);
        TreasureConfirmButton.onClick.AddListener(() =>
        {
            OnTreasureConfirm?.Invoke();
            OnTreasureConfirm = null;
        });

        Overlay.SetActive(false);
        OptionOpenButton.gameObject.SetActive(true);
        OptionPanel.SetActive(false);
        ResultPanel.SetActive(false);
    }

    void OptionOpen()
    {
        OptionPanel.SetActive(true);
        Overlay.SetActive(true);
        OptionOpenButton.gameObject.SetActive(false);
    }

    void OptionClose()
    {
        OptionPanel.SetActive(false);
        Overlay.SetActive(false);
        OptionOpenButton.gameObject.SetActive(true);
    }

    void Retry()
    {
        //Todo : 챕터에 따라 분기
        SceneManager.LoadScene("Chapter1_StageScene_TESTONLY");
    }

    void Quit()
    {
        //Todo : 챕터에 따라 스테이지 선택화면 분기
        //currentChapter
        SceneManager.LoadScene("Lobby");
    }

    void Exit()
    {
        //Todo : 챕터에 따라 스테이지 선택화면 분기
        //currentChapter
        SceneManager.LoadScene("Lobby");
    }

    public void UpdateTreasureIcons(bool t1, bool t2, bool t3)
    {
        if (rewardIcons == null || rewardIcons.Length < 3) return;

        rewardIcons[0].sprite = t1 ? collectedSprite : notCollectedSprite;
        rewardIcons[1].sprite = t2 ? collectedSprite : notCollectedSprite;
        rewardIcons[2].sprite = t3 ? collectedSprite : notCollectedSprite;
    }
}