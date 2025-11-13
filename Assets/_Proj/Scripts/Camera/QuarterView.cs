using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;
using TouchES = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// QuarterView (재패치 버전, 디버그 제거)
/// - 첫 입력 프레임에서의 0벡터/NaN을 LateUpdate 보정으로 완전히 차단
/// - 입력 처리 전에 안전 상태 검사(IsSafeState)
/// - 입력 그레이스 프레임 기본 4프레임로 상향
/// - 거리/오프셋 하한 이중 보정(연산 전/후)
/// - PC: 좌/우 드래그 회전, 휠 줌 / (편집모드) 우클릭 드래그 팬
/// - 모바일: 1손가락 회전, 2손가락 핀치 줌, (편집모드) 2손가락 드래그 팬
/// </summary>
[DisallowMultipleComponent]
public class QuarterView : MonoBehaviour
{
    #region ===== Inspector =====

    [Header("Cinemachine")]
    [Tooltip("CinemachineCamera (예: CM_FollowCamera)")]
    [SerializeField] private CinemachineCamera cm;

    [Header("회전 감도")]
    [SerializeField, Tooltip("좌우(Yaw) 회전 감도 (px → deg)")]
    private float yawSpeed = 0.6f;
    [SerializeField, Tooltip("상하(Pitch) 회전 감도 (px → deg)")]
    private float pitchSpeed = 0.4f;

    [Header("줌 감도")]
    [SerializeField, Tooltip("줌 속도 계수")]
    private float zoomSpeed = 0.5f;

    [Header("편집모드 팬")]
    [SerializeField, Tooltip("픽셀 → 월드 기본 스케일")] private float panBaseScale = 0.0025f;
    [SerializeField, Tooltip("팬 속도 가중치")] private float panSpeed = 1.0f;
    [SerializeField, Tooltip("드래그 방향 반전")] private bool invertEditPan = true;

    [Header("제한 (Limits)")]
    [SerializeField, Tooltip("피벗-카메라 최소 거리")] private float minDistance = 6f;
    [SerializeField, Tooltip("피벗-카메라 최대 거리")] private float maxDistance = 16f;
    [SerializeField, Tooltip("Pitch 최소 각도")] private float minPitch = 5f;
    [SerializeField, Tooltip("Pitch 최대 각도")] private float maxPitch = 90f;

    [Header("옵션")]
    [SerializeField, Tooltip("지정하지 않으면 최초 1회 자동 검색")] private EditModeController editController;

    [Header("편집모드 팬 제한")]
    [SerializeField] private bool limitEditPan = true;
    [SerializeField, Min(0f)] private float panLimitRadius = 12f;
    [SerializeField] private bool useRectLimit = false;
    [SerializeField] private Vector2 panHalfSize = new Vector2(12f, 12f);

    [Header("안전 옵션")]
    [SerializeField, Tooltip("활성 직후 입력 무시 프레임 수")] private int skipInputFramesOnEnable = 4;

    #endregion

    #region ===== Runtime State =====

    private CinemachineFollow _follow;           // FollowOffset/Target 제어
    private Transform _origFollowTarget;         // 편집모드 전 FollowTarget
    private Transform _editPanPivot;             // 편집모드 임시 피벗
    private Transform _editRotPivot;             // 편집모드 회전 피벗(항상 월드 원점)

    private float _pitchDeg = 35f;
    private Camera _cam;
    private bool _wasEditMode;
    private bool _lastBlockOrbit;
    private bool _hasEventSystem;
    private int _framesToSkip;
    private bool _useTouch = false;

    // 입력 액션
    private InputAction _lookDelta;      // <Pointer>/delta
    private InputAction _primary;        // <Mouse>/leftButton (강제)
    private InputAction _secondary;      // <Mouse>/rightButton (강제)
    private InputAction _scrollY;        // <Mouse>/scroll/y  (강제)

    // 포인터 ID (UI 히트 테스트용)
    private int _touchPointerId = 0; // 첫 번째 터치의 touchId로 갱신

    // 전역 오비트 차단 카운터 (UI에서 호출)
    private static int _uiOrbitBlockCount = 0;
    public static bool IsUIOrbitBlocked => _uiOrbitBlockCount > 0;
    public static void PushUIOrbitBlock() { _uiOrbitBlockCount++; }
    public static void PopUIOrbitBlock() { _uiOrbitBlockCount = Mathf.Max(0, _uiOrbitBlockCount - 1); }

    #endregion

    #region ===== Constants =====

    private const float Z_EPS = 0.3f;     // 카메라 플립 방지 z 버퍼
    private const float WHEEL_SCALE = 0.1f;
    private const float PINCH_SCALE = 0.01f;
    private const float MIN_DIST_EPS = 0.1f; // 거리 절대 하한

    #endregion

    #region ===== Unity Lifecycle =====

    private void Reset()
    {
        if (!cm) cm = GetComponentInChildren<CinemachineCamera>();
    }

    private void Awake()
    {
        EnsureCmAndFollow();
        _cam = Camera.main;

        if (!_follow)
        {
            // CinemachineFollow 필수
            enabled = false;
            return;
        }

        // pitch 초기화
        var o = _follow.FollowOffset;
        _pitchDeg = Mathf.Clamp(Rad2Deg(CurrentPitchRad(in o)), minPitch, maxPitch);

        // 입력 액션 (장치 고정 바인딩) ──────────────
        _lookDelta = new InputAction(type: InputActionType.PassThrough, binding: "<Pointer>/delta");
        _primary = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        _secondary = new InputAction(type: InputActionType.Button, binding: "<Mouse>/rightButton");
        _scrollY = new InputAction(type: InputActionType.PassThrough, binding: "<Mouse>/scroll/y");
        // ─────────────────────────────────────────

        // EventSystem 캐시(없으면 UI 차단 로직 생략)
        _hasEventSystem = EventSystem.current != null;

        // EditModeController 캐시(없으면 한 번만 시도)
        if (!editController)
            editController = FindFirstObjectByType<EditModeController>(FindObjectsInactive.Exclude);
    }

    private void OnEnable()
    {
        // 플랫폼별 터치 시스템
        if (Application.isMobilePlatform)
        {
            EnhancedTouchSupport.Enable();
            TouchSimulation.Enable();
            _useTouch = true;
        }
        else
        {
            EnhancedTouchSupport.Disable();
            TouchSimulation.Disable();
            _useTouch = false;
        }

        EnableInput(true);
        SceneManager.activeSceneChanged += OnSceneChanged;
        _framesToSkip = Mathf.Max(0, skipInputFramesOnEnable);

        HardResetBlocks();
        ValidateAndRepairCamera();
        ForceRebindCamera();
        Invoke(nameof(ForceRebindCamera), 0f);
    }

    private void OnDisable()
    {
        EnableInput(false);
        SceneManager.activeSceneChanged -= OnSceneChanged;
        // EnhancedTouchSupport.Disable(); // 전역 기능: 여기서 끄지 않음
    }

    private void OnDestroy()
    {
        _lookDelta?.Dispose();
        _primary?.Dispose();
        _secondary?.Dispose();
        _scrollY?.Dispose();
    }

    private void OnSceneChanged(Scene from, Scene to)
    {
        // 씬 전환 시 임시 피벗 정리 & 참조 무효화 후 즉시 복구 시도
        ExitEditPan();
        _origFollowTarget = null;
        cm = null; _follow = null; // 다음 프레임에 재획득
        _framesToSkip = 4;         // 몇 프레임 입력 차단

        // 전역 차단 하드 리셋 + 재바인딩
        HardResetBlocks();
        ValidateAndRepairCamera();
        ForceRebindCamera();                  // 즉시 1회
        Invoke(nameof(ForceRebindCamera), 0f); // 다음 프레임 1회
    }

    private void Update()
    {
        // EventSystem는 씬 전환 시 바뀔 수 있으므로 매 프레임 갱신
        _hasEventSystem = EventSystem.current != null;

        if (_framesToSkip-- > 0) { ValidateAndRepairCamera(); return; }

        ValidateAndRepairCamera();
        if (!_follow) return;

        SyncEditModePivot();

        HandlePointerInput(); // 안전 분기는 함수 내부에서 판단

        TrackBlockOrbitChange();
        ClampZBehindTarget();
    }

    // 같은 프레임에 CinemachineBrain(LateUpdate)이 읽기 전에 한 번 더 보정
    private void LateUpdate()
    {
        ValidateAndRepairCamera();
    }

    #endregion

    #region ===== Edit Mode Pivot (생성/복구) =====

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
        if (!_origFollowTarget) return;

        // 회전 피벗: 월드 원점(0,0,0) 기준 회전
        if (!_editRotPivot)
        {
            var rotGo = new GameObject("EditRotPivot (Runtime)");
            _editRotPivot = rotGo.transform;
            _editRotPivot.SetParent(null, worldPositionStays: false);
            _editRotPivot.position = Vector3.zero;        // (0,0,0) 고정
            _editRotPivot.rotation = Quaternion.identity;
        }

        // 팬 피벗: 회전 피벗의 자식(여기만 이동시켜 화면을 옮김)
        var panGo = new GameObject("EditPanPivot (Runtime)");
        _editPanPivot = panGo.transform;
        _editPanPivot.SetParent(_editRotPivot, worldPositionStays: false);

        // 시작 위치/회전은 원래 타깃 기준으로 맞춰줌(시각적 튐 방지)
        _editPanPivot.localPosition = Vector3.zero;
        _editPanPivot.localRotation = Quaternion.identity;

        // 카메라는 팬 피벗을 따라다님
        cm.Follow = _editPanPivot;
    }

    private void ExitEditPan()
    {
        if (_editPanPivot) Destroy(_editPanPivot.gameObject);
        _editPanPivot = null;

        if (_editRotPivot) Destroy(_editRotPivot.gameObject);
        _editRotPivot = null;

        if (_origFollowTarget) cm.Follow = _origFollowTarget;
        _origFollowTarget = null;
    }

    #endregion

    #region ===== Input Handling (PC & Mobile) =====

    private void HandlePointerInput()
    {
        if (IsUIOrbitBlocked) return;
        if (!IsSafeState()) return;

        HandleMouseInput();
        if (_useTouch) HandleTouchInput(); // 모바일에서만 터치 경로 사용
    }

    private void HandleMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // 강제 장치 바인딩 기반
        bool leftPressed = _primary.IsPressed() || mouse.leftButton.isPressed;
        bool rightPressed = _secondary.IsPressed() || mouse.rightButton.isPressed;

        // 팬: 편집모드 + 우클릭 드래그
        if (_editPanPivot && rightPressed && !IsPointerOverUI())
        {
            Vector2 delta = _lookDelta.ReadValue<Vector2>();
            if (delta == Vector2.zero) delta = mouse.delta.ReadValue(); // 폴백
            ApplyPanFromScreenDelta(delta);
        }
        // 회전
        else if ((leftPressed || rightPressed) && !EditModeController.BlockOrbit && !IsPointerOverUI())
        {
            Vector2 delta = _lookDelta.ReadValue<Vector2>();
            if (delta == Vector2.zero) delta = mouse.delta.ReadValue(); // 폴백
            RotateByDelta(delta);
        }

        // 줌: 액션값 → 폴백 순
        float wheel = _scrollY.ReadValue<float>();
        if (Mathf.Approximately(wheel, 0f))
            wheel = mouse.scroll.ReadValue().y;

        if (Mathf.Abs(wheel) > 0.01f)
            ApplyZoom(-wheel * WHEEL_SCALE);
    }

    private void HandleTouchInput()
    {
        int count = TouchES.activeTouches.Count;
        if (count == 0) return;

        // 첫 터치의 touchId로 포인터ID 최신화 (UI 히트 정확도)
        _touchPointerId = TouchES.activeTouches[0].touchId;

        // 1손가락 → 회전
        if (count == 1)
        {
            var t = TouchES.activeTouches[0];
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Moved && !EditModeController.BlockOrbit && !IsPointerOverUI())
            {
                RotateByDelta(t.delta);
            }
        }
        // 2손가락 이상 → 핀치 줌 + (편집모드) 팬
        else
        {
            var t0 = TouchES.activeTouches[0];
            var t1 = TouchES.activeTouches[1];

            Vector2 p0Prev = t0.screenPosition - t0.delta;
            Vector2 p1Prev = t1.screenPosition - t1.delta;
            float prevMag = (p0Prev - p1Prev).magnitude;
            float curMag = (t0.screenPosition - t1.screenPosition).magnitude;
            float pinch = curMag - prevMag;

            if (Mathf.Abs(pinch) > 0.01f)
                ApplyZoom(-pinch * PINCH_SCALE);

            if (_editPanPivot && !IsPointerOverUI())
            {
                Vector2 avgDelta = 0.5f * (t0.delta + t1.delta);
                ApplyPanFromScreenDelta(avgDelta);
            }
        }
    }

    /// <summary>
    /// 현재 포인터가 UI 위인지 판정.
    /// - 마우스: EventSystem.IsPointerOverGameObject()
    /// - 터치:   포인터ID 의존 대신 실제 화면 좌표로 UI Raycast (신 InputSystem/EnhancedTouch 호환)
    /// - EventSystem 없으면 false
    /// </summary>
    private bool IsPointerOverUI()
    {
        if (!_hasEventSystem || EventSystem.current == null) return false;

        // 마우스
        if (Mouse.current != null && Mouse.current.wasUpdatedThisFrame)
            return EventSystem.current.IsPointerOverGameObject();

        // 터치: 좌표 기반 레이케스트로 판정 (pointerId 불일치 이슈 회피)
        if (TouchES.activeTouches.Count > 0)
        {
            var pos = TouchES.activeTouches[0].screenPosition;
            return UIRaycastAt(pos);
        }

        return false;
    }

    // 실제 좌표로 UI 레이캐스트
    private static readonly List<RaycastResult> _uiHits = new List<RaycastResult>(8);
    private bool UIRaycastAt(Vector2 screenPos)
    {
        var es = EventSystem.current;
        if (es == null) return false;

        var ped = new PointerEventData(es) { position = screenPos };
        _uiHits.Clear();
        es.RaycastAll(ped, _uiHits);
        return _uiHits.Count > 0;
    }

    #endregion

    #region ===== Camera Ops (회전/줌/팬/클램프) =====

    private void ApplyPanFromScreenDelta(Vector2 screenDelta)
    {
        if (!_editPanPivot) return;
        if (!_cam) _cam = Camera.main;

        if (!Finite(_editPanPivot.position))
            _editPanPivot.position = Vector3.zero;
        if (!Finite(_cam.transform.position) || !Finite(_cam.transform.rotation))
        {
            _cam.transform.position = Vector3.zero;
            _cam.transform.rotation = Quaternion.identity;
        }

        var o = _follow.FollowOffset;
        float dist = Mathf.Max(1f, CurrentDistance(in o));
        float scale = dist * panBaseScale * panSpeed;

        Vector3 right = _cam.transform.right; right.y = 0f;
        Vector3 fwd = _cam.transform.forward; fwd.y = 0f;
        if (!Finite(right)) right = Vector3.right;
        if (!Finite(fwd)) fwd = Vector3.forward;
        right.Normalize(); fwd.Normalize();

        float sign = invertEditPan ? -1f : 1f;
        Vector3 worldMove = (right * screenDelta.x + fwd * screenDelta.y) * (scale * sign);

        Vector3 targetPos = _editPanPivot.position + worldMove;
        _editPanPivot.position = ClampEditPanWorld(targetPos);
    }

    private void RotateByDelta(Vector2 delta)
    {
        // 수평(Yaw): 편집모드면 _editRotPivot(원점 기준)만 회전
        if (_editRotPivot)    // 편집모드
        {
            _editRotPivot.Rotate(Vector3.up, delta.x * yawSpeed, Space.World);
        }
        else                  // 일반 모드: 기존대로 실제 피벗 회전
        {
            var pivot = _follow ? _follow.FollowTarget : null;
            if (pivot) pivot.Rotate(Vector3.up, delta.x * yawSpeed, Space.World);
            else transform.Rotate(Vector3.up, delta.x * yawSpeed, Space.World);
        }

        // 수직(Pitch): 동일 (오프셋 기반 피치 유지)
        _pitchDeg = Mathf.Clamp(_pitchDeg - delta.y * pitchSpeed, minPitch, maxPitch);
        ApplyPitchToOffset();
    }

    private void ApplyZoom(float delta)
    {
        if (!_follow) return;

        var o = _follow.FollowOffset;

        // 연산 전 하한 보정
        o = GuardOffset(o);
        float curDist = Mathf.Max(MIN_DIST_EPS, CurrentDistance(in o));
        float newDist = Mathf.Clamp(curDist + delta * zoomSpeed, Mathf.Max(MIN_DIST_EPS, minDistance), maxDistance);

        float rad = Deg2Rad(_pitchDeg);
        o.y = Mathf.Sin(rad) * newDist;
        o.z = -Mathf.Cos(rad) * newDist;

        // 연산 후 하한 보정
        o = GuardOffset(o);

        _follow.FollowOffset = o;

        // 보정 후 pitch 재계산
        _pitchDeg = Mathf.Clamp(Rad2Deg(CurrentPitchRad(in o)), minPitch, maxPitch);
    }

    private void ApplyPitchToOffset()
    {
        if (!_follow) return;
        var o = GuardOffset(_follow.FollowOffset); // 연산 전 보정
        float dist = Mathf.Max(MIN_DIST_EPS, CurrentDistance(in o));

        float rad = Deg2Rad(_pitchDeg);
        o.y = Mathf.Sin(rad) * dist;
        o.z = -Mathf.Cos(rad) * dist;

        o = GuardOffset(o); // 연산 후 보정

        _follow.FollowOffset = o;
    }

    private void ClampZBehindTarget()
    {
        if (!_follow) return;
        var o = _follow.FollowOffset;
        if (o.z > -Z_EPS)
        {
            o.z = -Z_EPS;
            _follow.FollowOffset = o;
        }
    }

    private Vector3 GuardOffset(Vector3 o)
    {
        if (!Finite(o)) o = new Vector3(0f, 8f, -10f);
        float d = Mathf.Max(MIN_DIST_EPS, CurrentDistance(in o));
        if (d < MIN_DIST_EPS) { o.y = 8f; o.z = -10f; }
        if (o.z > -Z_EPS) o.z = -Z_EPS;
        return o;
    }

    private Vector3 ClampEditPanWorld(Vector3 desiredWorldPos)
    {
        // 편집모드에서는 회전 기준을 (0,0,0)로 고정
        bool useOrigin = _editRotPivot != null;

        Vector3 center;
        if (useOrigin)
        {
            center = Vector3.zero;
        }
        else
        {
            center = (_origFollowTarget && Finite(_origFollowTarget.position))
                ? _origFollowTarget.position
                : Vector3.zero;
        }

        float y = center.y;

        if (!limitEditPan)
            return new Vector3(desiredWorldPos.x, y, desiredWorldPos.z);

        Vector3 offset = desiredWorldPos - center;
        offset.y = 0f;

        if (useRectLimit)
        {
            float clampedX = Mathf.Clamp(offset.x, -panHalfSize.x, panHalfSize.x);
            float clampedZ = Mathf.Clamp(offset.z, -panHalfSize.y, panHalfSize.y);
            return new Vector3(center.x + clampedX, y, center.z + clampedZ);
        }
        else
        {
            float mag = offset.magnitude;
            if (mag <= panLimitRadius)
                return new Vector3(desiredWorldPos.x, y, desiredWorldPos.z);

            Vector3 clamped = offset.normalized * panLimitRadius;
            return new Vector3(center.x + clamped.x, y, center.z + clamped.z);
        }
    }

    #endregion

    #region ===== Debug/Tracking =====

    private void TrackBlockOrbitChange()
    {
        if (EditModeController.BlockOrbit != _lastBlockOrbit)
            _lastBlockOrbit = EditModeController.BlockOrbit;
    }

    #endregion

    #region ===== Safety: NaN/Null 자가 복구 =====

    private static bool Finite(float f) => !(float.IsNaN(f) || float.IsInfinity(f));
    private static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
    private static bool Finite(Quaternion q) => Finite(q.x) && Finite(q.y) && Finite(q.z) && Finite(q.w);

    private bool IsSafeState()
    {
        if (!cm) return false;
        if (!_follow) return false;
        if (!_follow.FollowTarget || !_follow.FollowTarget.gameObject) return false;
        if (!Finite(transform.position) || !Finite(transform.rotation)) return false;
        var o = _follow.FollowOffset;
        if (!Finite(o)) return false;
        if (CurrentDistance(in o) < MIN_DIST_EPS) return false;
        if (o.z > -Z_EPS) return false;
        return true;
    }

    private void ValidateAndRepairCamera()
    {
        // cm/_follow 재획득
        EnsureCmAndFollow();

        // 카메라 참조
        if (!_cam) _cam = Camera.main;

        // Follow 타깃 복구
        if (_follow)
        {
            if (!_follow.FollowTarget || !_follow.FollowTarget.gameObject)
            {
                // 기본 피벗 찾기 → 없으면 자기 자신
                Transform fallback = FindFallbackPivot();
                if (cm) cm.Follow = fallback;
                _origFollowTarget = fallback;
            }

            // 오프셋 NaN 복구 & 거리 하한 보장
            var o = _follow.FollowOffset;
            if (!Finite(o))
            {
                o = new Vector3(0f, 8f, -10f);
                _follow.FollowOffset = o;
            }

            o = GuardOffset(_follow.FollowOffset);
            _follow.FollowOffset = o;

            // 보정 후 pitch 재계산
            _pitchDeg = Mathf.Clamp(Rad2Deg(CurrentPitchRad(in o)), minPitch, maxPitch);
        }

        // 트랜스폼 NaN 복구
        if (!Finite(transform.position)) transform.position = Vector3.zero;
        if (!Finite(transform.rotation)) transform.rotation = Quaternion.identity;
    }

    #endregion

    #region ===== Math Helpers =====

    private static float CurrentDistance(in Vector3 followOffset)
        => Mathf.Sqrt(followOffset.y * followOffset.y + followOffset.z * followOffset.z);

    private static float CurrentPitchRad(in Vector3 followOffset)
        => Mathf.Atan2(followOffset.y, -followOffset.z);

    private static float Deg2Rad(float deg) => deg * Mathf.Deg2Rad;
    private static float Rad2Deg(float rad) => rad * Mathf.Rad2Deg;

    #endregion

    #region ===== Small Utilities (Refactor only) =====

    private void EnableInput(bool enable)
    {
        if (enable)
        {
            _lookDelta?.Enable();
            _primary?.Enable();
            _secondary?.Enable();
            _scrollY?.Enable();
        }
        else
        {
            _lookDelta?.Disable();
            _primary?.Disable();
            _secondary?.Disable();
            _scrollY?.Disable();
        }
    }

    private void EnsureCmAndFollow()
    {
        // 1) 이미 참조가 있으면 그대로
        if (cm && _follow) return;

        // 2) 씬 내에서 CinemachineFollow 붙은 카메라를 우선 탐색
        var follow = FindFirstObjectByType<CinemachineFollow>(FindObjectsInactive.Exclude);
        if (follow)
        {
            _follow = follow;
            cm = follow.GetComponent<CinemachineCamera>();
            if (cm) return;
        }

        // 3) 그 다음 아무 CinemachineCamera나
        if (!cm)
            cm = FindFirstObjectByType<CinemachineCamera>(FindObjectsInactive.Exclude);

        // 4) cm가 있고 Follow가 없으면 동적 추가
        if (cm && !_follow)
            _follow = cm.gameObject.GetComponent<CinemachineFollow>() ?? cm.gameObject.AddComponent<CinemachineFollow>();
    }

    // 전역 차단 하드 리셋
    private void HardResetBlocks()
    {
        _uiOrbitBlockCount = 0;
        try { EditModeController.BlockOrbit = false; } catch { /* static 없으면 무시 */ }
    }

    // 명시적 피벗 탐색(로비/인게임 구분 이름 지원)
    private Transform FindFallbackPivot()
    {
        string[] names = { "LobbyCameraPivot", "GameCameraPivot", "CameraPivot" };
        foreach (var n in names)
        {
            var t = GameObject.Find(n)?.transform;
            if (t) return t;
        }
        return this.transform;
    }

    // 강제 재바인딩(즉시/다음 프레임용)
    private void ForceRebindCamera()
    {
        EnsureCmAndFollow();
        if (_follow)
        {
            if (!_follow.FollowTarget || !_follow.FollowTarget.gameObject)
            {
                var fallback = FindFallbackPivot();
                cm.Follow = fallback;
                _origFollowTarget = fallback;
            }

            var o = GuardOffset(_follow.FollowOffset);
            _follow.FollowOffset = o;
            _pitchDeg = Mathf.Clamp(Rad2Deg(CurrentPitchRad(in o)), minPitch, maxPitch);
        }
    }

    #endregion
}
