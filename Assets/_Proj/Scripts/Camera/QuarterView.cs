using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TouchES = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// QuarterView
/// - 피벗(Tracking Target)을 기준으로 카메라의 궤도 회전(Yaw/Pitch) + 거리(줌)를 제어.
/// - 마우스/한 손가락 드래그: 회전, 마우스 휠/핀치: 줌.
/// - 편집모드일 때: 우클릭 드래그(PC) / 두 손가락 동일방향 드래그(모바일) → 화면 팬(Pan).
/// - 편집모드 종료 시: 팬 오프셋 원복(회전/줌은 유지).
/// - CinemachineCamera + CinemachineFollow 조합에서 FollowOffset(y,z)만 조정.
/// </summary>
public class QuarterView : MonoBehaviour
{
    #region === Inspector ===
    [Header("Cinemachine")]
    [Tooltip("CM_FollowCamera (CinemachineCamera 컴포넌트)")]
    [SerializeField] private CinemachineCamera cm;

    [Header("감도 (Sensitivity)")]
    [Tooltip("좌우 회전 감도 (px -> deg)")]
    [SerializeField] private float yawSpeed = 0.6f;
    [Tooltip("상하 회전 감도 (px -> deg)")]
    [SerializeField] private float pitchSpeed = 0.4f;
    [Tooltip("줌 감도 (scale)")]
    [SerializeField] private float zoomSpeed = 0.5f;

    [Header("편집 모드 팬 감도")]
    [Tooltip("픽셀 → 월드 변환 기본 스케일. (거리 * 이 값)만큼 이동)")]
    [SerializeField] private float panBaseScale = 0.0025f;
    [Tooltip("두 손가락 드래그/우클릭 드래그의 속도 가중치")]
    [SerializeField] private float panSpeed = 1.0f;
    [SerializeField, Tooltip("편집모드 팬 방향 반전(손/마우스 이동과 반대로 화면이 움직이게)")]
    private bool invertEditPan = true;

    [Header("제한 (Limits)")]
    [Tooltip("피벗-카메라 거리 범위")]
    [SerializeField] private float minDistance = 6f;
    [SerializeField] private float maxDistance = 16f;
    [Tooltip("피치 각도 제한(도)")]
    [SerializeField] private float minPitch = 5f;
    [SerializeField] private float maxPitch = 90f;
    #endregion

    #region === Internals ===
    private CinemachineFollow _follow;       // FollowOffset, FollowTarget 제어 대상
    private Transform _origFollowTarget;     // 편집모드 진입 전 원래 타깃
    private Transform _editPanPivot;         // 편집 전용 피벗(팬 이동을 이 로컬좌표로 누적)
    private float _pitchDeg = 35f;           // 현재 피치(도)
    private bool _lastBlockOrbit;            // Debug용 상태 추적
    private bool _wasEditMode;               // 편집모드 상태 변화 감지
    private Camera _cam;                     // 화면 벡터 → 월드 투영용
    #endregion

    #region === Constants ===
    private const float Z_EPS = 0.3f;        // z>=0 방지(카메라 플립 방지용 버퍼)
    private const float WHEEL_SCALE = 0.1f;
    private const float PINCH_SCALE = 0.01f;
    #endregion

    #region === Input Actions ===
    private InputAction _lookDelta;      // <Pointer>/delta
    private InputAction _primary;        // */{PrimaryAction} (좌클릭/탭)
    private InputAction _secondary;      // */{SecondaryAction} (우클릭)
    private InputAction _scrollY;        // <Mouse>/scroll/y
    #endregion

    #region === Unity Lifecycle ===
    private void Awake()
    {
        if (!cm) cm = FindFirstObjectByType<CinemachineCamera>();
        _follow = cm ? cm.GetComponent<CinemachineFollow>() : null;
        _cam = Camera.main;

        if (!_follow)
        {
            Debug.LogError("[QuarterView] CinemachineFollow가 필요합니다. CM 카메라에 CinemachineFollow를 추가하세요.");
            enabled = false;
            return;
        }

        // 현재 FollowOffset에서 피치 초기화
        var o = _follow.FollowOffset;
        _pitchDeg = Mathf.Clamp(Rad2Deg(CurrentPitchRad(in o)), minPitch, maxPitch);

        // 입력 바인딩
        _lookDelta = new InputAction(type: InputActionType.PassThrough, binding: "<Pointer>/delta");
        _primary = new InputAction(type: InputActionType.Button, binding: "*/{PrimaryAction}");
        _secondary = new InputAction(type: InputActionType.Button, binding: "*/{SecondaryAction}");
        _scrollY = new InputAction(type: InputActionType.PassThrough, binding: "<Mouse>/scroll/y");
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
        EnhancedTouchSupport.Disable();
        _lookDelta?.Disable();
        _primary?.Disable();
        _secondary?.Disable();
        _scrollY?.Disable();
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

        SyncEditModePivot();      // 편집모드 전용 피벗 진입/종료 처리
        HandlePointerInput();     // 마우스/터치 통합 입력 (회전/줌/팬)
        TrackBlockOrbitChange();  // 상태 추적 (디버그용)
        ClampZBehindTarget();     // z 안전 보정(플립 방지)
    }
    #endregion

    #region === EditMode Pivot ===
    private void SyncEditModePivot()
    {
        bool isEdit = EditModeControllerExistsAndTrue();

        if (isEdit == _wasEditMode) return;
        _wasEditMode = isEdit;

        if (isEdit)
            EnterEditPan();
        else
            ExitEditPan();
    }

    private bool EditModeControllerExistsAndTrue()
    {
        // EditModeController.IsEditMode (public) 를 폴링
        var emc = FindFirstObjectByType<EditModeController>();
        return emc != null && emc.IsEditMode;
    }

    private void EnterEditPan()
    {
        // 이미 준비되어 있으면 무시
        if (_editPanPivot != null) return;

        // 원래 타깃 보관 후, 그 자식에 편집 피벗 생성
        _origFollowTarget = _follow.FollowTarget;
        if (_origFollowTarget == null)
        {
            Debug.LogWarning("[QuarterView] FollowTarget이 비어 있어 편집모드 팬 피벗을 만들 수 없습니다.");
            return;
        }

        var go = new GameObject("EditPanPivot (Runtime)");
        _editPanPivot = go.transform;
        _editPanPivot.SetParent(_origFollowTarget, worldPositionStays: false);
        _editPanPivot.localPosition = Vector3.zero;
        _editPanPivot.localRotation = Quaternion.identity;

        // 편집 동안에는 피벗을 이 가짜 타깃으로 교체
        cm.Follow = _editPanPivot;

    }

    private void ExitEditPan()
    {
        if (_editPanPivot)
            _editPanPivot.localPosition = Vector3.zero;

        if (_origFollowTarget)
            cm.Follow = _origFollowTarget;   // ✅ 원복

        if (_editPanPivot)
            Destroy(_editPanPivot.gameObject);
        _editPanPivot = null;
        _origFollowTarget = null;
    }

    #endregion

    #region === Input ===
    private void HandlePointerInput()
    {
        HandleMouseInput();
        HandleTouchInput();
    }

    /// <summary>PC: 좌/우 버튼 드래그(회전/팬), 휠(줌)</summary>
    private void HandleMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        bool leftPressed = _primary.IsPressed();
        bool rightPressed = _secondary.IsPressed();

        // === 팬: 편집모드 + 우클릭 드래그 ===
        if (_editPanPivot && rightPressed && !IsPointerOverUI())
        {
            Vector2 delta = _lookDelta.ReadValue<Vector2>();
            ApplyPanFromScreenDelta(delta);
        }
        // === 회전: (우클릭이 팬으로 소비되지 않는 경우) ===
        else if ((leftPressed || rightPressed) && !EditModeController.BlockOrbit && !IsPointerOverUI())
        {
            Vector2 delta = _lookDelta.ReadValue<Vector2>();
            RotateByDelta(delta);
        }

        // === 휠 줌 ===
        float wheel = _scrollY.ReadValue<float>();
        if (Mathf.Abs(wheel) > 0.01f)
        {
            ApplyZoom(-wheel * WHEEL_SCALE);
        }
    }

    /// <summary>모바일: 1손가락 회전, 2손가락 핀치 줌 + (편집모드일 때) 평균 이동으로 팬</summary>
    private void HandleTouchInput()
    {
        int count = TouchES.activeTouches.Count;
        if (count == 0) return;

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
        else // 2손가락 이상
        {
            var t0 = TouchES.activeTouches[0];
            var t1 = TouchES.activeTouches[1];

            // --- 핀치(줌) ---
            Vector2 p0Prev = t0.screenPosition - t0.delta;
            Vector2 p1Prev = t1.screenPosition - t1.delta;
            float prevMag = (p0Prev - p1Prev).magnitude;
            float curMag = (t0.screenPosition - t1.screenPosition).magnitude;
            float pinch = curMag - prevMag;
            ApplyZoom(-pinch * PINCH_SCALE);

            // --- 팬(편집모드에서만) ---
            if (_editPanPivot && !IsPointerOverUI())
            {
                // 두 손가락 평균 이동 벡터 = 화면 평면의 "번역"
                Vector2 avgDelta = 0.5f * (t0.delta + t1.delta);
                ApplyPanFromScreenDelta(avgDelta);
            }
        }
    }

    /// <summary>UI 위 입력 차단</summary>
    private static bool IsPointerOverUI()
    {
        if (!EventSystem.current) return false;

        // 현재 포인터 스크린 좌표 (마우스 우선, 그 외 터치 첫 포인트)
        Vector2 pos;
        if (Mouse.current != null)
            pos = Mouse.current.position.ReadValue();
        else if (TouchES.activeTouches.Count > 0)
            pos = TouchES.activeTouches[0].screenPosition;
        else
            return false;

        var data = new PointerEventData(EventSystem.current) { position = pos };
        var results = new System.Collections.Generic.List<RaycastResult>(8);
        EventSystem.current.RaycastAll(data, results);
        return results.Count > 0;
    }
    #endregion

    #region === Camera Ops ===
    private void RotateByDelta(Vector2 delta)
    {
        // Yaw(수평 회전): 월드 Y축 기준
        transform.Rotate(Vector3.up, delta.x * yawSpeed, Space.World);

        // Pitch(수직 회전): 내부 각도 갱신 → FollowOffset 재계산
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

        if (o.z > -Z_EPS) o.z = -Z_EPS; // 플립 방지
        _follow.FollowOffset = o;

        // 경계 보정 반영(수치 일관성 유지)
        _pitchDeg = Mathf.Clamp(Rad2Deg(CurrentPitchRad(in o)), minPitch, maxPitch);
    }

    /// <summary>편집 피벗을 화면 드래그 벡터에 맞춰 XZ 평면으로 이동</summary>
    private void ApplyPanFromScreenDelta(Vector2 screenDelta)
    {
        if (_editPanPivot == null) return;
        if (_cam == null) _cam = Camera.main;

        // 현재 카메라-피벗 거리로 스케일 보정
        var o = _follow.FollowOffset;
        float dist = Mathf.Max(1f, CurrentDistance(in o)); // 안전 가드
        float scale = dist * panBaseScale * panSpeed;

        // 화면 X/Y → 카메라 기준 Right/Forward 평탄화(XZ)
        Vector3 right = _cam.transform.right; right.y = 0f; right.Normalize();
        Vector3 fwd = _cam.transform.forward; fwd.y = 0f; fwd.Normalize();

        // 화면 y는 위가 +, 월드 forward는 보통 +z이므로 같은 방향으로 매핑
        float sign = invertEditPan ? -1f : 1f; // ← 반전 스위치
        Vector3 worldMove = (right * screenDelta.x + fwd * screenDelta.y) * (scale * sign);
        _editPanPivot.localPosition += worldMove;

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
