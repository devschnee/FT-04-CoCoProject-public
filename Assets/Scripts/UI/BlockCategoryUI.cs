using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class BlockCategoryUI : MonoBehaviour
{
    public GameObject blockButtonPrefab; // 버튼 프리팹
    public Transform blockAnimalGimick;
    public Transform blockListParent; // HorizontalLayoutGroup을 가진 오브젝트

    [Header("카테고리별 블록 목록")]
    public List<BlockListData> allCategories; // 블록, 동물, 기믹 등

    private List<GameObject> spawnedCategoryButtons = new List<GameObject>();
    private List<GameObject> spawnedButtons = new List<GameObject>();
    private List<BlockData> currentList; // 현재 표시 중인 블록 리스트

    private int currentPage = 0;
    private const int pageSize = 9;

    void Start()
    {
        CreateCategoryButtons(); // 시작 시 카테고리 버튼 자동 생성
    }

    void Update()
    {
        // 카테고리 전환
        // 단축키 블록 선택 (1~9)
        HandleCategoryHotkeys();
        HandleBlockHotkeys();
        HandlePageChange();
    }

    public void ShowBlocks(BlockListData category)
    {
        currentList = new List<BlockData>(category.blocks);
        currentPage = 0;

        RefreshPage();
    }

    private void RefreshPage()
    {
        ClearBlocks();

        if (currentList == null) return;

        int start = currentPage * pageSize;
        int end = Mathf.Min(start + pageSize, currentList.Count);

        for (int i = start; i < end; i++)
        {
            var data = currentList[i];

            var go = Instantiate(blockButtonPrefab, blockListParent);
            go.GetComponent<Image>().sprite = data.icon;
            go.GetComponent<Button>().onClick.AddListener(() => EditorManager.Instance.ChooseBlock(data));
            spawnedButtons.Add(go);

            // 숫자표시
            var keyText = go.transform.Find("KeyText")?.GetComponent<TextMeshProUGUI>();
            if (keyText != null)
                keyText.text = ((i - start) + 1).ToString();
        }

        EditorManager.Instance.EditorMode = EditorMode.Place;
    }
    public void ClearBlocks()
    {
        foreach (var btn in spawnedButtons)
            Destroy(btn);
        spawnedButtons.Clear();
        
    }

    private void CreateCategoryButtons()
    {
        foreach (var category in allCategories)
        {
            var go = Instantiate(blockButtonPrefab, blockAnimalGimick);
            var button = go.GetComponent<Button>();
            var image = go.GetComponent<Image>();
            image.sprite = category.icon;
            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            text.text = category.categoryName;

            spawnedCategoryButtons.Add(go);
        }
    }

    private void HandleCategoryHotkeys()
    {
        if (allCategories == null || allCategories.Count == 0) return;
        for (int i = 0; i < allCategories.Count; i++)
        {
            KeyCode funcKey = KeyCode.F1 + i;
            if (Input.GetKeyDown(funcKey))
            {
                ShowBlocks(allCategories[i]);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClearBlocks();
            EditorManager.Instance.EditorMode = EditorMode.Idle;
        }
    }

    private void HandleBlockHotkeys()
    {
        if (currentList == null || currentList.Count == 0) return;

        //에디터매니저의 모드가 배치모드가 아닐 경우 동작 안하도록 처리
        if (EditorManager.Instance.EditorMode != EditorMode.Place) return;

        int start = currentPage * pageSize;
        int end = Mathf.Min(start + pageSize, currentList.Count);

        for (int i = 0; i < end - start; i++)
        {
            // 숫자키 1~9 확인
            KeyCode key = KeyCode.Alpha1 + i;
            if (Input.GetKeyDown(key))
            {
                EditorManager.Instance.ChooseBlock(currentList[i + (currentPage * 9)]);
            }
        }
    }

    private void HandlePageChange()
    {
        if (currentList == null || currentList.Count <= pageSize) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            int maxPage = (currentList.Count - 1) / pageSize;
            currentPage++;

            if (currentPage > maxPage)
                currentPage = 0; // 다시 처음 페이지로

            RefreshPage();
        }
    }
}