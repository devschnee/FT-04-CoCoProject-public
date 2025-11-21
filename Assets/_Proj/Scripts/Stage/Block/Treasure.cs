using System;
using UnityEngine;

public class Treasure : MonoBehaviour
{
    public int treasureIndex;
    public string treaureBlockName;
    private string treasureId;
    private bool isCollected = false;
    void Start()
    {
        var progress = PlayerProgressManager.Instance.GetStageProgress(StageUIManager.Instance.stageManager.currentStageId);

        // 이미 먹은 보물은 회색 표시
        if (progress.treasureCollected[treasureIndex])
        {
            // 시각적 표시
            //GetComponent<Renderer>().material.color = Color.gray;
            //isCollected = true; // 다시 못먹게
        }
    }
    public void Init(string id)
    {
        treasureId = id;
        Debug.Log($"[Treasure] Init 완료 → ID: {treasureId}");

    }
    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        if (StageUIManager.Instance.stageManager.isTest) return;

        if (other.CompareTag("Player"))
        {
            isCollected = true;
            StageUIManager.Instance.TreasurePanel.SetActive(true);
            StageUIManager.Instance.OptionOpenButton.gameObject.SetActive(false);
            StageUIManager.Instance.CocoDoogyImage.sprite = StageUIManager.Instance.CoCoDoogySprite;

            var data = DataManager.Instance.Treasure.GetData(treasureId);

            switch(data.treasureType)
            {
                case TreasureType.deco:
                    TreasureUI(data);
                    break;
                case TreasureType.costume:
                    TreasureUI(data);
                    break;
                case TreasureType.artifact:
                    TreasureUI(data);
                    break;
                case TreasureType.coin:
                    TreasureUI(data);
                    break;
                case TreasureType.cap:
                    TreasureUI(data);
                    break;
            }


            // 플레이어 이동 막기
            other.GetComponent<PlayerMovement>().enabled = false;

            // 확인 버튼 클릭 시 호출되도록 이벤트 등록
            StageUIManager.Instance.OnTreasureConfirm = () => OnQuitAction(() =>
            {
                StageUIManager.Instance.stageManager.OnTreasureCollected(treasureIndex);
                StageUIManager.Instance.TreasurePanel.SetActive(false);
                StageUIManager.Instance.OptionOpenButton.gameObject.SetActive(true);
                other.GetComponent<PlayerMovement>().enabled = true;
            });
        }
    }

    private static void TreasureUI(TreasureData data)
    {
        var transData = DataManager.Instance.Codex.GetData(data.view_codex_id);
        StageUIManager.Instance.TreasureName.text = transData.codex_name;
        StageUIManager.Instance.TreasureDesc.text = transData.codex_lore;
        StageUIManager.Instance.TreasureImage.sprite = DataManager.Instance.Codex.GetCodexIcon(data.view_codex_id);
        StageUIManager.Instance.TreasureType.text = data.treasureType.ToString();
        StageUIManager.Instance.TreasureCount.text = data.count.ToString();
        StageUIManager.Instance.CocoDoogyDesc.text = data.coco_coment;
    }

    public void OnQuitAction(Action action)
    {
        action?.Invoke();
    }
}
