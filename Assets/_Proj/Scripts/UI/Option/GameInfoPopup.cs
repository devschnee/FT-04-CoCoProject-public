using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameInfoPopup : MonoBehaviour
{
    [Header("UI Targets")]
    [SerializeField] private TextMeshProUGUI manualName; // 소제목
    [SerializeField] private Image contentImage; // 이미지
    [SerializeField] private TextMeshProUGUI manualDesc; // 설명

    [Header("Tabs")]
    [SerializeField] private Transform tabGroup;
    [Tooltip("TabGroup 자식으로 생기게 될 Tab 프리팹")]
    [SerializeField] private Button tabButtonPrefab;

    [SerializeField] GameObject dim;

    private readonly List<Button> createdTabs = new(); // 생성된 버튼 저장
    // 현재 선택된 탭 수동 저장
    private int currManualId = -1;

    void Start()
    {
        BuildTabsFromCSV();
        Open();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        if (createdTabs.Count > 0)
        {
            // 기본 탭으로 초기화
            LoadManual(130001);
        }
        dim.gameObject.SetActive(true);
    }

    void BuildTabsFromCSV()
    {
        var manualList = DataManager.Instance.Manual.AllData;

        foreach (Transform child in tabGroup)
            Destroy(child.gameObject);

        createdTabs.Clear();

        foreach (var manual in manualList)
        {
            Button tab = Instantiate(tabButtonPrefab, tabGroup);
            createdTabs.Add(tab);

            // 탭에 들어갈 때는 따옴표 제거
            string tabName = manual.manual_name.Trim('"');
            tab.GetComponentInChildren<TextMeshProUGUI>().text = tabName;
            int id = manual.manual_id;

            tab.onClick.AddListener(() =>
            {
                LoadManual(id);
            });
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
        dim.gameObject.SetActive(false);
    }

    private void LoadManual(int manualId)
    {
        currManualId = manualId;

        // CSV 데이터 가져오기
        ManualData data = DataManager.Instance.Manual.GetData(manualId);
        if (data == null)
        {
            Debug.LogError($"[GameInfoPopup] 메뉴얼 ID {manualId} 를 찾을 수 없음.");
            return;
        }

        manualName.text = data.manual_name;
        //tabName.text = data.manual_name;

        // 이미지
        Sprite sprite = DataManager.Instance.Manual.GetIcon(manualId);
        if (sprite != null)
        {
            contentImage.sprite = sprite;
            contentImage.enabled = true;
        }
        else
        {
            contentImage.enabled = false; // 이미지 없는 경우 숨김
            Debug.Log("[GameInfoPopup] 이미지 없어서 숨김 처리");
        }

        // 설명 (개행 처리)
        manualDesc.text = data.manual_desc.Replace("\\n", "\n");

        // 탭 강조
        HighlightTab(manualId);
    }

    private void HighlightTab(int id)
    {
        Color selected = new Color(1f, 0.8f, 0.3f);
        Color normal = new Color(1f, 1f, 1f);

        for (int i = 0; i < createdTabs.Count; i++)
        {
            var data = DataManager.Instance.Manual.AllData[i];
            createdTabs[i].GetComponent<Image>().color =
                (data.manual_id == currManualId) ? selected : normal;
        }
    }
}
