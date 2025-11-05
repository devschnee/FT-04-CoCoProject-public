using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TouchES = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// QuarterView
/// - 피벗(Tracking Target)을 기준으로 카메라 궤도 회전(Yaw/Pitch) + 거리(줌) 제어
/// - PC: 마우스 좌/우 드래그로 회전, 휠로 줌
/// - 모바일: 1손가락 드래그로 회전, 2손가락 핀치로 줌, (편집모드일 때) 2손가락 드래그로 팬
/// - 편집모드일 때 FollowTarget 아래에 임시 피벗을 만들어서 그걸 팬으로 움직인다
/// - 편집모드 빠질 때는 원래 FollowTarget 으로 되돌린다
/// </summary>
[DisallowMultipleComponent]
public class QuarterView : MonoBehaviour
{
    #region === Inspector ===

    [Header("Cinemachine")]
    [Tooltip("CinemachineCamera (예: CM_FollowCamera)")]
    [SerializeField] private CinemachineCamera cm;

    [Header("회전 감도")]
    [SerializeField, Tooltip("좌우(Yaw) 회전 감도 (px -> deg)")]
    private float yawSpeed = 0.6f;
    [SerializeField, Tooltip("상하(Pitch) 회전 감도 (px -> deg)")]
    private float pitchSpeed = 0.4f;

    [Header("줌 감도")]
    [SerializeField, Tooltip("줌 속도 계수")]
    private float zoomSpeed = 0.5f;

    [Header("편집 모드 팬")]
    [SerializeField, Tooltip("픽셀 → 월드 변환 기본 스케일")]
    private float panBaseScale = 0.0025f;
    [SerializeField, Tooltip("팬 속도 가중치")]
    private float panSpeed = 1.0f;
    [SerializeField, Tooltip("마우스/손가락 이동과 반대로 화면이 움직이게 할지 여부")]
    private bool invertEditPan = true;

    [Header("제한 (Limits)")]
    [SerializeField, Tooltip("피벗-카메라 최소 거리")]
    private float minDistance = 6f;
    [SerializeField, Tooltip("피벗-카메라 최대 거리")]
    private float maxDistance = 16f;
    [SerializeField, Tooltip("Pitch 최소 각도")]
    private float minPitch = 5f;
    [SerializeField, Tooltip("Pitch 최대 각도")]
    private float maxPitch = 90f;

    [Header("옵션")]
    [SerializeField, Tooltip("편집모드 컨트롤러를 직접 연결하지 않으면 처음 한 번만 자동 검색")]
    private EditModeController editController;

    #endregion

    #region === Internals ===

    private CinemachineFollow _follow;   // FollowOffset/Target 제어
    private Transform _origFollowTarget; // 편집모드 진입 전 FollowTarget
    private Transform _editPanPivot;     // 편집모드용 임시 피벗

    private float _pitchDeg = 35f;

    // Input
    private InputAction _lookDelta;      // <Pointer>/delta
    private InputAction _primary;        // 좌클릭/탭
    private InputAction _secondary;      // 우클릭
    private InputAction _scrollY;        // 마우스 휠

    private Camera _cam;
    private bool _wasEditMode;
    private bool _lastBlockOrbit;

    // === UI-전역 오비트 차단 (외부 UI가 필요시 호출) ===
    private static int _uiOrbitBlockCount = 0;
    public static bool IsUIOrbitBlocked => _uiOrbitBlockCount > 0;
    public static void PushUIOrbitBlock() { _uiOrbitBlockCount++; }
    public static void PopUIOrbitBlock() { _uiOrbitBlockCount = Mathf.Max(0, _uiOrbitBlockCount - 1); }

    // 캐시: EventSystem/Canvas 존재 여부 (UI 히트 테스트 최적화)
    private bool _hasEventSystem;
    private int _mousePointerId = PointerInputModule.kMouseLeftId; // -1
    private int _touchPointerId = 0; // 첫 번째 터치만 검사

    #endregion

    #region === Const ===

    private const float Z_EPS = 0.3f;    // 카메라 플립 방지 z버퍼
    private const float WHEEL_SCALE = 0.1f;
    private const float PINCH_SCALE = 0.01f;

    #endregion

    #region === Unity Lifecycle ===

    private void Reset()
    {
        if (!cm) cm = GetComponentInChildren<CinemachineCamera>();
    }

    private void Awake()
    {
        if (!cm)
            cm = FindFirstObjectByType<CinemachineCamera>(FindObjectsInactive.Exclude);

        _follow = cm ? cm.GetComponent<CinemachineFollow>() : null;
        _cam = Camera.main;

        if (!_follow)
        {
            Debug.LogError("[QuarterView] CinemachineFollow가 필요합니다. CM 카메라에 CinemachineFollow를 붙이세요.");
            enabled = false;
            return;
        }

        // pitch 초기화
        var o = _follow.FollowOffset;
        _pitchDeg = Mathf.Clamp(Rad2Deg(CurrentPitchRad(in o)), minPitch, maxPitch);

        // 입력 Action 세팅
        _lookDelta = new InputAction(type: InputActionType.PassThrough, binding: "<Pointer>/delta");
        _primary = new InputAction(type: InputActionType.Button, binding: "*/{PrimaryAction}");
        _secondary = new InputAction(type: InputActionType.Button, binding: "*/{SecondaryAction}");
        _scrollY = new InputAction(type: InputActionType.PassThrough, binding: "<Mouse>/scroll/y");

        // EventSystem 캐시(없으면 UI 차단 로직을 생략)
        _hasEventSystem = EventSystem.current != null;

        // EditModeController 캐시(없으면 한 번만 시도)
        if (!editController)
            editController = FindFirstObjectByType<EditModeController>(FindObjectsInactive.Exclude);
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        _lookDelta?.Enable();
        _primary?.Enable();
        _secondary?.Enable();
        _scrollY?.Enable();
    }

    private void OnDisable()
    {
        _lookDelta?.Disable();
        _primary?.Disable();
        _secondary?.Disable();
        _scrollY?.Disable();
        // EnhancedTouchSupport.Disable(); // 전역 기능이라 다른 시스템이 쓸 수 있음: 여기서 끄지 않는다.
    }

    private void OnDestroy()
    {
        _lookDelta?.Dispose();
        _primary?.Dispose();
        _secondary?.Dispose();
        _scrollY?.Dispose();
    }

    private void Update()
    {
        if (!_follow) return;

        SyncEditModePivot();
        HandlePointerInput();
        TrackBlockOrbitChange();
        ClampZBehindTarget();
    }

    #endregion

    #region === 편집모드 피벗 교체 ===

    private bool IsEditModeOn()
    {
        if (!editController)
            editController = FindFirstObjectByType<EditModeController>(FindObjectsInactive.Exclude);
        return editController && editController.IsEditMode;
    }

    private void SyncEditModePivot()
    {
        bool isEdit = IsEditModeOn();
        if (isEdit == _wasEditMode) return;

        _wasEditMode = isEdit;
        if (isEdit) EnterEditPan();
        else ExitEditPan();
    }

    private void EnterEditPan()
    {
        if (_editPanPivot) return;

        _origFollowTarget = _follow.FollowTarget;
        if (!_origFollowTarget)
        {
            Debug.LogWarning("[QuarterView] FollowTarget 이 비어 있어서 편집용 피벗을 만들 수 없습니다.");
            return;
        }

        var go = new GameObject("EditPanPivot (Runtime)");
        _editPanPivot = go.transform;
        _editPanPivot.SetParent(_origFollowTarget, worldPositionStays: false);
        _editPanPivot.localPosition = Vector3.zero;
        _editPanPivot.localRotation = Quaternion.identity;

        // CM.Follow 교체
        cm.Follow = _editPanPivot;
    }

    private void ExitEditPan()
    {
        if (_editPanPivot) _editPanPivot.localPosition = Vector3.zero;
        if (_origFollowTarget) cm.Follow = _origFollowTarget;

        if (_editPanPivot)
            Destroy(_editPanPivot.gameObject);

        _editPanPivot = null;
        _origFollowTarget = null;
    }

    #endregion

    #region === 입력 처리 (PC + 모바일 공통) ===

    private void HandlePointerInput()
    {
        if (IsUIOrbitBlocked) return;

        HandleMouseInput();
        HandleTouchInput();
    }

    private void HandleMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        bool leftPressed = _primary.IsPressed();
        bool rightPressed = _secondary.IsPressed();

        // 팬 (편집모드 + 우클릭 드래그)
        if (_editPanPivot && rightPressed && !IsPointerOverUI())
        {
            Vector2 delta = _lookDelta.ReadValue<Vector2>();
            ApplyPanFromScreenDelta(delta);
        }
        // 회전
        else if ((leftPressed || rightPressed) &&
                 !EditModeController.BlockOrbit &&
                 !IsPointerOverUI())
        {
            Vector2 delta = _lookDelta.ReadValue<Vector2>();
            RotateByDelta(delta);
        }

        // 휠 줌
        float wheel = _scrollY.ReadValue<float>();
        if (Mathf.Abs(wheel) > 0.01f)
            ApplyZoom(-wheel * WHEEL_SCALE);
    }

    private void HandleTouchInput()
    {
        int count = TouchES.activeTouches.Count;
        if (count == 0) return;

        // 1손가락 → 회전
        if (count == 1)
        {
            var t = TouchES.activeTouches[0];
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Moved &&
                !EditModeController.BlockOrbit &&
                !IsPointerOverUI())
            {
                RotateByDelta(t.delta);
            }
        }
        // 2손가락 이상 → 핀치 + (편집모드) 팬
        else
        {
            var t0 = TouchES.activeTouches[0];
            var t1 = TouchES.activeTouches[1];

            Vector2 p0Prev = t0.screenPosition - t0.delta;
            Vector2 p1Prev = t1.screenPosition - t1.delta;
            float prevMag = (p0Prev - p1Prev).magnitude;
            float curMag = (t0.screenPosition - t1.screenPosition).magnitude;
            float pinch = curMag - prevMag;
            ApplyZoom(-pinch * PINCH_SCALE);

            if (_editPanPivot && !IsPointerOverUI())
            {
                Vector2 avgDelta = 0.5f * (t0.delta + t1.delta);
                ApplyPanFromScreenDelta(avgDelta);
            }
        }
    }

    /// <summary>UI 위인지 정확/저비용 판정:
    /// - 마우스: EventSystem.IsPointerOverGameObject()
    /// - 터치:   첫 터치 id 기준
    /// EventSystem 이 없으면 false</summary>
    private bool IsPointerOverUI()
    {
        if (!_hasEventSystem || EventSystem.current == null) return false;

        if (Mouse.current != null)
            return EventSystem.current.IsPointerOverGameObject();

        // 터치 존재 시 첫 포인터 id 사용
        if (TouchES.activeTouches.Count > 0)
            return EventSystem.current.IsPointerOverGameObject(_touchPointerId);

        return false;
    }

    #endregion

    #region === 카메라 연산 ===

    private void ApplyPanFromScreenDelta(Vector2 screenDelta)
    {
        if (!_editPanPivot) return;
        if (!_cam) _cam = Camera.main;

        var o = _follow.FollowOffset;
        float dist = Mathf.Max(1f, CurrentDistance(in o));
        float scale = dist * panBaseScale * panSpeed;

        Vector3 right = _cam.transform.right; right.y = 0f; right.Normalize();
        Vector3 fwd = _cam.transform.forward; fwd.y = 0f; fwd.Normalize();

        float sign = invertEditPan ? -1f : 1f;
        Vector3 worldMove = (right * screenDelta.x + fwd * screenDelta.y) * (scale * sign);

        _editPanPivot.localPosition += worldMove;
    }

    private void RotateByDelta(Vector2 delta)
    {
        // 수평(Yaw)
        transform.Rotate(Vector3.up, delta.x * yawSpeed, Space.World);

        // 수직(Pitch)
        _pitchDeg = Mathf.Clamp(_pitchDeg - delta.y * pitchSpeed, minPitch, maxPitch);
        ApplyPitchToOffset();
    }

    private void ApplyZoom(float delta)
    {
        var o = _follow.FollowOffset;

        float curDist = CurrentDistance(in o);
        float newDist = Mathf.Clamp(curDist + delta * zoomSpeed, minDistance, maxDistance);

        float rad = Deg2Rad(_pitchDeg);
        o.y = Mathf.Sin(rad) * newDist;
        o.z = -Mathf.Cos(rad) * newDist;

        if (o.z > -Z_EPS) o.z = -Z_EPS;

        _follow.FollowOffset = o;

        // 경계 보정 후 pitch 재계산
        _pitchDeg = Mathf.Clamp(Rad2Deg(CurrentPitchRad(in o)), minPitch, maxPitch);
    }

    private void ApplyPitchToOffset()
    {
        var o = _follow.FollowOffset;
        float dist = CurrentDistance(in o);

        float rad = Deg2Rad(_pitchDeg);
        o.y = Mathf.Sin(rad) * dist;
        o.z = -Mathf.Cos(rad) * dist;

        if (o.z > -Z_EPS) o.z = -Z_EPS;

        _follow.FollowOffset = o;
    }

    private void ClampZBehindTarget()
    {
        var o = _follow.FollowOffset;
        if (o.z > -Z_EPS)
        {
            o.z = -Z_EPS;
            _follow.FollowOffset = o;
        }
    }

    #endregion

    #region === Debug Track ===

    private void TrackBlockOrbitChange()
    {
        if (EditModeController.BlockOrbit != _lastBlockOrbit)
            _lastBlockOrbit = EditModeController.BlockOrbit;
    }

    #endregion

    #region === Math Helpers ===

    private static float CurrentDistance(in Vector3 followOffset)
        => Mathf.Sqrt(followOffset.y * followOffset.y + followOffset.z * followOffset.z);

    private static float CurrentPitchRad(in Vector3 followOffset)
        => Mathf.Atan2(followOffset.y, -followOffset.z);

    private static float Deg2Rad(float deg) => deg * Mathf.Deg2Rad;
    private static float Rad2Deg(float rad) => rad * Mathf.Rad2Deg;

    #endregion
}
