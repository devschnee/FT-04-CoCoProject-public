using System;
using UnityEngine;
using UnityEngine.Splines;
using static UnityEngine.ParticleSystem;

public class Treasure : MonoBehaviour
{
    public int treasureIndex;
    public string treaureBlockName;
    public GameObject particle;
    public GameObject artifactParticleSystem;

    private string treasureId;
    private bool isCollected = false;
    private SpriteRenderer sprite;

    void Start()
    {
        var progress = UserData.Local.progress.scores.TryGetValue(StageUIManager.Instance.stageManager.currentStageId, out var value) ? value : new();

        sprite = GetComponentInChildren<SpriteRenderer>();
        // 이미 먹은 보물은 회색 표시
        if (treasureIndex == 0 && progress.star_1_rewarded)
        {
            // 시각적 표시
            sprite.color = Color.gray;
            //isCollected = true; // 다시 못먹게
        }
        if (treasureIndex == 1 && progress.star_2_rewarded)
        {
            sprite.color = Color.gray;
        }
        if (treasureIndex == 2 && progress.star_3_rewarded)
        {
            sprite.color = Color.gray;
        }
        //보물 data에서 view_codex_id로 codexData 접근해서 이미지 가져오기-보류, 파티클색깔 편하게 변경
        var treasureData = DataManager.Instance.Treasure.GetData(treasureId);
        if (treasureData.treasureType == TreasureType.artifact)
        {
            //var data = DataManager.Instance.Codex.GetCodexIcon(treasureData.view_codex_id);
            //sprite.sprite = data;
            particle.SetActive(false);
            artifactParticleSystem.SetActive(true);
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

            Joystick joystick = FindAnyObjectByType<Joystick>();
            if (joystick != null)
            {
                // KHJ - Treasure Panel이 켜졌으니 조이스틱 입력 잠금
                joystick.IsLocked = true;
            }
            // 플레이어 이동 막기
            other.GetComponent<PlayerMovement>().enabled = false;

            // 확인 버튼 클릭 시 호출되도록 이벤트 등록
            StageUIManager.Instance.OnTreasureConfirm = () => OnQuitAction(() =>
            {
                
                StageUIManager.Instance.stageManager.OnTreasureCollected(treasureIndex);
                StageUIManager.Instance.TreasurePanel.SetActive(false);
                StageUIManager.Instance.OptionOpenButton.gameObject.SetActive(true);
                joystick.IsLocked = false;
                other.GetComponent<PlayerMovement>().enabled = true;
                sprite.color = new Color(1, 1, 1, 0);
                particle.SetActive(false);
                StageUIManager.Instance.stageGetTreasureCount++;
            });
        }
    }

    private static void TreasureUI(TreasureData data)
    {
        var transData = DataManager.Instance.Codex.GetData(data.view_codex_id);
        StageUIManager.Instance.TreasureName.text = transData.codex_name;
        StageUIManager.Instance.TreasureDesc.text = transData.codex_lore;
        StageUIManager.Instance.TreasureImage.sprite = DataManager.Instance.Codex.GetCodexIcon(data.view_codex_id);
        
        var typeTMP = StageUIManager.Instance.TreasureType;
        var countTMP = StageUIManager.Instance.TreasureCount;
        switch (data.treasureType)
        {
            case TreasureType.coin:
            case TreasureType.cap:
                typeTMP.text = "수량";
                typeTMP.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                countTMP.text = data.count.ToString();
                break;
            case TreasureType.deco:
                typeTMP.text = "조경";
                typeTMP.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                countTMP.text = data.count.ToString();
                break;
            case TreasureType.costume:
                typeTMP.text = "치장";
                typeTMP.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                countTMP.text = data.count.ToString();
                break;
            case TreasureType.artifact:
                typeTMP.text = "유물";
                typeTMP.alignment = TMPro.TextAlignmentOptions.Center;
                countTMP.text = "";
                break;
        }
        StageUIManager.Instance.CocoDoogyDesc.text = data.coco_coment;
    }

    public void OnQuitAction(Action action)
    {
        action?.Invoke();
    }
}
