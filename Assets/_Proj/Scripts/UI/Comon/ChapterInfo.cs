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
            imgs[0].sprite = null;
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
            chapterButton.onClick.AddListener(() => OpenStage(data.chapter_id));
        }
    }

    void OpenStage(string chapterId)
    {
        PanelRouter pr = GetComponentInParent<PanelRouter>();
        pr.ToggleStage(chapterId);
    }
}
