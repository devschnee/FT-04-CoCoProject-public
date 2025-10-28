using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using TouchES = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class EditModeController : MonoBehaviour
{
    #region === Config (Inspector) ===
    [Header("Pick & Drag")]
    [SerializeField] private LayerMask draggableMask = ~0;

    [Header("Move Plane")]
    [SerializeField, Tooltip("Y를 시작 높이에 고정")] private bool lockYToInitial = true;
    [SerializeField, Tooltip("lockYToInitial=false일 때 사용할 고정 Y")] private float fixedY = 0f;

    [Header("Grid Snap")]
    [SerializeField] private bool snapToGrid = true;
    [SerializeField, Min(0.01f)] private float gridSize = 1f;
    [SerializeField, Tooltip("격자 원점(이 오프셋 기준으로 스냅)")]
    private Vector3 gridOrigin = Vector3.zero;

    [Header("Undo")]
    [SerializeField, Tooltip("히스토리 최대 저장 개수(-1: 무제한)")] private int undoMax = -1;

    [Header("Overlap")]
    [SerializeField, Tooltip("침투 허용오차(이 값 이하의 접촉은 허용)")] private float overlapEpsilon = 0.0005f;

    [Header("UI (Toolbar/Undo)")]
    [SerializeField] private Button undoButton;
    [SerializeField] private ObjectActionToolbar actionToolbar;

    [Header("Edit Mode Entry (Long Press)")]
    [SerializeField, Tooltip("롱프레스 대상(중심 오브젝트). 이 오브젝트 위에서 1초 누르면 편집모드 진입")]
    private Transform longPressTarget;
    [SerializeField, Tooltip("롱프레스 유지 시간(초)")]
    private float longPressSeconds = 1.0f;
    [SerializeField, Tooltip("롱프레스 중 허용되는 포인터 이동량(px)")]
    private float longPressSlopPixels = 10f;

    [Header("Save/Back Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button backButton;

    [Header("Panels")]
    [SerializeField, Tooltip("뒤로가기 확인 패널(Yes/No)")]
    private GameObject exitConfirmPanel;
    [SerializeField] private Button exitYesButton;
    [SerializeField] private Button exitNoButton;
    [SerializeField, Tooltip("저장 완료 알림 패널(확인 버튼 1개)")]
    private GameObject savedInfoPanel;
    [SerializeField] private Button savedOkButton;

    [Header("Ground Limit")]
    [SerializeField] private LayerMask groundMask;       // ✅ 허용 지면 레이어 (예: Ground)
    [SerializeField] private float groundProbeUp = 3f;   // 위쪽으로 여유 높이
    [SerializeField] private float groundProbeDown = 6f; // 아래쪽으로 탐색할 거리
    [SerializeField] private bool requireGround = true;  // Ground 위에서만 허용할지

    #endregion

    #region === Public State ===
    public bool IsEditMode { get; private set; }
    public Transform CurrentTarget { get; private set; }
    /// <summary>오브젝트 드래그 중일 때 카메라 회전 차단 플래그</summary>
    public static bool BlockOrbit;
    #endregion

    #region === Private State ===
    private Camera cam;

    private bool pointerDown;
    private Vector2 pressScreenPos;
    private Transform pressedHitTarget;
    private bool isDragging;
    private bool movedDuringDrag;
    private bool currentPlacementValid = true;
    private bool startedOnDraggable;

    private Plane movePlane;
    private float movePlaneY;
    private bool movePlaneReady;

    private struct Snap { public Vector3 pos; public Quaternion rot; }
    private Snap? lastBeforeDrag;
    private readonly Dictionary<Transform, Stack<Snap>> history = new();

    // Long-press
    private bool longPressArmed;
    private float longPressTimer;
    private Vector2 longPressStartPos;

    // Save/baseline
    private bool hasUnsavedChanges;
    private struct ObjSnapshot { public Transform t; public Vector3 pos; public Quaternion rot; public bool activeSelf; }
    private readonly List<ObjSnapshot> baseline = new();
    #endregion

    #region === Unity Lifecycle ===
    private void Awake()
    {
        cam = Camera.main;
        if (!cam) Debug.LogWarning("[EditModeController] Main Camera를 찾지 못했습니다.");

        WireUndoButton();
        actionToolbar?.Hide();

        WireSaveButton();
        WireBackButton();
        WireExitPanels();
        WireSavedInfoPanel();
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

    #region === UI Wiring ===
    private void WireUndoButton()
    {
        if (!undoButton) return;
        undoButton.gameObject.SetActive(false);
        undoButton.interactable = false;
        undoButton.onClick.RemoveAllListeners();
        undoButton.onClick.AddListener(UndoLastMove);
    }

    private void WireSaveButton()
    {
        if (!saveButton) return;
        saveButton.gameObject.SetActive(false);
        saveButton.onClick.RemoveAllListeners();
        saveButton.onClick.AddListener(OnSaveClicked);
    }

    private void WireBackButton()
    {
        if (!backButton) return;
        backButton.gameObject.SetActive(false);
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(OnBackClicked);
    }

    private void WireExitPanels()
    {
        if (exitConfirmPanel) exitConfirmPanel.SetActive(false);

        if (exitYesButton)
        {
            exitYesButton.onClick.RemoveAllListeners();
            exitYesButton.onClick.AddListener(() =>
            {
                if (exitConfirmPanel) exitConfirmPanel.SetActive(false);
                ExitWithoutSave(restore: true);
            });
        }

        if (exitNoButton)
        {
            exitNoButton.onClick.RemoveAllListeners();
            exitNoButton.onClick.AddListener(() =>
            {
                if (exitConfirmPanel) exitConfirmPanel.SetActive(false);
            });
        }
    }

    private void WireSavedInfoPanel()
    {
        if (savedInfoPanel) savedInfoPanel.SetActive(false);
        if (!savedOkButton) return;

        savedOkButton.onClick.RemoveAllListeners();
        savedOkButton.onClick.AddListener(() => savedInfoPanel?.SetActive(false));
    }
    #endregion

    #region === Edit Mode / Selection ===
    private void SetEditMode(bool on, bool keepTarget)
    {
        if (IsEditMode == on)
        {
            if (!on && !keepTarget) SelectTarget(null);
            return;
        }

        ToggleTopButtons(on);
        IsEditMode = on;

        // 편집모드 전환 신호: EditModeManager 호출 (UI 스위칭은 스위처가 이벤트로 처리)
        var mgr = FindAnyObjectByType<EditModeManager>();
        if (mgr != null)
        {
            if (on) mgr.EnterEditMode();
            else mgr.ExitEditMode();
        }

        if (on)
        {
            // 🔧 화면 회전(오빗) 막기 - 진입 시에 반드시 켜줘야 함
            BlockOrbit = true;  // ← 이 줄이 누락되어 있었음

            history.Clear();
            CaptureBaseline();
            hasUnsavedChanges = false;
            UpdateUndoUI();
            UpdateToolbar();
        }
        else
        {
            // 종료 처리
            if (CurrentTarget && CurrentTarget.TryGetComponent<Draggable>(out var drag))
            {
                drag.SetInvalid(false);
                drag.SavePosition(); // 정책: 종료 시 저장
                drag.SetHighlighted(false);
            }
            if (!keepTarget) SelectTarget(null);

            lastBeforeDrag = null;
            isDragging = false;

            // 🔓 종료 시에는 해제
            BlockOrbit = false;

            history.Clear();
            UpdateUndoUI();
            actionToolbar?.Hide();
        }
    }


    private void ToggleTopButtons(bool on)
    {
        if (undoButton) undoButton.gameObject.SetActive(on);
        if (saveButton) saveButton.gameObject.SetActive(on);
        if (backButton) backButton.gameObject.SetActive(on);
    }

    public void SelectTarget(Transform t)
    {
        if (CurrentTarget && CurrentTarget.TryGetComponent<Draggable>(out var prev))
        {
            prev.SetInvalid(false);
            prev.SetHighlighted(false);
        }

        CurrentTarget = t;

        if (CurrentTarget && CurrentTarget.TryGetComponent<Draggable>(out var now))
        {
            now.SetInvalid(false);
            now.SetHighlighted(true);
        }

        UpdateToolbar();
        UpdateUndoUI();
    }

    private void UpdateToolbar()
    {
        if (!actionToolbar) return;
        if (IsEditMode && CurrentTarget) ShowToolbarFor(CurrentTarget);
        else actionToolbar.Hide();
    }
    #endregion

    #region === Pointer Lifecycle ===
    private void HandlePointerLifecycle()
    {
        if (IsPointerDownThisFrame() && !IsPointerOverUI())
            OnPointerDown();

        if (!pointerDown) return;

        OnPointerHeldOrDragged();

        if (IsPointerUpThisFrame())
            OnPointerUp();
    }

    private void OnPointerDown()
    {
        pointerDown = true;
        pressScreenPos = GetPointerScreenPos();
        if (!ScreenPosValid(pressScreenPos)) { pointerDown = false; return; } // ★ 좌표 가드

        pressedHitTarget = RaycastDraggable(pressScreenPos);
        movePlaneReady = false;

        if (IsEditMode && pressedHitTarget) SelectTarget(pressedHitTarget);

        // 롱프레스 준비
        longPressArmed = false;
        longPressTimer = 0f;
        if (!IsEditMode && longPressTarget)
        {
            var hit = RaycastTransform(pressScreenPos);
            if (hit == longPressTarget) // 필요 시 자식 허용: hit.IsChildOf(longPressTarget)
            {
                longPressArmed = true;
                longPressStartPos = pressScreenPos;
            }
        }

        startedOnDraggable = IsEditMode && pressedHitTarget;

        isDragging = false;
        movedDuringDrag = false;
        currentPlacementValid = true;
    }

    private void OnPointerHeldOrDragged()
    {
        if (IsEditMode && startedOnDraggable && CurrentTarget && IsPointerMoving())
        {
            // 드래그 시작 시
            if (!isDragging)
            {
                isDragging = true;
                BlockOrbit = true;

                lastBeforeDrag = new Snap { pos = CurrentTarget.position, rot = CurrentTarget.rotation };

                PrepareMovePlane();

                // 드래그 중 툴바 숨김
                actionToolbar?.Hide();
            }

            var sp = GetPointerScreenPos();
            if (!ScreenPosValid(sp)) return; // ★ 좌표 가드
            DragMove(sp);
        }
    }

    private void OnPointerUp()
    {
        pointerDown = false;

        longPressArmed = false;
        longPressTimer = 0f;

        if (isDragging)
        {
            isDragging = false;
            BlockOrbit = false;

            if (IsEditMode && CurrentTarget)
            {
                if (!currentPlacementValid)
                {
                    // 원복
                    if (lastBeforeDrag.HasValue)
                    {
                        CurrentTarget.position = lastBeforeDrag.Value.pos;
                        CurrentTarget.rotation = lastBeforeDrag.Value.rot;
                    }

                    if (CurrentTarget.TryGetComponent<Draggable>(out var drag0))
                    {
                        drag0.SetInvalid(false);
                        drag0.SetHighlighted(true);
                    }
                }
                else if (movedDuringDrag && lastBeforeDrag.HasValue)
                {
                    var stack = GetOrCreateHistory(CurrentTarget);
                    stack.Push(lastBeforeDrag.Value);
                    TrimHistoryIfNeeded(stack);

                    hasUnsavedChanges = true;
                    UpdateUndoUI();
                }
            }
        }

        movedDuringDrag = false;
        lastBeforeDrag = null;
        currentPlacementValid = true;
        startedOnDraggable = false;

        if (IsEditMode && CurrentTarget) UpdateToolbar();
    }
    #endregion

    #region === Long-Press Entry ===
    private void HandleLongPress()
    {
        if (!longPressArmed || IsEditMode || !pointerDown) return;

        Vector2 cur = GetPointerScreenPos();
        if (!ScreenPosValid(cur)) { longPressArmed = false; return; } // ★ 좌표 가드

        if ((cur - longPressStartPos).sqrMagnitude > longPressSlopPixels * longPressSlopPixels)
        {
            longPressArmed = false;
            return;
        }

        longPressTimer += Time.unscaledDeltaTime;
        if (longPressTimer >= longPressSeconds)
        {
            longPressArmed = false;
            SetEditMode(true, keepTarget: true);
            if (longPressTarget) SelectTarget(longPressTarget);
        }
    }
    #endregion

    #region === Drag / Move / Snap ===
    private void PrepareMovePlane()
    {
        float y = fixedY;
        if (lockYToInitial && CurrentTarget) y = CurrentTarget.position.y;

        movePlaneY = y;
        movePlane = new Plane(Vector3.up, new Vector3(0f, movePlaneY, 0f));
        movePlaneReady = true;
    }

    private void DragMove(Vector2 screenPos)
    {
        if (!cam) return;
        if (!ScreenPosValid(screenPos)) return; // ★ 좌표 가드
        if (!movePlaneReady) PrepareMovePlane();

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!movePlane.Raycast(ray, out float enter)) return;

        Vector3 hit = ray.GetPoint(enter);
        hit.y = movePlaneY;

        if (snapToGrid) hit = SnapToGrid(hit);

        if (!CurrentTarget || CurrentTarget.position == hit) return;

        CurrentTarget.position = hit;
        movedDuringDrag = true;

        // 👇 추가: Ground 위 여부 + 겹침 여부를 모두 만족해야 유효
        bool onGround = IsOverGround(hit);
        bool noOverlap = !OverlapsOthers(CurrentTarget);
        bool valid = onGround && noOverlap;

        currentPlacementValid = valid;

        if (CurrentTarget.TryGetComponent<Draggable>(out var drag))
        {
            drag.SetInvalid(!valid);   // 유효하지 않으면 빨간색(기존 로직 활용)
            drag.SetHighlighted(true);
        }
    }


    private Vector3 SnapToGrid(Vector3 world)
    {
        float Snap(float v, float origin) => Mathf.Round((v - origin) / gridSize) * gridSize + origin;
        world.x = Snap(world.x, gridOrigin.x);
        world.z = Snap(world.z, gridOrigin.z);
        return world;
    }
    #endregion

    #region === Baseline Snapshot ===
    private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    private void CaptureBaseline()
    {
        baseline.Clear();
        var set = new HashSet<int>();

#if UNITY_2022_2_OR_NEWER
        var drags = FindObjectsByType<Draggable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var drags = Resources.FindObjectsOfTypeAll<Draggable>();
#endif
        foreach (var d in drags)
        {
            if (!d) continue;
            var tr = d.transform;
            if (tr && set.Add(tr.GetInstanceID()))
                baseline.Add(new ObjSnapshot { t = tr, pos = tr.position, rot = tr.rotation, activeSelf = tr.gameObject.activeSelf });
        }

#if UNITY_2022_2_OR_NEWER
        var cols = FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var cols = Resources.FindObjectsOfTypeAll<Collider>();
#endif
        foreach (var c in cols)
        {
            if (!c) continue;
            var go = c.gameObject;
            if (!go.scene.IsValid()) continue;
            if (!IsInLayerMask(go.layer, draggableMask)) continue;

            var tr = c.transform;
            if (tr && set.Add(tr.GetInstanceID()))
                baseline.Add(new ObjSnapshot { t = tr, pos = tr.position, rot = tr.rotation, activeSelf = tr.gameObject.activeSelf });
        }
    }

    private void RestoreBaseline()
    {
        foreach (var s in baseline)
        {
            if (!s.t) continue;

            if (s.t.gameObject.activeSelf != s.activeSelf)
                s.t.gameObject.SetActive(s.activeSelf);

            var rb = s.t.GetComponent<Rigidbody>();
            if (rb)
            {
                bool prevKinematic = rb.isKinematic;
                var prevDetect = rb.collisionDetectionMode;

                rb.isKinematic = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.position = s.pos;
                rb.rotation = s.rot;
                rb.collisionDetectionMode = prevDetect;
                rb.isKinematic = prevKinematic;
            }
            else
            {
                s.t.position = s.pos;
                s.t.rotation = s.rot;
            }

            var d = s.t.GetComponent<Draggable>();
            if (d) { d.SetInvalid(false); d.SetHighlighted(false); }
        }

        Physics.SyncTransforms();
    }
    #endregion

    #region === Overlap Check ===
    private bool OverlapsOthers(Transform t)
    {
        var myCols = t.GetComponentsInChildren<Collider>();
        if (myCols == null || myCols.Length == 0) return false;
        if (!TryGetCombinedBoundsFromColliders(myCols, out Bounds myBounds)) return false;

        var half = myBounds.extents;
        var center = myBounds.center;

        var candidates = Physics.OverlapBox(center, half, Quaternion.identity, draggableMask, QueryTriggerInteraction.Ignore);
        if (candidates == null || candidates.Length == 0) return false;

        foreach (var other in candidates)
        {
            if (!other || !other.enabled) continue;
            if (IsSameRootOrChild(t, other.transform)) continue;

            foreach (var my in myCols)
            {
                if (!my || !my.enabled) continue;
                if (my.isTrigger || other.isTrigger) continue;

                if (Physics.ComputePenetration(
                        my, my.transform.position, my.transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out _, out float dist))
                {
                    if (dist > overlapEpsilon) return true;
                }
            }
        }
        return false;
    }

    private static bool IsSameRootOrChild(Transform root, Transform other) => other == root || other.IsChildOf(root);

    private static bool TryGetCombinedBoundsFromColliders(Collider[] cols, out Bounds combined)
    {
        combined = new Bounds();
        bool hasAny = false;
        foreach (var c in cols)
        {
            if (!c || !c.enabled) continue;
            if (!hasAny) { combined = c.bounds; hasAny = true; }
            else combined.Encapsulate(c.bounds);
        }
        return hasAny;
    }
    #endregion

    #region === Undo ===
    public void UndoLastMove()
    {
        if (!CurrentTarget) return;

        if (history.TryGetValue(CurrentTarget, out var stack) && stack.Count > 0)
        {
            Snap prev = stack.Peek();

            // 현재 상태 백업
            Vector3 curPos = CurrentTarget.position;
            Quaternion curRot = CurrentTarget.rotation;

            // 이전 스냅샷으로 복원
            CurrentTarget.position = prev.pos;
            CurrentTarget.rotation = prev.rot;

            if (OverlapsOthers(CurrentTarget))
            {
                // 겹치면 취소
                CurrentTarget.position = curPos;
                CurrentTarget.rotation = curRot;

                if (CurrentTarget.TryGetComponent<Draggable>(out var dragFail))
                {
                    dragFail.SetInvalid(true);
                    dragFail.SetHighlighted(true);
                }
                Debug.Log("[Undo] 이전 상태가 겹쳐서 되돌릴 수 없습니다.");
                return;
            }

            stack.Pop();
            if (CurrentTarget.TryGetComponent<Draggable>(out var dragOk))
            {
                dragOk.SetInvalid(false);
                dragOk.SetHighlighted(true);
            }
            hasUnsavedChanges = true;
            Debug.Log("[Undo] 되돌리기 성공");
        }

        UpdateUndoUI();
    }

    public void ClearCurrentHistory()
    {
        if (!CurrentTarget) return;
        if (history.ContainsKey(CurrentTarget)) history[CurrentTarget].Clear();
        UpdateUndoUI();
    }

    private Stack<Snap> GetOrCreateHistory(Transform t)
    {
        if (!history.TryGetValue(t, out var stack))
        {
            stack = new Stack<Snap>(8);
            history[t] = stack;
        }
        return stack;
    }

    private void TrimHistoryIfNeeded(Stack<Snap> stack)
    {
        if (undoMax <= 0) return;
        if (stack.Count <= undoMax) return;

        // 오래된 항목 제거 (Bottom부터)
        var arr = stack.ToArray();   // Top->Bottom
        Array.Reverse(arr);          // Bottom->Top
        int removeCount = stack.Count - undoMax;

        var trimmed = new List<Snap>(undoMax);
        for (int i = 0; i < arr.Length; i++)
        {
            if (i < removeCount) continue;
            trimmed.Add(arr[i]);
        }
        stack.Clear();
        for (int i = trimmed.Count - 1; i >= 0; i--)
            stack.Push(trimmed[i]);
    }

    private void UpdateUndoUI()
    {
        if (!undoButton) return;

        if (!IsEditMode)
        {
            undoButton.interactable = false;
            return;
        }

        bool canUndo = false;
        if (CurrentTarget && history.TryGetValue(CurrentTarget, out var stack))
            canUndo = stack != null && stack.Count > 0;

        undoButton.interactable = canUndo;
    }
    #endregion

    #region === Save / Back ===
    private void OnBackClicked()
    {
        if (hasUnsavedChanges)
        {
            if (exitConfirmPanel) exitConfirmPanel.SetActive(true);
            else ExitWithoutSave(restore: true);
        }
        else
        {
            ExitWithoutSave(restore: false);
        }
    }

    private void ExitWithoutSave(bool restore)
    {
        if (restore)
        {
            RestoreBaseline();
            SaveAllDraggablePositions();
        }

        SetEditMode(false, keepTarget: false);

        hasUnsavedChanges = false;
        baseline.Clear();
    }

    private void OnSaveClicked()
    {
        SaveAllDraggablePositions();
        hasUnsavedChanges = false;

        CaptureBaseline();
        if (savedInfoPanel) savedInfoPanel.SetActive(true);
        else Debug.Log("[Save] 저장되었습니다!");
    }

    private void SaveAllDraggablePositions()
    {
#if UNITY_2022_2_OR_NEWER
        var drags = FindObjectsByType<Draggable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var drags = Resources.FindObjectsOfTypeAll<Draggable>();
#endif
        int count = 0;
        foreach (var d in drags)
        {
            if (!d) continue;
            if (!d.gameObject.activeInHierarchy) continue; // 활성만 저장
            d.SavePosition();
            count++;
        }
        Debug.Log($"[Save] Draggable (활성) {count}개 저장 완료");
    }
    #endregion

    #region === Toolbar ===
    private void ShowToolbarFor(Transform t)
    {
        if (!actionToolbar) return;

        // 인벤/프리뷰 제거: 기본 두 버튼만
        actionToolbar.Show(
            target: t,
            worldCamera: cam,
            onInfo: OnToolbarInfo,
            onRotate: OnToolbarRotate,
            onInventory: null,
            onOk: null,
            onCancel: null
        );
    }

    private void OnToolbarInfo()
    {
        if (!CurrentTarget) return;

        // 대상에서 ObjectMeta 찾기(부모/자식 허용)
        var meta = CurrentTarget.GetComponentInParent<ObjectMeta>();
        if (!meta) meta = CurrentTarget.GetComponentInChildren<ObjectMeta>();
        if (!meta)
        {
            Debug.LogWarning("[EditModeController] 선택 대상에 ObjectMeta가 없습니다.");
            return;
        }

        var panel = InfoPanel.FindInScene();
        if (!panel)
        {
            Debug.LogWarning("[EditModeController] InfoPanel을 씬에서 찾지 못했습니다.");
            return;
        }

        // 토글: 열려 있으면 닫고, 아니면 메타 정보로 열기
        panel.Toggle(meta.DisplayName, meta.Description);
    }

    private void OnToolbarRotate()
    {
        if (!CurrentTarget) return;

        // 회전 전 스냅샷 저장 (Undo용)
        var originalSnap = new Snap { pos = CurrentTarget.position, rot = CurrentTarget.rotation };

        CurrentTarget.Rotate(0f, 90f, 0f, Space.World);

        if (OverlapsOthers(CurrentTarget))
        {
            // 겹치면 회전 취소
            CurrentTarget.position = originalSnap.pos;
            CurrentTarget.rotation = originalSnap.rot;

            if (CurrentTarget.TryGetComponent<Draggable>(out var dragFail))
            {
                dragFail.SetInvalid(true);
                dragFail.SetHighlighted(true);
            }
            Debug.Log("[Rotate] 겹쳐서 회전을 취소했습니다.");
            return;
        }

        // 정상 회전 → Undo 스택에 방금 전 상태 push
        var stack = GetOrCreateHistory(CurrentTarget);
        stack.Push(originalSnap);
        TrimHistoryIfNeeded(stack);
        UpdateUndoUI();

        if (CurrentTarget.TryGetComponent<Draggable>(out var dragOk))
        {
            dragOk.SetInvalid(false);
            dragOk.SetHighlighted(true);
        }

        hasUnsavedChanges = true;
    }
    #endregion

    #region === Raycast / Input Utils ===
    private static Vector2 GetPointerScreenPos()
    {
        if (TouchES.activeTouches.Count > 0)
            return TouchES.activeTouches[0].screenPosition;
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    private static bool IsPointerDownThisFrame()
    {
        for (int i = 0; i < TouchES.activeTouches.Count; i++)
            if (TouchES.activeTouches[i].phase == UnityEngine.InputSystem.TouchPhase.Began)
                return true;

        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private static bool IsPointerUpThisFrame()
    {
        for (int i = 0; i < TouchES.activeTouches.Count; i++)
        {
            var ph = TouchES.activeTouches[i].phase;
            if (ph == UnityEngine.InputSystem.TouchPhase.Ended ||
                ph == UnityEngine.InputSystem.TouchPhase.Canceled)
                return true;
        }
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
    }

    private static bool IsPointerMoving()
    {
        for (int i = 0; i < TouchES.activeTouches.Count; i++)
            if (TouchES.activeTouches[i].phase == UnityEngine.InputSystem.TouchPhase.Moved)
                return true;

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return Mouse.current.delta.ReadValue().sqrMagnitude > 0f;

        return false;
    }

    private static bool IsPointerOverUI()
    {
        if (!EventSystem.current) return false;

        // 현재 포인터 스크린 좌표
        Vector2 pos = GetPointerScreenPos();

        // 이 좌표로 UI 레이캐스트를 직접 수행
        var data = new PointerEventData(EventSystem.current) { position = pos };
        var results = new List<RaycastResult>(8);
        EventSystem.current.RaycastAll(data, results);

        return results.Count > 0; // 하나라도 맞으면 UI 위
    }

    // ★ 좌표 유효성 가드 (frustum 경고 방지)
    private static bool ScreenPosValid(Vector2 sp)
    {
        if (float.IsNaN(sp.x) || float.IsNaN(sp.y) || float.IsInfinity(sp.x) || float.IsInfinity(sp.y))
            return false;
        return (sp.x >= 0 && sp.y >= 0 && sp.x <= Screen.width && sp.y <= Screen.height);
    }

    private Transform RaycastDraggable(Vector2 screenPos)
    {
        if (!cam) return null;
        if (!ScreenPosValid(screenPos)) return null; // ★ 가드
        Ray ray = cam.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out RaycastHit hit, 1000f, draggableMask) ? hit.transform : null;
    }

    private Transform RaycastTransform(Vector2 screenPos)
    {
        if (!cam) return null;
        if (!ScreenPosValid(screenPos)) return null; // ★ 가드
        Ray ray = cam.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out RaycastHit hit, 1000f, ~0) ? hit.transform : null;
    }

    private void MaintainOrbitBlockFlag()
    {
        if (!isDragging && BlockOrbit) BlockOrbit = false;
    }
    #endregion

    // EditController.cs 내부 아무 곳 (Region Utils 등)
    private bool IsOverGround(Vector3 worldPos)
    {
        if (!requireGround) return true;

        // worldPos 위에서 아래로 레이캐스트 → Ground 맞으면 OK
        Vector3 origin = new Vector3(worldPos.x, worldPos.y + groundProbeUp, worldPos.z);
        float dist = groundProbeUp + groundProbeDown;

        return Physics.Raycast(origin, Vector3.down, out _, dist, groundMask, QueryTriggerInteraction.Ignore);
    }

}