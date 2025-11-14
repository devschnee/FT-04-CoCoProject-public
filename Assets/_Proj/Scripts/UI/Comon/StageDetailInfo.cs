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

    void Awake()
    {
        enterButton.onClick.AddListener(EnterStage);
    }

    public void ShowDetail(string id)
    {
        currentStageId = id; 
        var data = DataManager.Instance.Stage.GetData(id);
        var progress = PlayerProgressManager.Instance.GetStageProgress(id);

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
        await FirebaseManager.Instance.FindMapDataByStageID(currentStageId);

        //Todo : 챕터에 따라 분기
        //씬 이름 수정해야함
        await SceneManager.LoadSceneAsync("Chapter1_StageScene");
    }

    public void CloseButton()
    {
        currentStageId = null;
        gameObject.SetActive(false);
    }
}