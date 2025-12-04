using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    public GameObject TreasureCollectedPanel;
    public GameObject DialoguePanel;
    public GameObject Overlay;
    public GameObject FadePanel;

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

    [Header("DialoguePanel")]
    public Image DialogueSpeakerLeft;
    public Image DialogueSpeakerRight;
    public TextMeshProUGUI DialogueNameText;
    public TextMeshProUGUI DialogueText;

    [Header("TreasureCollectedSprite")]
    public Sprite collectedSprite; // 획득된 보물 아이콘
    public Sprite notCollectedSprite; // 미획득 상태 아이콘
    public Sprite CoCoDoogySprite;
    public Sprite[] ResultCoCoDoogySprite;
    public Action OnTreasureConfirm;

    public TextMeshProUGUI CollecTreausreCountText;

    public GameObject videoImage;

    [HideInInspector]public StageIdInformation stageIdInformation;
    public int stageGetTreasureCount;

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
        TreasureConfirmButton.onClick.AddListener(() =>
        {
            OnTreasureConfirm?.Invoke();
            OnTreasureConfirm = null;
        });

        Overlay.SetActive(false);
        //OptionOpenButton.gameObject.SetActive(true);
        OptionPanel.SetActive(false);
        ResultPanel.SetActive(false);
        
        stageIdInformation = FindAnyObjectByType<StageIdInformation>();
        stageImage.sprite = ResultCoCoDoogySprite[0];
        stageGetTreasureCount = 0;
    }

    void OptionOpen()
    {
        //LSH 1120 추가
        //AudioEvents.Raise(UIKey.Normal, 0);
        OptionPanel.SetActive(true);
        Overlay.SetActive(true);
        OptionOpenButton.gameObject.SetActive(false);
        
        Joystick joystick = FindAnyObjectByType<Joystick>();
        if (joystick != null)
        {
            // KHJ - Option Panel이 켜졌으니 조이스틱 입력 잠금
            joystick.IsLocked = true;
        }
    }

    void OptionClose()
    {
        //LSH 1120 추가
        AudioEvents.Raise(UIKey.Normal, 1);
        OptionPanel.SetActive(false);
        Overlay.SetActive(false);
        OptionOpenButton.gameObject.SetActive(true);

        Joystick joystick = FindAnyObjectByType<Joystick>();
        if (joystick != null)
        {
            joystick.IsLocked = false;
        }
    }

    void Retry()
    {
        if (stageManager.isTest) { SceneManager.LoadScene("Chapter1_StageScene_TESTONLY"); return; }
        //LSH 1120 추가
        AudioEvents.Raise(UIKey.Normal, 2);
        //Todo : 챕터에 따라 분기
        if (stageManager.currentStageId.Contains("stage_1"))
        {
            SceneManager.LoadScene("Chapter1_StageScene");
        }
        else if (stageManager.currentStageId.Contains("stage_2"))
        {
            SceneManager.LoadScene("Chapter2_StageScene");
        }
        else if (stageManager.currentStageId.Contains("stage_3"))
        {
            SceneManager.LoadScene("Chapter3_StageScene");
        }
    }

    void Quit()
    {
        //Todo : 챕터에 따라 스테이지 선택화면 분기

        
        //currentChapter

        //StageManager.currentStageId를 보면 됨.
        //currentStageId.Contains("0_1") => 튜토리얼 1번씬
        //currentStageId.Contains("0_2") => 튜토리얼 2번씬
        //위의 두 케이스 모두 타이틀씬을 불러오도록 하기.
        //LSH 1120 추가
        AudioEvents.Raise(UIKey.Normal, 2);
        SceneManager.LoadScene("Main");
    }

    //void Exit()
    //{
    //    //StageManager로 기능이관
    //    //Todo : 챕터에 따라 스테이지 선택화면 분기
    //    //currentChapter
    //    SceneManager.LoadScene("Main");
    //}
}