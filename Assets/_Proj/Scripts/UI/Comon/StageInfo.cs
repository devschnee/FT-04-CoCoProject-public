using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageInfo : MonoBehaviour
{
    public GameObject StagePrefab;          // 버튼 프리팹
    public GameObject StageDetailPrefab;    // 상세창 프리팹
    public GameObject SelectStageDimOverlay;
    public Transform stageParent;           // StagePrefab 붙일 위치
    public Transform detailParent;          // StageDetailPrefab 붙일 위치

    [Header("Treasure Icon Sprites")]
    public Sprite collectedSprite;    // 로비 전용
    public Sprite notCollectedSprite; // 로비 전용

    [Header("Star UI")]
    public TextMeshProUGUI starText;

    private List<GameObject> activeDetails = new List<GameObject>();
    private string currentChapterId;
    private int maxStar;
    private int myStar;
    
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
        //LSH 추가 1125
        AudioManager.Instance.EnterChapterPanel();
        //
        currentChapterId = chapterId; // 챕터 기억
        Debug.Log($"[StageInfo] ShowStages 호출됨 — chapterId:{chapterId}");
        RefreshUI();
    }

    private void RefreshUI()
    {
        Debug.Log($"ppm.instance ?? {PlayerProgressManager.Instance == null}");
        if (PlayerProgressManager.Instance == null) return;

        // 최신 데이터 강제 동기화
        PlayerProgressManager.Instance.LoadProgress();
        

        Debug.Log($"string null ?? {currentChapterId.IsNullOrEmpty()}");
        if (string.IsNullOrEmpty(currentChapterId)) return;

        myStar = 0;
        var chapter = DataManager.Instance.Chapter.GetData(currentChapterId);

        foreach (var stageId in chapter.chapter_staglist)
        {
            var stageProgress = PlayerProgressManager.Instance.GetStageProgress(stageId);
            myStar += stageProgress.GetCollectedCount();
        }

        maxStar = chapter.chapter_staglist.Length * 3;

        if (starText != null)
            starText.text = $"{myStar} / {maxStar}";

        ClearStages();
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

        // 이전 스테이지 조건 확인 추가
        var chapter = DataManager.Instance.Chapter.GetData(currentChapterId);
        bool canEnter = true;

        if (chapter != null && chapter.chapter_staglist != null)
        {
            // 현재 스테이지의 인덱스 찾기
            int stageIndex = Array.IndexOf(chapter.chapter_staglist, id);

            // UI 반영
            var text = stageObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                switch (chapter.chapter_id)
                {
                    case "chapter_1": text.text = $"1-{stageIndex + 1}"; break;
                    case "chapter_2": text.text = $"2-{stageIndex + 1}"; break;
                    case "chapter_3": text.text = $"3-{stageIndex + 1}"; break;
                    default: text.text = $"0-{stageIndex + 1}"; break;
                }
            }

            if (stageIndex > 0) // 첫 번째 스테이지가 아니면 이전 스테이지 조건 검사
            {
                string prevStageId = chapter.chapter_staglist[stageIndex - 1];
                var prevProgress = PlayerProgressManager.Instance.GetStageProgress(prevStageId);

                // 이전 스테이지에서 보물 하나 이상 먹었는지 확인
                canEnter = prevProgress.bestTreasureCount >= 0;
            }
            else if (stageIndex == 0)
            {
                // 첫 스테이지는 항상 입장 가능
                canEnter = true;
            }
            else
            {
                // 못 찾았을 경우 잠금 처리
                canEnter = false;
            }
        }

        // 보물 아이콘 그룹
        Transform treasureGroup = stageObj.transform.Find("TreasureGroup");
        if (treasureGroup)
        {
            int collectedCount = progress.GetCollectedCount();

            for (int i = 0; i < 3; i++)
            {
                var icon = treasureGroup.GetChild(i).GetComponent<Image>();
                icon.sprite = i < collectedCount ? collectedSprite : notCollectedSprite;
                if(!canEnter)
                {
                    icon.color = new Color(1, 1, 1, .5f);
                }
            }
        }

        // 클릭 이벤트
        var btn = stageObj.GetComponentInChildren<Button>();
        btn.interactable = canEnter;
        // LSH 추가 1125
        btn.onClick.AddListener(() => {ShowStageDetail(data.stage_id); AudioEvents.Raise(UIKey.Normal, 2);});
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

        SelectStageDimOverlay.SetActive(true);
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