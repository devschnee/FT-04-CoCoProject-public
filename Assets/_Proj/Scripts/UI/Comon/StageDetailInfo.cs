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
    private int consume = 1;

    void Awake()
    {
        enterButton.onClick.AddListener(EnterStage);
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
        //if (UserData.Local.goods[GoodsType.energy] < consume)
        //{
        //    //Todo : 행동력이 부족하다면 추가 팝업 보여주고 리턴
        //    print("행동려기부족카당");
        //    return;
        //}
        await FirebaseManager.Instance.FindMapDataByStageID(currentStageId);

        //행동력을 consume 만큼 빼줘야함
        //UserData.Local.goods[100001] -= consume;
        //UserData.Local.goods.Save();

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
        currentStageId = null;
        gameObject.SetActive(false);
        GameObject obj = GameObject.Find("SelectStageDimOverlay");
        obj.SetActive(false);
    }
}