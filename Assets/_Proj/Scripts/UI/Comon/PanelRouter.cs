using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PanelRouter : MonoBehaviour
{
    [Header("Toggle Panel")]
    [SerializeField] GameObject profilePanel;
    [SerializeField] GameObject dexPanel;
    [SerializeField] GameObject questPanel;
    [SerializeField] GameObject shopPanel;
    [SerializeField] GameObject friendsPanel;
    [SerializeField] GameObject optionPanel;
    [SerializeField] GameObject chapterPanel;
    [SerializeField] GameObject stagePanel;

    [Header("Option")]
    [SerializeField] bool startClosed = true;
    public GameObject dimOverlay;
    public GameObject selectStageDimOverlay;
    [SerializeField] Sprite chapterSelectSprite;

    private StageIdInformation stageIdInformation;
    GameObject current;

    void Awake()
    {
        if (startClosed) CloseAll();
        SyncDim();
    }
    void OnEnable()
    {
        if (typeof(EditModeManager) != null)
        {
            EditModeManager.OnEnter += CloseAll;
        }
    }
    void OnDisable()
    {
        if (typeof(EditModeManager) != null)
        { 
            EditModeManager.OnEnter -= CloseAll;
        }
    }
    public void ToggleProfile() => Toggle(profilePanel);
    public void ToggleDex() => Toggle(dexPanel);
    public void ToggleQuest() => Toggle(questPanel);
    public void ToggleShop() => Toggle(shopPanel);
    public void ToggleFriends() => Toggle(friendsPanel);
    public void ToggleOption() => Toggle(optionPanel);
    public void ToggleChapter()
    {
        Toggle(chapterPanel);
        if (chapterPanel)
        {
            var img = dimOverlay.GetComponent<Image>();
            img.color = Color.white;
            img.sprite = chapterSelectSprite;
        }
        if (selectStageDimOverlay)
            selectStageDimOverlay.SetActive(false);
    }
    public void ToggleStage(string chapterId = null)
    {
        Toggle(stagePanel);
        if (stagePanel)
        {
            var bg = DataManager.Instance.Chapter.GetChapterBgIcon(chapterId);
            if (bg != null)
            {
                var img = dimOverlay.GetComponent<Image>();
                img.color = Color.white;
                img.sprite = bg;
            }

            // StageInfo에 chapterId 전달
            var stageInfo = stagePanel.GetComponentInChildren<StageInfo>();
            stageInfo.ShowStages(chapterId);
            Debug.Log($"[PanelRouter] ToggleStage 호출됨 — chapterId:{chapterId}");
        }
    }
    public void CloseAll()
    {
        if (profilePanel) profilePanel.SetActive(false);
        if (dexPanel) dexPanel.SetActive(false);
        if (questPanel) questPanel.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false);
        if (friendsPanel) friendsPanel.SetActive(false);
        if (optionPanel) optionPanel.SetActive(false);
        if (chapterPanel) chapterPanel.SetActive(false);
        if (stagePanel) stagePanel.SetActive(false);
        current = null;
        SyncDim();
    }

    public void CloseCurrent()
    {
        if (!current) return;
        //LSH 추가 1125
        if (current != shopPanel) CloseButton();
        //
        current.SetActive(false);
        current = null;
        SyncDim();
    }

    public void CloseStage()
    {
        if (!stagePanel) return;
        stagePanel.SetActive(false);

        if (chapterPanel)
        { 
            chapterPanel.SetActive(true);
            var img = dimOverlay.GetComponent<Image>();
            img.color = Color.white;
            img.sprite = chapterSelectSprite;
        }

        current = chapterPanel;
    }

    public void SelectDimClose()
    {
        selectStageDimOverlay.SetActive(false);
        GameObject stageDetail = GameObject.Find("StageDetailPanel(Clone)");
        stageDetail.SetActive(false);
    }

    void Toggle(GameObject target)
    {
        if (!target) return;

        //LSH 추가 1125
        OpenButton();
        //
        if (current == target)
        {
            target.SetActive(false);
            current = null;
            SyncDim();
            return;
        }

        if (current) current.SetActive(false);
        target.SetActive(true);
        current = target;
        SyncDim();
    }

    void SyncDim()
    {
        if (!dimOverlay) return;
        dimOverlay.SetActive(current != null);
        dimOverlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
        dimOverlay.GetComponent<Image>().sprite = null;
    }


    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {

            if (optionPanel && optionPanel.activeSelf) optionPanel.GetComponent<OptionPanelController>().HandleEscapeKey();
            else Toggle(optionPanel);
        }
    }

    //LSH 추가 1121
    public void EnterChapterPanel()
    {
        AudioManager.Instance.EnterChapterPanel();
        ETCEvent.InvokeSaveAnimalPos();
    }
    public void ReturnToMain()
    {
        AudioManager.Instance.ExitChapterOrStagePanel();
    }
    public void OpenButton()
    {
        AudioEvents.Raise(UIKey.Normal, 0);
    }
    public void CloseButton()
    {
        AudioEvents.Raise(UIKey.Normal, 1);
    }
    public void SelectButton()
    {
        AudioEvents.Raise(UIKey.Normal, 2);
    }
}
