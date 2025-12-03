using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TouchES = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// EditModeController (Core)
/// - 인스펙터 설정
/// - 공용 필드 (다른 partial에서 씀)
/// - 모드 on/off는 다른 partial에 있음
/// - 저장/복원(Baseline)과 연계되는 공용 상태
/// - Home(집) 프리뷰/확정/취소 흐름의 공통 상태
/// </summary>
public partial class EditModeController : MonoBehaviour
{
    #region === Inspector ===

    [Header("Pick & Drag")]
    [SerializeField] private LayerMask draggableMask = ~0;

    [Header("Move Plane")]
    [SerializeField, Tooltip("드래그 시 Y를 처음 높이에 고정할지")]
    private bool lockYToInitial = true;
    [SerializeField, Tooltip("lockYToInitial = false 일 때 고정할 Y")]
    private float fixedY = 0f;

    [Header("Grid Snap")]
    [SerializeField] private bool snapToGrid = true;
    [SerializeField, Min(0.01f)] private float gridSize = 1f;
    [SerializeField, Tooltip("스냅 기준 원점")]
    private Vector3 gridOrigin = Vector3.zero;

    [Header("Undo")]
    [SerializeField, Tooltip("히스토리 최대 저장 개수 (-1: 무제한)")]
    private int undoMax = -1;

    [Header("Overlap")]
    [SerializeField, Tooltip("겹침 허용 오차")]
    private float overlapEpsilon = 0.0005f;

    [Header("UI (Toolbar/Undo)")]
    [SerializeField] private Button undoButton;
    [SerializeField] private ObjectActionToolbar actionToolbar;

    [Header("Edit Mode Entry (Long Press)")]
    [SerializeField, Tooltip("롱프레스 대상")]
    private Transform longPressTarget;
    [SerializeField, Tooltip("롱프레스 시간(초)")]
    private float longPressSeconds = 1.0f;
    [SerializeField, Tooltip("롱프레스 허용 이동량(px)")]
    private float longPressSlopPixels = 10f;

    [Header("Save/Back Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button backButton;

    [Header("Panels")]
    [SerializeField, Tooltip("편집 종료 확인 패널")]
    private GameObject exitConfirmPanel;
    [SerializeField] private Button exitYesButton;
    [SerializeField] private Button exitNoButton;
    [SerializeField, Tooltip("저장 완료 패널")]
    private GameObject savedInfoPanel;
    [SerializeField] private Button savedOkButton;

    [Header("Ground Limit")]
    [SerializeField, Tooltip("설치 가능한 바닥 레이어")]
    private LayerMask groundMask;
    [SerializeField, Tooltip("Ground 체크 위쪽 여유")]
    private float groundProbeUp = 3f;
    [SerializeField, Tooltip("Ground 체크 아래쪽 여유")]
    private float groundProbeDown = 6f;
    [SerializeField, Tooltip("무조건 Ground 위에서만 배치")]
    private bool requireGround = true;
    [SerializeField] private DecoDatabase decoDB;

    [Header("Main Character DB")]
    [SerializeField] private MainCharacterDatabase mainCharDB;

    [Header("Grid Overlay")]
    [SerializeField] private GridOverlay gridOverlay;   // ← 이것만 남기기

    private Transform homeCandidate;
    public static System.Action<bool> HomePreviewActiveChanged;
    public bool IsHomePreviewActive => homePreview != null;
    private bool homePreviewConfirmed = false;

    // LSH 추가 1203
    public event Action OnChangedLobby;
    #endregion


    #region === Public State ===

    /// <summary>현재 편집모드 여부</summary>
    public bool IsEditMode { get; private set; }

    /// <summary>현재 선택/드래그 중인 오브젝트</summary>
    public Transform CurrentTarget { get; private set; }

    /// <summary>카메라(QuarterView)가 회전하면 안 되는 상태일 때 true</summary>
    public static bool BlockOrbit;

    #endregion


    #region === Shared State (다른 partial에서 씀) ===

    // 카메라
    private Camera cam;

    // 포인터/드래그 상태
    private bool pointerDown;
    private Vector2 pressScreenPos;
    private Transform pressedHitTarget;
    private bool isDragging;
    private bool movedDuringDrag;
    private bool currentPlacementValid = true;
    private bool startedOnDraggable;

    // 드래그용 평면
    private Plane movePlane;
    private float movePlaneY;
    private bool movePlaneReady;

    // Undo 히스토리 (오브젝트별)
    private struct Snap { public Vector3 pos; public Quaternion rot; }
    private readonly Dictionary<Transform, Stack<Snap>> history = new();

    // 롱프레스
    private bool longPressArmed;
    private float longPressTimer;
    private Vector2 longPressStartPos;

    // 저장/복원용 베이스라인
    private bool hasUnsavedChanges;
    private readonly List<ObjSnapshot> baseline = new();
    private readonly List<InventorySnapshot> invBaseline = new();

    // 인벤에서 막 꺼낸 오브젝트
    private Transform pendingFromInventory;

    // === Home 전용 상태 ===
    private Transform homePrev;     // 현재 씬의 "확정된" 집
    private int homePrevId;    // 확정 집 id
    private Transform homePreview;  // 인벤에서 고른 새 집(프리뷰, OK/Cancel 대기)
    private bool _homeSwapBusy; // 프리뷰 중복 생성 방지

    // 공용 로더 캐시
    private ResourcesLoader _loader;

    // 동물 슬롯 토글 이벤트
    public static Action<int> AnimalTakenFromInventory;   // 동물을 꺼냈을 때(슬롯 숨김)
    public static Action<int> AnimalReturnedToInventory;  // 동물을 되돌렸을 때(슬롯 표시)

    // baseline에 존재했던 Transform 집합(InstanceID로 추적)
    private readonly HashSet<int> baselineIds = new();

    #endregion


    #region === Snapshot Structs ===

    private struct ObjSnapshot
    {
        public Transform t;
        public Vector3 pos;
        public Quaternion rot;
        public bool activeSelf;
    }

    [Serializable]
    public struct InventorySnapshot
    {
        public int id;
        public int count;
    }

    #endregion


    #region === Unity Lifecycle ===

    private void Awake()
    {
        cam = Camera.main;
        if (!cam) Debug.LogWarning("[EditModeController] Main Camera를 찾지 못했습니다.");

        _loader ??= new ResourcesLoader();

        // UI/Wiring
        WireUndoButton();
        actionToolbar?.Hide();
        WireSaveButton();
        WireBackButton();
        WireExitPanels();
        WireSavedInfoPanel();
    }

    private void Start()
    {
        // 씬 복원(PlaceableStore) 이후 시점에서 집을 찾아 캐시하고 롱프레스 타깃으로 설정
#if UNITY_2022_2_OR_NEWER
        var tags = FindObjectsByType<PlaceableTag>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var tags = FindObjectsOfType<PlaceableTag>();
#endif
        foreach (var t in tags)
        {
            if (t && t.category == PlaceableCategory.Home)
            {
                homePrev = t.transform;
                homePrevId = t.id;
                SetLongPressTarget(homePrev);
                break;
            }
        }
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        TouchSimulation.Enable();
    }

    private void OnDisable()
    {
        TouchSimulation.Disable();
        EnhancedTouchSupport.Disable();

        BlockOrbit = false;
        isDragging = false;
        actionToolbar?.Hide();
    }

    private void Update()
    {
        HandlePointerLifecycle();
        HandleLongPress();
        MaintainOrbitBlockFlag();
    }

    #endregion


    #region === Home Helpers ===

    private static bool IsHome(Transform t)
    {
        if (!t) return false;
        var tag = t.GetComponent<PlaceableTag>() ?? t.GetComponentInParent<PlaceableTag>();
        return tag && tag.category == PlaceableCategory.Home;
    }

    private static int GetPlaceableId(Transform t)
    {
        var tag = t ? t.GetComponent<PlaceableTag>() ?? t.GetComponentInParent<PlaceableTag>() : null;
        return tag ? tag.id : 0;
    }

    private void SetLongPressTarget(Transform t)
    {
        longPressTarget = t;
    }

    private bool TryCacheExistingHome()
    {
#if UNITY_2022_2_OR_NEWER
        var tags = FindObjectsByType<PlaceableTag>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var tags = FindObjectsOfType<PlaceableTag>();
#endif
        foreach (var tt in tags)
        {
            if (tt && tt.category == PlaceableCategory.Home)
            {
                homePrev = tt.transform;
                homePrevId = tt.id;
                return true;
            }
        }
        return false;
    }

    #endregion


    #region === Home Preview Swap (중복 생성 방지 + 같은 집 재클릭 무시) ===

    /// <summary>
    /// 인벤에서 선택한 '집'을 (0,0,0)에 프리뷰로 교체한다. (OK/Cancel 대기)
    /// - 이미 같은 집이 프리뷰/확정 상태면 새로 생성하지 않음.
    /// - 빠르게 연속 클릭해도 1개만 생성되도록 reentrancy 가드.
    /// </summary>
    public void PreviewSwapHome(IPlaceableData data)
    {
        if (data is not HomePlaceable)
        {
            Debug.LogWarning("[EditModeController] PreviewSwapHome: Home 데이터가 아닙니다.");
            return;
        }

        if (homePreview != null)
        {
            Debug.Log("[Home] 이미 프리뷰/확정 대기 중인 집이 있습니다. 저장 또는 취소 먼저!");
            return;
        }

        if (_homeSwapBusy) return; // 중복 호출 가드
        _homeSwapBusy = true;

        try
        {
            if (!IsEditMode) SetEditMode(true, keepTarget: false);

            int targetId = data.Id;
            int previewId = homePreview ? GetPlaceableId(homePreview) : 0;
            int currentId = homePrev ? homePrevId : 0;

            // 1) 이미 같은 집이 '프리뷰' 중이면: 새로 생성하지 않고 선택만
            if (homePreview && previewId == targetId)
            {
                if (!IsEditMode) SetEditMode(true, keepTarget: false);
                SelectTarget(homePreview);
                SetLongPressTarget(homePreview);
                return;
            }

            // 2) 이미 같은 집이 '확정' 상태면: 생성하지 않음 (선택만)
            if (!homePreview && homePrev && currentId == targetId)
            {
                if (!IsEditMode) SetEditMode(true, keepTarget: false);
                SelectTarget(homePrev);
                SetLongPressTarget(homePrev);
                return;
            }

            // 이전 프리뷰 제거
            if (homePreview) { Destroy(homePreview.gameObject); homePreview = null; }

            // 기존 확정 집 캐시 후 "비활성화만"
            if (!homePrev) TryCacheExistingHome();
            if (homePrev) homePrev.gameObject.SetActive(false);

            // 5) 프리팹 로드
            _loader ??= new ResourcesLoader();
            var prefab = data.GetPrefab(_loader);
            if (!prefab)
            {
                Debug.LogWarning("[EditModeController] PreviewSwapHome: 프리팹 없음");
                if (homePrev) homePrev.gameObject.SetActive(true);
                return;
            }

            int homeId = data.Id;
            Quaternion rot = GetHomeRotation(homeId);


            var preview = Instantiate(prefab, Vector3.zero, rot);
            preview.name = data.DisplayName;

            // 7) 태그/드래그/임시마커
            var tag = preview.GetComponent<PlaceableTag>() ?? preview.AddComponent<PlaceableTag>();
            tag.category = PlaceableCategory.Home;
            tag.id = targetId;

            if (!preview.TryGetComponent<Draggable>(out _)) preview.AddComponent<Draggable>();
            MarkAsInventoryTemp(preview.transform, true);

            // 8) 편집모드 + 선택 + 롱프레스 타깃
            SetEditMode(true, keepTarget: false);
            SelectTarget(preview.transform);
            SetLongPressTarget(preview.transform);

            homePreview = preview.transform;
            homePreviewConfirmed = false;
            hasUnsavedChanges = true; // Cancel 시 해제됨

            HomePreviewActiveChanged?.Invoke(true);
        }
        finally
        {
            _homeSwapBusy = false;
        }
    }
    // 집 id별 "절대 회전값"을 반환하는 헬퍼
    private Quaternion GetHomeRotation(int homeId)
    {
        switch (homeId)
        {
            case 40001:          // 기본집 → 항상 0도
                return Quaternion.Euler(0f, 180f, 0f);

            case 40002:          // 90도
                return Quaternion.Euler(0f, 90f, 0f);

            case 40003:          // 180도
            case 40004:          // 180도
                return Quaternion.Euler(0f, 180f, 0f);

            default:             // 나머지는 일단 0도
                return Quaternion.identity;
        }
    }

    #endregion
}
