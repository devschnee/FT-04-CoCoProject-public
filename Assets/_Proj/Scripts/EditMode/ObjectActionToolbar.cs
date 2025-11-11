using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 선택된 오브젝트 위에 떠서
/// - 정보
/// - 회전
/// - 인벤토리로 보관
/// - OK / Cancel
/// 버튼을 보여주는 툴바
///
/// 화면 공간에 떠야 해서 다음을 한다:
/// 1) 대상 월드위치 → 스크린 좌표 변환
/// 2) 캔버스 안으로 클램프
/// 3) 카메라 뒤로 가면 숨김
/// </summary>
[DisallowMultipleComponent]
public class ObjectActionToolbar : MonoBehaviour
{
    #region === Types ===
    public enum AnchorMode { Transform, BoundsTop }
    #endregion

    #region === Inspector ===

    [Header("Wiring")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Button btnInfo;
    [SerializeField] private Button btnRotate;
    [SerializeField] private Button btnInventory;
    [SerializeField] private Button btnOk;
    [SerializeField] private Button btnCancel;

    [Header("Anchor")]
    [SerializeField] private AnchorMode anchorMode = AnchorMode.BoundsTop;
    [SerializeField, Tooltip("BoundsTop 기준일 때 윗면에서 더 띄울 높이(m)")]
    private float extraHeight = 0.15f;
    [SerializeField, Tooltip("Transform 기준일 때 월드 오프셋(m)")]
    private Vector3 worldOffset = new(0f, 1.2f, 0f);

    [Header("Screen")]
    [SerializeField, Tooltip("스크린에서의 추가 오프셋(px)")]
    private Vector2 screenOffset = new(0f, 16f);
    [SerializeField, Tooltip("화면 밖으로 나가지 않도록 할지")]
    private bool clampToScreen = true;
    [SerializeField, Tooltip("클램프 패딩(px)")]
    private Vector2 clampPadding = new(8f, 8f);
    [SerializeField, Tooltip("ScreenSpaceOverlay에서 SafeArea를 고려할지")]
    private bool respectSafeArea = true;

    [Header("Follow")]
    [SerializeField, Tooltip("부드럽게 따라오기(0=즉시, 10=매우부드러움)")]
    private float followLerp = 0f;

    [Header("Visibility")]
    [SerializeField, Tooltip("카메라 뒤로 가면 자동으로 숨김")]
    private bool autoHideWhenBehindCamera = true;

    #endregion


    #region === State ===

    private Transform target;        // 따라다닐 대상
    private Camera cam;              // 월드 카메라
    private RectTransform rect;      // 이 툴바의 RectTransform
    private Vector2 currentAnchored; // 현재 화면 위치(스무딩용)
    private bool visibleByCamera = true;
    private RectTransform canvasRect;   // 캔버스 RectTransform 캐시
    private Camera lastWorldCam;        // 비-Overlay에서 변환에 쓸 카메라 캐시
    #endregion


    #region === Unity ===

    private void Awake()
    {
        EnsureRefs();                      // ★ 추가
        rect = transform as RectTransform;
        if (!canvas)
            canvas = GetComponentInParent<Canvas>();

        // 기본은 숨김
        gameObject.SetActive(false);
    }

    private void EnsureRefs()
    {
        if (!rect) rect = transform as RectTransform;
        if (!canvas) canvas = GetComponentInParent<Canvas>(includeInactive: true);
        if (!canvasRect && canvas) canvasRect = canvas.transform as RectTransform;
        if (!cam) cam = Camera.main;
        if (!lastWorldCam) lastWorldCam = cam;
    }

    private void LateUpdate()
    {
        EnsureRefs();
        if (!CanUpdatePosition()) return;

        // 카메라 뒤면 숨김
        visibleByCamera = IsTargetInFrontOfCamera();
        if (autoHideWhenBehindCamera && !visibleByCamera)
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
            return;
        }
        else if (!gameObject.activeSelf)
        {
            // 다시 보이도록
            gameObject.SetActive(true);
        }

        // 위치 갱신
        Vector2 screenPos = CalcScreenPos();
        SetAnchoredPosition(screenPos);
    }

    #endregion


    #region === Public API ===

    /// <summary>
    /// 툴바를 보이게 하고 각 버튼 콜백을 설정.
    /// null 로 주면 그 버튼은 숨겨진다.
    /// </summary>
    public void Show(
    Transform target,
    Camera worldCamera,
    Action onInfo = null,
    Action onRotate = null,
    Action onInventory = null,
    Action onOk = null,
    Action onCancel = null)
    {
        EnsureRefs();                                 // ★ 추가

        this.target = target;
        cam = worldCamera ? worldCamera : Camera.main;
        if (!cam) cam = Camera.main;                  // 폴백
        lastWorldCam = cam ? cam : lastWorldCam;      // ★ 캐시

        Wire(btnInfo, onInfo);
        Wire(btnRotate, onRotate);
        Wire(btnInventory, onInventory);
        Wire(btnOk, onOk);
        Wire(btnCancel, onCancel);

        // 비-Overlay면 변환에 쓸 카메라가 꼭 있어야 함
        if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay && !cam)
        {
            Debug.LogWarning("[ObjectActionToolbar] Non-Overlay canvas requires worldCamera.");
            gameObject.SetActive(false);
            return;
        }

        // 카메라 뒤인지 먼저 판단
        visibleByCamera = IsTargetInFrontOfCamera();
        gameObject.SetActive(visibleByCamera);

        // 초기 위치는 즉시
        Vector2 pos = CalcScreenPos();
        SetAnchoredPositionImmediate(pos);
    }


    /// <summary>툴바 숨기기 (버튼 콜백도 제거)</summary>
    public void Hide()
    {
        if (!this) return;

        gameObject.SetActive(false);
        target = null;
        cam = null;

        Unwire(btnInfo);
        Unwire(btnRotate);
        Unwire(btnInventory);
        Unwire(btnOk);
        Unwire(btnCancel);
    }

    #endregion


    #region === Positioning ===

    /// <summary>대상 월드 포지션을 기준으로 스크린pos → 캔버스pos 구한다.</summary>
    private Vector2 CalcScreenPos()
    {
        Vector3 worldPos = GetAnchorWorldPosition();
        if (!cam) cam = Camera.main;

        // 월드 → 스크린
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        screen += screenOffset;

        // 화면 밖 클램프
        if (clampToScreen)
        {
            Vector2 min = clampPadding;
            Vector2 max = new Vector2(Screen.width, Screen.height) - clampPadding;

            // SafeArea 고려 (Overlay에서만)
            if (respectSafeArea && canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Rect sa = Screen.safeArea;
                min = new Vector2(Mathf.Max(min.x, sa.xMin), Mathf.Max(min.y, sa.yMin));
                max = new Vector2(Mathf.Min(max.x, sa.xMax), Mathf.Min(max.y, sa.yMax));
            }

            screen.x = Mathf.Clamp(screen.x, min.x, max.x);
            screen.y = Mathf.Clamp(screen.y, min.y, max.y);
        }

        // Canvas 좌표계로 변환
        if (canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Overlay 는 스크린 = anchored
            return screen;
        }
        else
        {
            // ★ 카메라 & 캔버스 Rect 보장
            var cr = canvasRect ? canvasRect : (canvas ? canvas.transform as RectTransform : null);
            var camForCanvas = (canvas && canvas.worldCamera) ? canvas.worldCamera : (cam ? cam : lastWorldCam);

            if (cr && RectTransformUtility.ScreenPointToLocalPointInRectangle(cr, screen, camForCanvas, out var local))
                return local;

            // 실패 시 이전 값 유지
            return currentAnchored;
        }
    }

    /// <summary>AnchorMode 에 따라 기준 월드 위치를 구한다.</summary>
    private Vector3 GetAnchorWorldPosition()
    {
        if (!target) return Vector3.zero;

        if (anchorMode == AnchorMode.BoundsTop)
        {
            if (TryGetWorldBounds(target, out Bounds b))
                return new Vector3(b.center.x, b.max.y + extraHeight, b.center.z);

            // 바운즈 없으면 Transform 기준
            return target.position + Vector3.up * Mathf.Max(extraHeight, 0f);
        }

        // Transform 기준
        return target.position + worldOffset;
    }

    private void SetAnchoredPositionImmediate(Vector2 p)
    {
        EnsureRefs();                         // ★ 추가
        if (!rect) return;                    // ★ 가드

        currentAnchored = p;
        rect.anchoredPosition = p;
    }

    private void SetAnchoredPosition(Vector2 p)
    {
        EnsureRefs();                         // ★ 추가
        if (!rect) return;                    // ★ 가드

        if (followLerp <= 0f)
        {
            SetAnchoredPositionImmediate(p);
            return;
        }

        float t = 1f - Mathf.Exp(-followLerp * Time.unscaledDeltaTime);
        currentAnchored = Vector2.Lerp(currentAnchored, p, t);
        rect.anchoredPosition = currentAnchored;
    }

    #endregion


    #region === Helpers ===

    private void Wire(Button b, Action cb)
    {
        if (!b) return;

        b.onClick.RemoveAllListeners();

        if (cb != null)
        {
            b.onClick.AddListener(() => cb());
            b.gameObject.SetActive(true);
        }
        else
        {
            b.gameObject.SetActive(false);
        }

        // 혹시 부모 CanvasGroup 이 비활성화 되어있을 수도 있으니 보장
        var cg = b.GetComponentInParent<CanvasGroup>();
        if (cg) cg.interactable = true;
    }

    private static void Unwire(Button b)
    {
        if (!b) return;
        b.onClick.RemoveAllListeners();
    }

    private bool CanUpdatePosition()
    {
        EnsureRefs(); // ★ 추가

        if (!rect || !canvas || !target) return false;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return true;
        return cam != null; // Camera/WorldSpace는 cam 필수
    }

    private bool IsTargetInFrontOfCamera()
    {
        if (!target) return true;
        if (!cam) cam = Camera.main;        // ★ 폴백 시도
        if (!cam) return true;              // 카메라를 아직 못 잡았으면 가리진 않은 걸로 간주

        var vp = cam.WorldToViewportPoint(target.position);
        return vp.z > 0f;
    }


    /// <summary>트리 전체의 Renderer/Collider bounds 를 합쳐서 반환.</summary>
    private static bool TryGetWorldBounds(Transform t, out Bounds bounds)
    {
        bounds = default;
        bool has = false;

        // 1) Renderer 기준
        var rs = t.GetComponentsInChildren<Renderer>(includeInactive: false);
        for (int i = 0; i < rs.Length; i++)
        {
            var r = rs[i];
            if (!r) continue;

            if (!has) { bounds = r.bounds; has = true; }
            else bounds.Encapsulate(r.bounds);
        }

        // 2) 없으면 Collider 기준
        if (!has)
        {
            var cs = t.GetComponentsInChildren<Collider>(includeInactive: false);
            for (int i = 0; i < cs.Length; i++)
            {
                var c = cs[i];
                if (!c) continue;

                if (!has) { bounds = c.bounds; has = true; }
                else bounds.Encapsulate(c.bounds);
            }
        }

        return has;
    }

    #endregion
}
