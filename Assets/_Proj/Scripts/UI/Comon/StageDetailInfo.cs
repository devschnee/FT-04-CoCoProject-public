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

        // TODO: 보상 표시 (treasure_01_id ~ 03_id)
        // rewardGroup 아래 동적 생성 가능
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
}