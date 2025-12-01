using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterInfo : MonoBehaviour
{
    public GameObject ChapterPrefab;
    void Awake()
    {
        var chapters = DataManager.Instance.Chapter.GetAllChapters();

        foreach (var chapter in chapters)
        {
            CreateChapterUI(chapter);
        }
    }

    void CreateChapterUI(ChapterData data)
    {
        GameObject chapterInstance = Instantiate(ChapterPrefab, transform);

        // 이미지 설정
        Image[] imgs = chapterInstance.GetComponentsInChildren<Image>(true);
        if (imgs.Length > 1)
        {
            //imgs[0].sprite = DataManager.Instance.Chapter.GetChapterBgIcon(data.chapter_id);
            imgs[1].sprite = DataManager.Instance.Chapter.GetChapterIcon(data.chapter_id);
        }

        // 텍스트 설정
        TextMeshProUGUI[] texts = chapterInstance.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (texts.Length >= 2)
        {
            texts[0].text = data.chapter_name;
            texts[1].text = data.chapter_desc;
        }

        // 버튼 설정
        Button chapterButton = chapterInstance.GetComponent<Button>();
        if (chapterButton != null)
        {
            //if (UserData.Local.passedTutorials >= 3)
            //{
            //NOTE: 조건문 제거: passedTutorials가 3 미만인 상황에서 로비로 올 수가 없도록 처리되어 있습니다.
            //위의 조건문을 남길 경우(원래는 UserData.Local.passedTutorials == 2였음) 스테이지 선택 패널이 열리지 않습니다.
                chapterButton.onClick.AddListener(() => OpenStage(data.chapter_id));
            //}
            if (!UserData.Local.progress.scores.ContainsKey($"stage_1_10"))
            {
                if(data.chapter_id == "chapter_2")
                  chapterButton.interactable = false; 
            }
            if (!UserData.Local.progress.scores.ContainsKey($"stage_2_10"))
            {
                if (data.chapter_id == "chapter_3")
                    chapterButton.interactable = false; 
            }
        }
    }

    void OpenStage(string chapterId)
    {
        PanelRouter pr = GetComponentInParent<PanelRouter>();
        pr.ToggleStage(chapterId);
    }
}
