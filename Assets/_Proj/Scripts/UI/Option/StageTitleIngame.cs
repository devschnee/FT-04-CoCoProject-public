using TMPro;
using UnityEngine;

public class StageTitleIngame : MonoBehaviour
{
    public TextMeshProUGUI stageNameTxt;
    private string currentStageId;

    void Awake()
    {
        currentStageId = FirebaseManager.Instance.selectStageID;
    }

    void Start()
    {
        StageUIManager.Instance.stageIdInformation.stageIdInfo = currentStageId;
        var data = DataManager.Instance.Stage.GetData(currentStageId);
        StageUIManager.Instance.stageName.text = data.stage_name;
        stageNameTxt.text = data.stage_name;
    }
}