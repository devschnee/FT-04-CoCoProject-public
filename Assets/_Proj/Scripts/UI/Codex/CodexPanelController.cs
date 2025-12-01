using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodexPanelController : MonoBehaviour
{
    [Header("Service")]
    [SerializeField] private CodexService codexService;

    [Header("Detail UI")]
    [SerializeField] private Image detailDisplay;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailDesc;
    [SerializeField] private ScrollRect detailScroll;
    //슬롯 레이블이 빠져 있었네요
    [SerializeField] private TMP_Text slotLabel;


    [Header("Grid UI")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private CodexItemSlot slotPrefab;
    [SerializeField] private TMP_Text pageLabel;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private List<CodexTypeButton> typeButtons;

    //12.01mj   
    [Header("Type Red Dots")]  // 🔴 카테고리 버튼 위 빨간점들
    [SerializeField] private GameObject animalRedDot;
    [SerializeField] private GameObject decoRedDot;
    [SerializeField] private GameObject costumeRedDot;
    [SerializeField] private GameObject artifactRedDot;
    [SerializeField] private GameObject homeRedDot;

    [Header("Settings")]
    [SerializeField, Min(1)] private int pageSize = 8;
    [SerializeField] private CodexType defaultType = CodexType.animal;

    private CodexType currentType;
    private List<CodexData> listBuffer = new List<CodexData>();
    private int pageIndex = 0;

    private void OnEnable()
    {
        UIPanelAnimator.Open(gameObject);
        StartCoroutine(DelayInit());

        //12.01mj   
        CodexRedDotManager.OnStateChanged += ApplyRedDots;
        ApplyRedDots(CodexRedDotManager.Current);
    }
    //12.01mj   
    private void OnDisable()
    {
        CodexRedDotManager.OnStateChanged -= ApplyRedDots;
    }
    private void ApplyRedDots(CodexRedDotState state)
    {
        if (animalRedDot) animalRedDot.SetActive(state.hasAnimal);
        if (decoRedDot) decoRedDot.SetActive(state.hasDeco);
        if (costumeRedDot) costumeRedDot.SetActive(state.hasCostume);
        if (artifactRedDot) artifactRedDot.SetActive(state.hasArtifact);
        if (homeRedDot) homeRedDot.SetActive(state.hasHome);
    }

    private void Close()
    {
        UIPanelAnimator.Close(gameObject);
    }

    private IEnumerator DelayInit()
    {
        yield return null;
        FindButtonsAndInit();
        SetType(defaultType);
    }

    //HACK: 강욱 - 1123: 버튼들을 Init(this)해주는 처리 누락되어 있었음.
    private void FindButtonsAndInit()
    {
        typeButtons.ForEach(x => x.Init(this));
    }

    public void OnTypeButtonClicked(CodexType type)
    {
        //LSH 추가 1125
        AudioEvents.Raise(UIKey.Normal, 2);
        //
        SetType(type);
    }

    private void SetType(CodexType type)
    {
        currentType = type;
        pageIndex = 0;

        string slotLabelText = "";
        
        switch (type)
        {
            case CodexType.deco: slotLabelText = "조경물";
                break;
            case CodexType.costume:
                slotLabelText = "치장품";
                break;
            case CodexType.animal:
                slotLabelText = "동물친구";
                break;
            case CodexType.home:
                slotLabelText = "집";
                break;
            case CodexType.artifact:
                slotLabelText = "유물";
                break;
        }
        slotLabel.text = slotLabelText;

        listBuffer = codexService.GetByType(type);
        HighlightTypeButtons(type);

        RebuildPage();

        // 첫 항목 상세 표시
        if (listBuffer.Count > 0)
        {
            CodexData first = listBuffer[0];
            if (codexService.IsUnlocked(type, first.item_id))
                SetDetail(first);
            else
                SetLockedDetail(first);
        }
        else
        {
            ClearDetail();
        }
    }

    private void HighlightTypeButtons(CodexType selected)
    {
        foreach (var btn in typeButtons)
            btn?.SetSelected(btn.GetCodexType() == selected);
    }

    private void RebuildPage()
    {
        // 슬롯 비우기
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        int total = listBuffer.Count;
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(total / (float)pageSize));
        pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);

        int start = pageIndex * pageSize;
        int end = Mathf.Min(start + pageSize, total);

        for (int i = start; i < end; i++)
        {
            CodexData data = listBuffer[i];

            var slot = Instantiate(slotPrefab, gridParent);
            slot.Bind(this, codexService, currentType, data);
        }

        // 페이지 버튼
        pageLabel.text = $"{pageIndex + 1}/{pageCount}";
        prevButton.interactable = (pageIndex > 0);
        nextButton.interactable = (pageIndex < pageCount - 1);
    }

    public void OnPrevPage()
    {
        pageIndex--;
        RebuildPage();
    }

    public void OnNextPage()
    {
        pageIndex++;
        RebuildPage();
    }

    // 슬롯 클릭 (해금됨)
    public void OnItemClicked(CodexType type, int itemId)
    {
        var entry = codexService.GetData(type, itemId);
        if (entry != null)
            SetDetail(entry);
        
        //TODO: 코덱스에서 빨간점 지우는 처리가 들어 있는 부분임.
        var hashset = UserData.Local.codex.newlyUnlocked[type.ToString().ToLower()];
        if (hashset.Contains(itemId)) hashset.Remove(itemId);
        UserData.Local.codex.Save();

        //12.01mj
        // 🔴 도감 읽었으니까 빨간점 재계산
        CodexRedDotManager.Recalculate();

        // 🔴 슬롯들에 있는 빨간점(해당 페이지)도 다시 구성
        RebuildPage();
    }

    // 슬롯 클릭 (잠금)
    public void OnLockedItemClicked(CodexType type, int itemId)
    {
        var entry = codexService.GetData(type, itemId);
        if (entry != null)
            SetLockedDetail(entry);
    }

    private void SetDetail(CodexData entry)
    {
        detailDisplay.sprite = codexService.GetDisplay(entry);
        detailDisplay.color = Color.white;

        detailName.text = entry.codex_name;
        detailDesc.text = entry.codex_lore;

        StartCoroutine(ResetScroll());
    }

    private void SetLockedDetail(CodexData entry)
    {
        detailDisplay.sprite = codexService.GetDisplay(entry);
        detailDisplay.color = new Color(0.4f, 0.4f, 0.4f, 1f);

        detailName.text = "???"/*entry.codex_name*/;
        detailDesc.text = "아직 해금되지 않은 항목입니다.";

        StartCoroutine(ResetScroll());
    }

    private IEnumerator ResetScroll()
    {
        yield return null;
        detailScroll.verticalNormalizedPosition = 1f;
    }

    private void ClearDetail()
    {
        detailDisplay.sprite = null;
        detailName.text = "";
        detailDesc.text = "";
    }
}
