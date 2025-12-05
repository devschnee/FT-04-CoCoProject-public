using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StageDetailInfo : MonoBehaviour
{
    public Image stageImage;
    public TextMeshProUGUI stageName;
    public TextMeshProUGUI stageDesc;
    public Transform rewardGroup; // 보상 UI 부모
    public Button enterButton;
    private string currentStageId;

    public Image[] treasureIcons; // 스테이지별 보물 아이콘들 (3개)
    public Sprite collectedSprite;
    public Sprite notCollectedSprite;
    public GameObject EnergyNoticePanel;
    private int consume = 1;

    void Awake()
    {
        // LSH 추가 1125
        enterButton.onClick.AddListener(() => {EnterStage(); AudioEvents.Raise(UIKey.Normal, 2);});
    }
    public void OnEnable()
    {
        UIPopupAnimator.Open(gameObject);
    }

    public void Close()
    {
        UIPopupAnimator.Close(gameObject);
    }

    public void ShowDetail(string id)
    {
        currentStageId = id; 
        var data = DataManager.Instance.Stage.GetData(id);
        var progress = PlayerProgressManager.Instance.GetStageProgress(id);

        stageImage.sprite = DataManager.Instance.Stage.GetIcon(id);
        stageName.text = data.stage_name;
        stageDesc.text = data.stage_desc;

        // 총 몇 개 먹었는지 카운트
        int collectedCount = progress.GetCollectedCount();

        for (int i = 0; i < 3; i++)
        {
            treasureIcons[i].sprite = i < collectedCount ? collectedSprite : notCollectedSprite;
        }
    }

    async void EnterStage()
    {
        if (UserData.Local.goods[110001] < consume)
        {
            //Todo : 행동력이 부족하다면 추가 팝업 보여주고 리턴
            print("행동려기부족카당");
            Instantiate(EnergyNoticePanel, transform);
            return;
        }
        await FirebaseManager.Instance.FindMapDataByStageID(currentStageId);

        //행동력을 consume 만큼 빼줘야함
        UserData.Local.goods[110001] -= consume;
        UserData.Local.goods.Save();

        if (currentStageId.Contains("stage_1"))
        {
            SceneManager.LoadScene("Chapter1_StageScene"); 
        }
        else if (currentStageId.Contains("stage_2"))
        {
            SceneManager.LoadScene("Chapter2_StageScene");
        }
        else if (currentStageId.Contains("stage_3"))
        {
            SceneManager.LoadScene("Chapter3_StageScene");
        }
    }

    public void CloseButton()
    {
        // LSH 추가 1125
        AudioEvents.Raise(UIKey.Normal, 2);
        //
        currentStageId = null;
        gameObject.SetActive(false);
        GameObject obj = GameObject.Find("SelectStageDimOverlay");
        obj.SetActive(false);
    }
}