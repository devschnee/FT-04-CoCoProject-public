using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Treasure : MonoBehaviour
{
    public int treasureIndex;
    public string treaureBlockName;
    public GameObject particle;
    public GameObject artifactParticleSystem;

    private string treasureId;
    private bool isCollected = false;
    private SpriteRenderer sprite;
    private int _glowColorID;
    private int _baseColorID;

    // KHJ - NOTE : 기존에는 aftifact타입이면 무조건 particle을 켜주게 되어 있었으나 기획팀 요청으로 파티클은 모두 끄고 sprite의 Color(Shader)를 변경해야 해서 전체적으로 수정함
    void Start()
    {
        _baseColorID = Shader.PropertyToID("_BaseColor");
        _glowColorID = Shader.PropertyToID("_GlowColor");

        var progress = UserData.Local.progress.scores.TryGetValue(StageUIManager.Instance.stageManager.currentStageId, out var value) ? value : new();
        
        sprite = GetComponentInChildren<SpriteRenderer>();

        // 획득 상태 확인 및 처리
        bool isAlreadyCollected = (treasureIndex == 0 && progress.star_1_rewarded) ||
                                  (treasureIndex == 1 && progress.star_2_rewarded) ||
                                  (treasureIndex == 2 && progress.star_3_rewarded);

        if (isAlreadyCollected)
        {
            // 획득 시 시각 효과(회색 처리, 파티클 끄기)
            SetCollectedVisuals();
        }

        // 보물 타입별 초기화
        var treasureData = DataManager.Instance.Treasure.GetData(treasureId);

        // 아티팩트 파티클은 '획득하지 않은 상태'일 때만 활성화해야 함.
        if (treasureData.treasureType == TreasureType.artifact)
        {
            // 획득하지 않았을 때만 artifactParticleSystem을 켜고, 일반 파티클은 끄기
            if (!isAlreadyCollected)
            {
                particle.SetActive(false);
                artifactParticleSystem.SetActive(true);
            }
            else
            {
                // 이미 획득했다면 SetCollectedVisuals에서 처리하지만, 혹시 모를 경우를 대비하여 모두 꺼주기
                particle.SetActive(false);
                artifactParticleSystem.SetActive(false);
            }
        }
        // artifact가 아닌 다른 타입의 보물은 일반 particle이 켜져 있어야 하므로 별도로 처리X
    }

    private void SetCollectedVisuals()
    {
        // 파티클 끄기
        if (particle != null) particle.SetActive(false);
        if (artifactParticleSystem != null) artifactParticleSystem.SetActive(false);

        // 셰이더 색상 변경
        if (sprite != null)
        {
            //sprite.material.SetColor(_baseColorID, new Color(0.5f, 0.5f, 0.5f, 1f));
            sprite.material.SetColor(_glowColorID, new Color(0.6f, 0.6f, 0.6f, 1f)); 
        }
         isCollected = true;
    }

    public void Init(string id)
    {
        treasureId = id;

        Debug.Log($"[Treasure] Init 완료 → ID: {treasureId}");
    }

    void OnTriggerEnter(Collider other)
    {
        if (StageUIManager.Instance.stageManager.isTest) return;

        // KHJ - 이미 먹은 보물의 패널 분기
        if (isCollected)
        {
            if (other.CompareTag("Player"))
            {
                StartCoroutine(CollectedTreasureAS(other));
            }
        }
        else
        {
            if (other.CompareTag("Player"))
            {
                isCollected = true;
                // LSH 추가 1128
                AudioEvents.Raise(UIKey.Stage, 4);
                StageUIManager.Instance.TreasurePanel.SetActive(true);
                StageUIManager.Instance.OptionOpenButton.gameObject.SetActive(false);
                StageUIManager.Instance.CocoDoogyImage.sprite = StageUIManager.Instance.CoCoDoogySprite;

                var data = DataManager.Instance.Treasure.GetData(treasureId);

                switch (data.treasureType)
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
                    //sprite.color = new Color(1, 1, 1, 0);
                    this.gameObject.SetActive(false);
                    //sprite.gameObject.SetActive(false);
                    //particle.SetActive(false);
                    //artifactParticleSystem.SetActive(false);
                    StageUIManager.Instance.stageGetTreasureCount++;
                    StageUIManager.Instance.CollecTreausreCountText.text = $"{StageUIManager.Instance.stageGetTreasureCount} / 3";
                });
            }
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

    // KHJ - 이미 획득한 보물은 팝업창 n초 후 자동으로 꺼지도록
    IEnumerator CollectedTreasureAS(Collider other)
    {
        AudioEvents.Raise(UIKey.Stage, 4);
        StageUIManager.Instance.TreasureCollectedPanel.SetActive(true);
        StageUIManager.Instance.OptionOpenButton.gameObject.SetActive(false);

        Joystick joystick = FindAnyObjectByType<Joystick>();
        if (joystick != null)
        {
            joystick.IsLocked = true;
        }
        // 플레이어 이동 막기
        other.GetComponent<PlayerMovement>().enabled = false;

        yield return new WaitForSeconds(1.5f);

        StageUIManager.Instance.stageManager.OnTreasureCollected(treasureIndex);
        StageUIManager.Instance.TreasureCollectedPanel.SetActive(false);
        StageUIManager.Instance.OptionOpenButton.gameObject.SetActive(true);
        joystick.IsLocked = false;
        other.GetComponent<PlayerMovement>().enabled = true;
        //sprite.color = new Color(1, 1, 1, 0);
        gameObject.SetActive(false);
        //sprite.gameObject.SetActive(false);
        //particle.SetActive(false);
        //artifactParticleSystem.SetActive(false);
        StageUIManager.Instance.stageGetTreasureCount++;
        StageUIManager.Instance.CollecTreausreCountText.text = $"{StageUIManager.Instance.stageGetTreasureCount} / 3";
    }
}
