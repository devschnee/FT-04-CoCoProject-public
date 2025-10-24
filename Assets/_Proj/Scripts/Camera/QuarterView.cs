using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TouchES = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// QuarterView
/// - 피벗(Tracking Target)을 중심으로 카메라 궤도 회전(Yaw/Pitch) + 거리(줌)를 제어.
/// - 마우스 드래그/한 손가락 드래그: 회전, 휠/핀치: 줌.
/// - 오브젝트 드래그 중(EditModeController.BlockOrbit)에는 "회전"만 차단(줌은 허용).
/// - FollowOffset(y,z)만 다루므로 CinemachineFollow가 필요.
/// </summary>
public class QuarterView : MonoBehaviour
{
    #region === Serialized Config ===
    [Header("Cinemachine")]
    [Tooltip("CM_FollowCamera")]
    public CinemachineCamera cm;                 // Inspector 할당 권장

    [Header("Sensitivity")]
    [Tooltip("좌우 회전 감도 (px -> deg)")]
    public float yawSpeed = 0.6f;
    [Tooltip("상하 회전 감도 (px -> deg)")]
    public float pitchSpeed = 0.4f;
    [Tooltip("줌 감도 (scalar)")]
    public float zoomSpeed = 0.5f;

    [Header("Limits")]
    [Tooltip("피벗-카메라 거리 범위")]
    public float minDistance = 6f;
    public float maxDistance = 16f;
    [Tooltip("피치 각도 제한(도)")]
    public float minPitch = 5f;
    public float maxPitch = 90f;
    #endregion

    #region === Internals / State ===
    private CinemachineFollow follow;            // FollowOffset 제어 대상
    private float pitchDeg = 35f;                // 현재 피치(도)
    private bool _lastBlockOrbit = false;        // 디버깅 추적용(기능적 의미 X)
    #endregion

    #region === Constants ===
    private const float Z_EPS = 0.3f;            // z>=0 방지(카메라 플립 방지)
    private const float WHEEL_SCALE = 0.1f;      // 마우스 휠 배율
    private const float PINCH_SCALE = 0.01f;     // 핀치 배율
    #endregion

    #region === Input Actions ===
    private InputAction lookDelta;               // <Pointer>/delta
    private InputAction primary;                 // */{PrimaryAction}
    private InputAction secondary;               // */{SecondaryAction}
    private InputAction scrollY;                 // <Mouse>/scroll/y
    #endregion

    #region === Unity Lifecycle ===
    private void Awake()
    {
        // Cinemachine 준비
        if (!cm) cm = FindFirstObjectByType<CinemachineCamera>();
        follow = cm ? cm.GetComponent<CinemachineFollow>() : null;

        if (!follow)
        {
            Debug.LogError("[QuarterView] CinemachineFollow가 필요합니다. CM 카메라에 CinemachineFollow를 추가하세요.");
            enabled = false;
            return;
        }

        // 현재 FollowOffset에서 피치 초기화
        var o = follow.FollowOffset;
        pitchDeg = Mathf.Clamp(Rad2Deg(CurrentPitchRad(o)), minPitch, maxPitch);

        // 입력 바인딩 생성
        lookDelta = new InputAction(type: InputActionType.PassThrough, binding: "<Pointer>/delta");
        primary = new InputAction(type: InputActionType.Button, binding: "*/{PrimaryAction}");
        secondary = new InputAction(type: InputActionType.Button, binding: "*/{SecondaryAction}");
        scrollY = new InputAction(type: InputActionType.PassThrough, binding: "<Mouse>/scroll/y");
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        lookDelta?.Enable();
        primary?.Enable();
        secondary?.Enable();
        scrollY?.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        lookDelta?.Disable();
        primary?.Disable();
        secondary?.Disable();
        scrollY?.Disable();
    }

    private void OnDestroy()
    {
        lookDelta?.Dispose();
        primary?.Dispose();
        secondary?.Dispose();
        scrollY?.Dispose();
    }

    private void Update()
    {
        if (!follow) return;

        HandlePointerInput();      // 마우스/터치 입력 모두 처리
        TrackBlockOrbitChange();   // 디버깅/동기화용
        ClampZBehindTarget();      // 플립 방지
    }
    #endregion

    #region === Input Dispatcher ===
    /// <summary>
    /// 마우스/터치 입력을 한 곳에서 분기 처리.
    /// </summary>
    private void HandlePointerInput()
    {
        HandleMouseInput();
        HandleTouchInput();
    }

    /// <summary>
    /// 마우스 입력:
    /// - 드래그(좌/우 버튼): 회전 (단, 드래그 중엔 차단)
    /// - 휠: 줌
    /// </summary>
    private void HandleMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        bool pressed = primary.IsPressed() || secondary.IsPressed();
        if (pressed && !EditModeController.BlockOrbit)
        {
            Vector2 delta = lookDelta.ReadValue<Vector2>();
            RotateByDelta(delta);
        }

        float wheel = scrollY.ReadValue<float>();
        if (Mathf.Abs(wheel) > 0.01f)
            ApplyZoom(-wheel * WHEEL_SCALE);
    }

    /// <summary>
    /// 터치 입력:
    /// - 1손가락 드래그: 회전 (단, 드래그 중엔 차단)
    /// - 2손가락 이상: 핀치 줌
    /// </summary>
    private void HandleTouchInput()
    {
        int count = TouchES.activeTouches.Count;
        if (count == 0) return;

        if (count == 1)
        {
            var t = TouchES.activeTouches[0];
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Moved && !EditModeController.BlockOrbit)
                RotateByDelta(t.delta);
        }
        else // 2손가락 이상 → 핀치 줌
        {
            var t0 = TouchES.activeTouches[0];
            var t1 = TouchES.activeTouches[1];

            Vector2 p0Prev = t0.screenPosition - t0.delta;
            Vector2 p1Prev = t1.screenPosition - t1.delta;

            float prevMag = (p0Prev - p1Prev).magnitude;
            float curMag = (t0.screenPosition - t1.screenPosition).magnitude;
            float pinch = curMag - prevMag;

            ApplyZoom(-pinch * PINCH_SCALE);
        }
    }

    /// <summary>
    /// BlockOrbit 플래그 변경 추적(동작 변경은 없고, 상태 동기화 목적).
    /// </summary>
    private void TrackBlockOrbitChange()
    {
        if (EditModeController.BlockOrbit != _lastBlockOrbit)
            _lastBlockOrbit = EditModeController.BlockOrbit;
    }
    #endregion

    #region === Camera Ops ===
    /// <summary>드래그 델타를 야우/피치로 변환하여 적용.</summary>
    private void RotateByDelta(Vector2 delta)
    {
        // Yaw: 월드 기준 Y 회전(피벗 부모에 적용)
        transform.Rotate(Vector3.up, delta.x * yawSpeed, Space.World);

        // Pitch: 내부 각도 갱신 후 FollowOffset(y,z) 재계산
        pitchDeg = Mathf.Clamp(pitchDeg - delta.y * pitchSpeed, minPitch, maxPitch);
        ApplyPitchToOffset();
    }

    /// <summary>줌(거리만 변경, 현재 피치는 유지).</summary>
    private void ApplyZoom(float delta)
    {
        var o = follow.FollowOffset;
        float curDist = CurrentDistance(o);
        float newDist = Mathf.Clamp(curDist + delta * zoomSpeed, minDistance, maxDistance);

        float rad = Deg2Rad(pitchDeg);
        o.y = Mathf.Sin(rad) * newDist;
        o.z = -Mathf.Cos(rad) * newDist;

        if (o.z > -Z_EPS) o.z = -Z_EPS;          // 플립 방지
        follow.FollowOffset = o;

        // 수치 일관성 유지(경계에서 보정되는 경우를 반영)
        pitchDeg = Mathf.Clamp(Rad2Deg(CurrentPitchRad(o)), minPitch, maxPitch);
    }

    /// <summary>현재 거리 유지하면서 피치 각도만 FollowOffset에 반영.</summary>
    private void ApplyPitchToOffset()
    {
        var o = follow.FollowOffset;
        float dist = CurrentDistance(o);

        float rad = Deg2Rad(pitchDeg);
        o.y = Mathf.Sin(rad) * dist;
        o.z = -Mathf.Cos(rad) * dist;

        if (o.z > -Z_EPS) o.z = -Z_EPS;
        follow.FollowOffset = o;
    }

    /// <summary>항상 카메라가 피벗 뒤(-z)에 있도록 강제.</summary>
    private void ClampZBehindTarget()
    {
        var o = follow.FollowOffset;
        if (o.z > -Z_EPS)
        {
            o.z = -Z_EPS;
            follow.FollowOffset = o;
        }
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
