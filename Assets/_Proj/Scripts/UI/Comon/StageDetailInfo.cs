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
        // 이미지
        if (stageImage != null)
            stageImage.sprite = DataManager.Instance.Stage.GetIcon(id);

        // 텍스트
        if (stageName != null) stageName.text = data.stage_name;
        if (stageDesc != null) stageDesc.text = data.stage_desc;

        // 보물 아이콘 상태 반영
        UpdateTreasureDisplay(data);
    }

    async void EnterStage()
    {
        var data = DataManager.Instance.Stage.GetData(currentStageId);

        string mapId = data.map_id;
        
        await FirebaseManager_FORTEST.Instance.Temp(mapId);

        //Todo : 챕터에 따라 분기
        //씬 이름 수정해야함
        await SceneManager.LoadSceneAsync("Chapter1_StageScene_TESTONLY");

    }

    public void CloseButton()
    {
        currentStageId = null;
        gameObject.SetActive(false);
    }

    void UpdateTreasureDisplay(StageData data)
    {
        // StageManager.Instance가 실행 중인 스테이지에서만 접근 가능
        // 로비 씬에서는 Firebase 데이터 기반으로도 가능

        if (StageUIManager.Instance.stageManager != null)
        {
            treasureIcons[0].sprite = StageUIManager.Instance.stageManager.IsTreasureCollected(data.treasure_01_id) ? collectedSprite : notCollectedSprite;
            treasureIcons[1].sprite = StageUIManager.Instance.stageManager.IsTreasureCollected(data.treasure_02_id) ? collectedSprite : notCollectedSprite;
            treasureIcons[2].sprite = StageUIManager.Instance.stageManager.IsTreasureCollected(data.treasure_03_id) ? collectedSprite : notCollectedSprite;
        }
        else
        {
            // 추후 저장된 데이터에서 불러오는 로직 추가 가능 (ex: Firebase / PlayerPrefs)
            treasureIcons[0].sprite = notCollectedSprite;
            treasureIcons[1].sprite = notCollectedSprite;
            treasureIcons[2].sprite = notCollectedSprite;
        }
    }
}