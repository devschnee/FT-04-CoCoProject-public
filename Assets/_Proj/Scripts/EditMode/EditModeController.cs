using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TouchES = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class EditModeController : MonoBehaviour
{
    #region === Config (Inspector) ===
    [Header("Pick & Drag")]
    [SerializeField] LayerMask draggableMask = ~0;

    [Header("Move Plane")]
    [SerializeField, Tooltip("Y를 시작 높이에 고정")] bool lockYToInitial = true;
    [SerializeField, Tooltip("lockYToInitial=false일 때 사용할 고정 Y")] float fixedY = 0f;

    [Header("Grid Snap")]
    [SerializeField] bool snapToGrid = true;
    [SerializeField, Min(0.01f)] float gridSize = 1f;
    [SerializeField, Tooltip("격자 원점(이 오프셋 기준으로 스냅)")]
    Vector3 gridOrigin = Vector3.zero;

    [Header("Undo")]
    [SerializeField, Tooltip("히스토리 최대 저장 개수(-1: 무제한)")] int undoMax = -1;

    [Header("Overlap")]
    [SerializeField, Tooltip("침투 허용오차(이 값 이하의 접촉은 허용)")] float overlapEpsilon = 0.0005f;

    [Header("UI (Toolbar/Undo)")]
    [SerializeField] Button undoButton;
    [SerializeField] ObjectActionToolbar actionToolbar;

    [Header("Edit Mode Entry (Long Press)")]
    [SerializeField, Tooltip("롱프레스 대상(중심 오브젝트). 이 오브젝트 위에서 1초 누르면 편집모드 진입")]
    Transform longPressTarget;
    [SerializeField, Tooltip("롱프레스 유지 시간(초)")]
    float longPressSeconds = 1.0f;
    [SerializeField, Tooltip("롱프레스 중 허용되는 포인터 이동량(px)")]
    float longPressSlopPixels = 10f;

    [Header("Save/Back Buttons")]
    [SerializeField] Button saveButton;
    [SerializeField] Button backButton;

    [Header("Panels")]
    [SerializeField, Tooltip("뒤로가기 확인 패널(Yes/No)")]
    GameObject exitConfirmPanel;
    [SerializeField] Button exitYesButton;
    [SerializeField] Button exitNoButton;
    [SerializeField, Tooltip("저장 완료 알림 패널(확인 버튼 1개)")]
    GameObject savedInfoPanel;
    [SerializeField] Button savedOkButton;
    #endregion

    #region === Public State ===
    public bool IsEditMode { get; private set; }
    public Transform CurrentTarget { get; private set; }
    public static bool BlockOrbit;
    #endregion

    #region === Private State ===
    Camera cam;

    bool pointerDown;
    Vector2 pressScreenPos;
    Transform pressedHitTarget;
    bool isDragging = false;
    bool movedDuringDrag = false;
    bool _currentPlacementValid = true;
    bool _startedOnDraggable = false;

    Plane movePlane;
    float movePlaneY;
    bool movePlaneReady;

    Vector3? lastBeforeDragPos = null;
    readonly Dictionary<Transform, Stack<Vector3>> _history = new();

    bool longPressArmed = false;
    float longPressTimer = 0f;
    Vector2 longPressStartPos;

    bool hasUnsavedChanges = false;

    struct ObjSnapshot { public Transform t; public Vector3 pos; public Quaternion rot; public bool activeSelf; }
    readonly List<ObjSnapshot> _baseline = new List<ObjSnapshot>();
    #endregion

    #region === Unity Lifecycle ===
    void Awake()
    {
        cam = Camera.main;
        if (!cam) Debug.LogWarning("[EditModeController] Main Camera를 찾지 못했습니다.");

        InitUndoButton();
        actionToolbar?.Hide();

        InitSaveButton();
        InitBackButton();
        InitExitPanels();
        InitSavedInfoPanel();
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        TouchSimulation.Enable();
    }

    void OnDisable()
    {
        TouchSimulation.Disable();
        EnhancedTouchSupport.Disable();
        BlockOrbit = false;
        isDragging = false;
        if (actionToolbar != null) actionToolbar.Hide();
    }

    void Update()
    {
        HandlePointerLifecycle();
        HandleLongPressCheck();
        MaintainOrbitBlockFlag();
    }
    #endregion

    #region === UI Wiring (Awake) ===
    void InitUndoButton()
    {
        if (!undoButton) return;
        undoButton.gameObject.SetActive(false);
        undoButton.interactable = false;
        undoButton.onClick.RemoveAllListeners();
        undoButton.onClick.AddListener(UndoLastMove);
    }

    void InitSaveButton()
    {
        if (!saveButton) return;
        saveButton.gameObject.SetActive(false);
        saveButton.onClick.RemoveAllListeners();
        saveButton.onClick.AddListener(OnSaveClicked);
    }

    void InitBackButton()
    {
        if (!backButton) return;
        backButton.gameObject.SetActive(false);
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(OnBackClicked);
    }

    void InitExitPanels()
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

    void InitSavedInfoPanel()
    {
        if (savedInfoPanel) savedInfoPanel.SetActive(false);
        if (!savedOkButton) return;

        savedOkButton.onClick.RemoveAllListeners();
        savedOkButton.onClick.AddListener(() =>
        {
            if (savedInfoPanel) savedInfoPanel.SetActive(false);
        });
    }
    #endregion

    #region === Edit Mode Toggle / Selection ===
    void SetEditMode(bool on, bool keepTarget)
    {
        if (IsEditMode == on)
        {
            if (!on && !keepTarget) SelectTarget(null);
            return;
        }

        if (undoButton) undoButton.gameObject.SetActive(on);
        if (saveButton) saveButton.gameObject.SetActive(on);
        if (backButton) backButton.gameObject.SetActive(on);

        IsEditMode = on;

        if (on)
        {
            _history.Clear();
            CaptureBaseline();
            hasUnsavedChanges = false;
            UpdateUndoUI();

            if (actionToolbar)
            {
                if (CurrentTarget) ShowToolbarFor(CurrentTarget);
                else actionToolbar.Hide();
            }
        }
        else
        {
            if (CurrentTarget && CurrentTarget.TryGetComponent<Draggable>(out var drag))
            {
                drag.SetInvalid(false);
                drag.SavePosition(); // 편집 종료 시 저장(현재 정책)
                drag.SetHighlighted(false);
            }
            if (!keepTarget) SelectTarget(null);

            lastBeforeDragPos = null;
            isDragging = false;
            BlockOrbit = false;

            _history.Clear();
            UpdateUndoUI();

            if (actionToolbar) actionToolbar.Hide();
        }
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

        if (actionToolbar)
        {
            if (IsEditMode && CurrentTarget) ShowToolbarFor(CurrentTarget);
            else actionToolbar.Hide();
        }

        UpdateUndoUI();
    }
    #endregion

    #region === Pointer Lifecycle ===
    void HandlePointerLifecycle()
    {
        if (IsPointerDownThisFrame() && !IsPointerOverUI())
            OnPointerDown();

        if (!pointerDown) return;

        OnPointerHeldOrDragged();

        if (IsPointerUpThisFrame())
            OnPointerUp();
    }

    void OnPointerDown()
    {
        pointerDown = true;
        pressScreenPos = GetPointerScreenPos();
        pressedHitTarget = RaycastDraggable(pressScreenPos);
        movePlaneReady = false;

        if (IsEditMode && pressedHitTarget) SelectTarget(pressedHitTarget);

        longPressArmed = false;
        longPressTimer = 0f;
        if (!IsEditMode && longPressTarget)
        {
            var hit = RaycastTransform(pressScreenPos);
            if (hit == longPressTarget)
            {
                longPressArmed = true;
                longPressStartPos = pressScreenPos;
            }
        }

        _startedOnDraggable = IsEditMode && pressedHitTarget;

        isDragging = false;
        movedDuringDrag = false;
        _currentPlacementValid = true;
    }

    void OnPointerHeldOrDragged()
    {
        if (IsEditMode && _startedOnDraggable && CurrentTarget && IsPointerMoving())
        {
            if (!isDragging)
            {
                isDragging = true;
                BlockOrbit = true;
                lastBeforeDragPos = CurrentTarget.position;
                PrepareMovePlane();

                actionToolbar?.Hide();
            }

            DragMove(GetPointerScreenPos());
        }
    }

    void OnPointerUp()
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
                if (!_currentPlacementValid)
                {
                    if (lastBeforeDragPos.HasValue)
                        CurrentTarget.position = lastBeforeDragPos.Value;

                    if (CurrentTarget.TryGetComponent<Draggable>(out var drag0))
                    {
                        drag0.SetInvalid(false);
                        drag0.SetHighlighted(true);
                    }
                }
                else if (movedDuringDrag)
                {
                    if (lastBeforeDragPos.HasValue)
                    {
                        var stack = GetOrCreateHistory(CurrentTarget);
                        stack.Push(lastBeforeDragPos.Value);
                        TrimHistoryIfNeeded(stack);
                    }

                    hasUnsavedChanges = true;
                    UpdateUndoUI();
                }
            }
        }

        movedDuringDrag = false;
        lastBeforeDragPos = null;
        _currentPlacementValid = true;
        _startedOnDraggable = false;

        if (IsEditMode && CurrentTarget && actionToolbar) ShowToolbarFor(CurrentTarget);
    }
    #endregion

    #region === Long-Press Entry ===
    void HandleLongPressCheck()
    {
        if (!longPressArmed || IsEditMode || !pointerDown) return;

        Vector2 cur = GetPointerScreenPos();
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
    void PrepareMovePlane()
    {
        float y = fixedY;
        if (lockYToInitial && CurrentTarget) y = CurrentTarget.position.y;

        movePlaneY = y;
        movePlane = new Plane(Vector3.up, new Vector3(0f, movePlaneY, 0f));
        movePlaneReady = true;
    }

    void DragMove(Vector2 screenPos)
    {
        if (!cam) return;
        if (!movePlaneReady) PrepareMovePlane();

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!movePlane.Raycast(ray, out float enter)) return;

        Vector3 hit = ray.GetPoint(enter);
        hit.y = movePlaneY;

        if (snapToGrid) hit = SnapToGrid(hit);

        if (CurrentTarget && CurrentTarget.position != hit)
        {
            CurrentTarget.position = hit;
            movedDuringDrag = true;

            bool valid = !OverlapsOthers(CurrentTarget);
            _currentPlacementValid = valid;

            if (CurrentTarget.TryGetComponent<Draggable>(out var drag))
            {
                drag.SetInvalid(!valid);
                drag.SetHighlighted(true);
            }
        }
    }

    Vector3 SnapToGrid(Vector3 world)
    {
        float Snap(float v, float origin) => Mathf.Round((v - origin) / gridSize) * gridSize + origin;
        world.x = Snap(world.x, gridOrigin.x);
        world.z = Snap(world.z, gridOrigin.z);
        return world;
    }
    #endregion

    #region === Baseline Snapshot ===
    static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    void CaptureBaseline()
    {
        _baseline.Clear();
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
                _baseline.Add(new ObjSnapshot { t = tr, pos = tr.position, rot = tr.rotation, activeSelf = tr.gameObject.activeSelf });
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
                _baseline.Add(new ObjSnapshot { t = tr, pos = tr.position, rot = tr.rotation, activeSelf = tr.gameObject.activeSelf });
        }
    }

    void RestoreBaseline()
    {
        foreach (var s in _baseline)
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
    bool OverlapsOthers(Transform t)
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

    static bool IsSameRootOrChild(Transform root, Transform other) => other == root || other.IsChildOf(root);

    static bool TryGetCombinedBoundsFromColliders(Collider[] cols, out Bounds combined)
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

        if (_history.TryGetValue(CurrentTarget, out var stack) && stack.Count > 0)
        {
            Vector3 prev = stack.Peek();
            Vector3 original = CurrentTarget.position;

            CurrentTarget.position = prev;
            bool overlap = OverlapsOthers(CurrentTarget);

            if (overlap)
            {
                CurrentTarget.position = original;
                if (CurrentTarget.TryGetComponent<Draggable>(out var dragFail))
                {
                    dragFail.SetInvalid(true);
                    dragFail.SetHighlighted(true);
                }
                Debug.Log("[Undo] 이전 위치가 다른 오브젝트와 겹쳐 되돌릴 수 없습니다.");
                return;
            }

            stack.Pop();
            if (CurrentTarget.TryGetComponent<Draggable>(out var drag))
            {
                drag.SetInvalid(false);
                drag.SetHighlighted(true);
            }
            hasUnsavedChanges = true;
            Debug.Log("[Undo] 위치 되돌리기 성공");
        }

        UpdateUndoUI();
    }

    public void ClearCurrentHistory()
    {
        if (!CurrentTarget) return;
        if (_history.ContainsKey(CurrentTarget)) _history[CurrentTarget].Clear();
        UpdateUndoUI();
    }

    Stack<Vector3> GetOrCreateHistory(Transform t)
    {
        if (!_history.TryGetValue(t, out var stack))
        {
            stack = new Stack<Vector3>(8);
            _history[t] = stack;
        }
        return stack;
    }

    void TrimHistoryIfNeeded(Stack<Vector3> stack)
    {
        if (undoMax <= 0) return;
        if (stack.Count <= undoMax) return;

        var arr = stack.ToArray();
        Array.Reverse(arr);
        int removeCount = stack.Count - undoMax;

        var trimmed = new List<Vector3>(undoMax);
        for (int i = 0; i < arr.Length; i++)
        {
            if (i < removeCount) continue;
            trimmed.Add(arr[i]);
        }
        stack.Clear();
        for (int i = trimmed.Count - 1; i >= 0; i--)
            stack.Push(trimmed[i]);
    }

    void UpdateUndoUI()
    {
        if (!undoButton) return;

        if (!IsEditMode)
        {
            undoButton.interactable = false;
            return;
        }

        bool canUndo = false;
        if (CurrentTarget && _history.TryGetValue(CurrentTarget, out var stack))
            canUndo = stack != null && stack.Count > 0;

        undoButton.interactable = canUndo;
    }
    #endregion

    #region === Save / Back ===
    void OnBackClicked()
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

    void ExitWithoutSave(bool restore)
    {
        if (restore)
        {
            RestoreBaseline();
            SaveAllDraggablePositions();
        }

        SetEditMode(false, keepTarget: false);

        hasUnsavedChanges = false;
        _baseline.Clear();
    }

    void OnSaveClicked()
    {
        SaveAllDraggablePositions();
        hasUnsavedChanges = false;

        CaptureBaseline();
        if (savedInfoPanel) savedInfoPanel.SetActive(true);
        else Debug.Log("[Save] 저장되었습니다!");
    }

    void SaveAllDraggablePositions()
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
    void ShowToolbarFor(Transform t)
    {
        if (!actionToolbar) return;

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

    void OnToolbarInfo()
    {
        if (!CurrentTarget) return;
        if (CurrentTarget.TryGetComponent<ObjectMeta>(out var meta))
        {
            var panel = UnityEngine.Object.FindFirstObjectByType<InfoPanel>(FindObjectsInactive.Include);
            if (panel) meta.ShowInfo(panel);
        }
    }

    void OnToolbarRotate()
    {
        if (!CurrentTarget) return;

        Quaternion originalRot = CurrentTarget.rotation;
        CurrentTarget.Rotate(0f, 90f, 0f, Space.World);

        if (OverlapsOthers(CurrentTarget))
        {
            CurrentTarget.rotation = originalRot;
            if (CurrentTarget.TryGetComponent<Draggable>(out var dragFail))
            {
                dragFail.SetInvalid(true);
                dragFail.SetHighlighted(true);
            }
            Debug.Log("[Rotate] 회전 결과가 다른 오브젝트와 겹쳐서 취소되었습니다.");
            return;
        }

        if (CurrentTarget.TryGetComponent<Draggable>(out var dragOk))
        {
            dragOk.SetInvalid(false);
            dragOk.SetHighlighted(true);
        }

        hasUnsavedChanges = true;
    }
    #endregion

    #region === Raycast / Input Utils ===
    static Vector2 GetPointerScreenPos()
    {
        if (TouchES.activeTouches.Count > 0)
            return TouchES.activeTouches[0].screenPosition;
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    static bool IsPointerDownThisFrame()
    {
        for (int i = 0; i < TouchES.activeTouches.Count; i++)
            if (TouchES.activeTouches[i].phase == UnityEngine.InputSystem.TouchPhase.Began)
                return true;

        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    static bool IsPointerUpThisFrame()
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

    static bool IsPointerMoving()
    {
        for (int i = 0; i < TouchES.activeTouches.Count; i++)
            if (TouchES.activeTouches[i].phase == UnityEngine.InputSystem.TouchPhase.Moved)
                return true;

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return Mouse.current.delta.ReadValue().sqrMagnitude > 0f;

        return false;
    }

    static bool IsPointerOverUI()
    {
        if (!EventSystem.current) return false;

        if (TouchES.activeTouches.Count > 0)
        {
            for (int i = 0; i < TouchES.activeTouches.Count; i++)
            {
                var t = TouchES.activeTouches[i];
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                    t.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                    t.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    if (EventSystem.current.IsPointerOverGameObject(t.touchId))
                        return true;
                }
            }
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    Transform RaycastDraggable(Vector2 screenPos)
    {
        if (!cam) return null;
        Ray ray = cam.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out RaycastHit hit, 1000f, draggableMask) ? hit.transform : null;
    }

    Transform RaycastTransform(Vector2 screenPos)
    {
        if (!cam) return null;
        Ray ray = cam.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out RaycastHit hit, 1000f, ~0) ? hit.transform : null;
    }

    void MaintainOrbitBlockFlag()
    {
        if (!isDragging && BlockOrbit) BlockOrbit = false;
    }
    #endregion
}
