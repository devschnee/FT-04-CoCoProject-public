using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 탭 컨트롤러
/// - 상위: 거점 / 외형 / 배치
/// - 하위(거점): 거점(Home) / 배경(Background)
/// - 하위(외형): 모자/옷/신발/꼬리 (보류: 패널만 표시)
/// - 하위(배치): 동물(Animal) / 조경물(Deco)
/// 
/// 초기에: 편집모드 진입 시 인벤토리 열려 있고, "거점" 상위 + "거점(Home)" 하위가 기본으로 열림.
/// </summary>
public class InventoryTabsController : MonoBehaviour
{
    // === 상위 탭 버튼 ===
    [Header("Top Tabs Buttons")]
    [SerializeField] private Button topBaseCampBtn;
    [SerializeField] private Button topAppearanceBtn;
    [SerializeField] private Button topPlacementBtn;

    // === 상위 탭별 하위 탭 컨테이너(패널 그룹) ===
    [Header("Top Tab Containers")]
    [SerializeField] private GameObject baseCampSubTabs;   // 거점 하위(거점/배경)
    [SerializeField] private GameObject appearanceSubTabs; // 외형 하위(모자/옷/신발/꼬리)
    [SerializeField] private GameObject placementSubTabs;  // 배치 하위(동물/조경물)

    // === 하위 탭 버튼들 ===
    [Header("BaseCamp Sub Buttons")]
    [SerializeField] private Button baseHomeBtn;        // 거점
    [SerializeField] private Button baseBackgroundBtn;  // 배경

    [Header("Appearance Sub Buttons (보류)")]
    [SerializeField] private Button appHatBtn;
    [SerializeField] private Button appClothBtn;
    [SerializeField] private Button appShoesBtn;
    [SerializeField] private Button appTailBtn;

    [Header("Placement Sub Buttons")]
    [SerializeField] private Button placementAnimalBtn; // 동물
    [SerializeField] private Button placementDecoBtn;   // 조경물

    // === 실제 인벤토리 패널들 ===
    [Header("Inventory Panels (Assign)")]
    [SerializeField] private GameObject homePanel;        // IInventoryPanel 구현 스크립트가 같은 오브젝트나 자식에 붙어있어야 함
    [SerializeField] private GameObject backgroundPanel;
    [SerializeField] private GameObject animalPanel;
    [SerializeField] private GameObject decoPanel;
    [SerializeField] private GameObject appearancePlaceholderPanel; // 외형(보류)용 안내/빈패널

    private enum TopTab { BaseCamp, Appearance, Placement }
    private TopTab currentTop;

    private void Awake()
    {
        // 상위 탭 버튼
        SafeWire(topBaseCampBtn, () => OpenTop(TopTab.BaseCamp));
        SafeWire(topAppearanceBtn, () => OpenTop(TopTab.Appearance));
        SafeWire(topPlacementBtn, () => OpenTop(TopTab.Placement));

        // 하위(거점)
        SafeWire(baseHomeBtn, () => OpenBaseCampSub(home: true));
        SafeWire(baseBackgroundBtn, () => OpenBaseCampSub(home: false));

        // 하위(외형) - 동작 보류: 패널만 켜짐
        SafeWire(appHatBtn, () => OpenAppearanceSub());
        SafeWire(appClothBtn, () => OpenAppearanceSub());
        SafeWire(appShoesBtn, () => OpenAppearanceSub());
        SafeWire(appTailBtn, () => OpenAppearanceSub());

        // 하위(배치)
        SafeWire(placementAnimalBtn, () => OpenPlacementSub(animal: true));
        SafeWire(placementDecoBtn, () => OpenPlacementSub(animal: false));
    }

    private void OnEnable()
    {
        // 편집모드 진입 시 기본: 거점(상위) + 거점(Home 하위)
        OpenTop(TopTab.BaseCamp);
        OpenBaseCampSub(home: true);
    }

    private static void SafeWire(Button b, System.Action cb)
    {
        if (!b) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(() => cb());
    }

    // ─────────────────────────────────────────────
    // 상위 탭 열기
    // ─────────────────────────────────────────────
    private void OpenTop(TopTab tab)
    {
        currentTop = tab;

        // 하위 탭 컨테이너 표시
        SetActive(baseCampSubTabs, tab == TopTab.BaseCamp);
        SetActive(appearanceSubTabs, tab == TopTab.Appearance);
        SetActive(placementSubTabs, tab == TopTab.Placement);

        // 컨텐츠 패널은 하위 탭에서 고름 (여기선 일단 모두 꺼두기)
        SetAllContentOff();

        // 기본 하위 탭 자동 선택
        switch (tab)
        {
            case TopTab.BaseCamp:
                OpenBaseCampSub(home: true);
                break;
            case TopTab.Appearance:
                OpenAppearanceSub();
                break;
            case TopTab.Placement:
                OpenPlacementSub(animal: true);
                break;
        }
    }

    private void SetAllContentOff()
    {
        SetActive(homePanel, false);
        SetActive(backgroundPanel, false);
        SetActive(animalPanel, false);
        SetActive(decoPanel, false);
        SetActive(appearancePlaceholderPanel, false);
    }

    private static void SetActive(GameObject go, bool on)
    {
        if (go && go.activeSelf != on) go.SetActive(on);
    }

    // 공통: 활성화된 패널에서 IInventoryPanel 구현을 찾아 Rebuild 호출
    private static void RebuildPanel(GameObject panelGO)
    {
        if (!panelGO) return;

        // 같은 오브젝트에서 먼저 찾고, 없으면 자식에서 찾는다.
        if (!panelGO.TryGetComponent<IInventoryPanel>(out var p))
            p = panelGO.GetComponentInChildren<IInventoryPanel>(includeInactive: true);

        p?.Rebuild();
    }

    // ─────────────────────────────────────────────
    // 하위: 거점( Home / Background )
    // ─────────────────────────────────────────────
    private void OpenBaseCampSub(bool home)
    {
        // 컨텐츠 패널 선택
        SetAllContentOff();
        if (home)
        {
            SetActive(homePanel, true);
            RebuildPanel(homePanel);
        }
        else
        {
            SetActive(backgroundPanel, true);
            RebuildPanel(backgroundPanel);
        }
    }

    // ─────────────────────────────────────────────
    // 하위: 외형(보류) - 패널만
    // ─────────────────────────────────────────────
    private void OpenAppearanceSub()
    {
        SetAllContentOff();
        SetActive(appearancePlaceholderPanel, true);
        // 외형 보류: 별도 Rebuild 없음
    }

    // ─────────────────────────────────────────────
    // 하위: 배치( Animal / Deco )
    // ─────────────────────────────────────────────
    private void OpenPlacementSub(bool animal)
    {
        SetAllContentOff();
        if (animal)
        {
            SetActive(animalPanel, true);
            RebuildPanel(animalPanel);
        }
        else
        {
            SetActive(decoPanel, true);
            RebuildPanel(decoPanel);
        }
    }
}
