using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageInfo : MonoBehaviour
{
    public GameObject StagePrefab;          // 버튼 프리팹
    public GameObject StageDetailPrefab;    // 상세창 프리팹
    public Transform stageParent;           // StagePrefab 붙일 위치
    public Transform detailParent;          // StageDetailPrefab 붙일 위치

    [Header("Treasure Icon Sprites")]
    public Sprite collectedSprite;    // 로비 전용
    public Sprite notCollectedSprite; // 로비 전용

    private List<GameObject> activeDetails = new List<GameObject>();
    private string currentChapterId;
    
    void OnEnable()
    {
        PlayerProgressManager.OnProgressUpdated += RefreshUI;
        Debug.Log("[StageInfo] OnEnable → 이벤트 구독 완료");

        RefreshUI();
    }

    void OnDisable()
    {
        PlayerProgressManager.OnProgressUpdated -= RefreshUI;
    }

    public void ShowStages(string chapterId)
    {
        currentChapterId = chapterId; // 챕터 기억
        Debug.Log($"[StageInfo] ShowStages 호출됨 — chapterId:{chapterId}");
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (PlayerProgressManager.Instance == null) return;

        // 최신 데이터 강제 동기화
        PlayerProgressManager.Instance.LoadProgress();

        if (string.IsNullOrEmpty(currentChapterId)) return;

        ClearStages();

        var chapter = DataManager.Instance.Chapter.GetData(currentChapterId);
        if (chapter == null) return;

        foreach (var stageId in chapter.chapter_staglist)
        {
            var stageData = DataManager.Instance.Stage.GetData(stageId);
            if (stageData == null) continue;
            CreateStageButton(stageData.stage_id);
        }
    }

    void CreateStageButton(string id)
    {
        GameObject stageObj = Instantiate(StagePrefab, stageParent);

        var data = DataManager.Instance.Stage.GetData(id);
        var progress = PlayerProgressManager.Instance.GetStageProgress(id);

        Debug.Log($"[StageInfo] {id} 보물 진행도: " +
                  $"{progress.treasureCollected[0]}, {progress.treasureCollected[1]}, {progress.treasureCollected[2]}");

        // 텍스트
        var text = stageObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text) text.text = data.stage_name;

        // 보물 아이콘 그룹
        Transform treasureGroup = stageObj.transform.Find("TreasureGroup");
        if (treasureGroup)
        {
            int collectedCount = progress.GetCollectedCount();

            for (int i = 0; i < 3; i++)
            {
                var icon = treasureGroup.GetChild(i).GetComponent<Image>();
                icon.sprite = i < collectedCount ? collectedSprite : notCollectedSprite;
            }
        }

        // 클릭 이벤트
        Button btn = stageObj.GetComponentInChildren<Button>();
        btn.onClick.AddListener(() => ShowStageDetail(data.stage_id));
    }

    void ShowStageDetail(string id)
    {
        // 기존 상세창 제거
        foreach (var obj in activeDetails)
            Destroy(obj);
        activeDetails.Clear();

        // 새로운 상세창 Instantiate
        GameObject detailObj = Instantiate(StageDetailPrefab, detailParent);
        activeDetails.Add(detailObj);

        StageDetailInfo detail = detailObj.GetComponent<StageDetailInfo>();
        if (detail != null)
            detail.ShowDetail(id);
    }

    void ClearStages()
    {
        foreach (Transform child in stageParent)
            Destroy(child.gameObject);

        foreach (var obj in activeDetails)
            Destroy(obj);
        activeDetails.Clear();
    }
}