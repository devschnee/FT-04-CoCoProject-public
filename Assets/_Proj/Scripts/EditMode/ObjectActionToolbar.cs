using System;
using UnityEngine;
using UnityEngine.UI;

public class ObjectActionToolbar : MonoBehaviour
{
    #region === Types ===
    public enum AnchorMode { Transform, BoundsTop }
    #endregion

    #region === Inspector ===
    [Header("Wiring")]
    [SerializeField] Canvas canvas;          // 툴바를 올릴 캔버스
    [SerializeField] Button btnInfo;
    [SerializeField] Button btnRotate;
    [SerializeField] Button btnInventory; // 보관
    [SerializeField] Button btnOk;        // 새로 추가
    [SerializeField] Button btnCancel;    // 새로 추가

    [Header("Anchor")]
    [SerializeField] AnchorMode anchorMode = AnchorMode.BoundsTop;
    [SerializeField, Tooltip("BoundsTop 기준일 때 윗면에서 더 띄울 높이(m)")]
    float extraHeight = 0.15f;
    [SerializeField, Tooltip("Transform 기준일 때 월드 오프셋(m)")]
    Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Screen")]
    [SerializeField, Tooltip("화면에서의 추가 오프셋(px)")]
    Vector2 screenOffset = new Vector2(0f, 16f);
    [SerializeField, Tooltip("화면 밖으로 나가지 않도록 클램프")]
    bool clampToScreen = true;
    [SerializeField, Tooltip("클램프 패딩(px)")]
    Vector2 clampPadding = new Vector2(8f, 8f);

    [Header("Follow")]
    [SerializeField, Tooltip("부드럽게 따라오기(0=즉시, 10=매우부드러움)")]
    float followLerp = 0f;
    #endregion

    #region === State ===
    Transform target;          // 따라다닐 대상
    Camera cam;                // 월드 카메라
    RectTransform rect;        // 이 툴바의 RectTransform
    Vector2 currentAnchored;   // 스무딩 중간값
    #endregion

    #region === Unity Lifecycle ===
    void Awake()
    {
        rect = transform as RectTransform;
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        gameObject.SetActive(false); // 기본은 숨김
    }

    void LateUpdate()
    {
        if (!IsRenderable()) return;

        Vector2 screenPos = CalcScreenPos();
        SetAnchoredPosition(screenPos);
    }
    #endregion

    #region === Public API ===
    /// <summary>
    /// 툴바 표시 및 버튼 콜백 설정.
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
        this.target = target;
        this.cam = worldCamera ? worldCamera : Camera.main;

        Wire(btnInfo, onInfo);
        Wire(btnRotate, onRotate);
        Wire(btnInventory, onInventory);
        Wire(btnOk, onOk);         // ✅ 콜백 넘기면 자동 활성
        Wire(btnCancel, onCancel); // ✅ "

        gameObject.SetActive(true);

        Vector2 pos = CalcScreenPos();
        SetAnchoredPositionImmediate(pos);
    }

    /// <summary>
    /// 툴바 숨김 및 버튼 콜백 해제.
    /// </summary>
    public void Hide()
    {
        if (this == null) return; 

        gameObject.SetActive(false);
        target = null;

        if (btnInfo) btnInfo.onClick.RemoveAllListeners();
        if (btnRotate) btnRotate.onClick.RemoveAllListeners();
        if (btnInventory) btnInventory.onClick.RemoveAllListeners();
    }

    #endregion

    #region === Positioning ===
    /// <summary>
    /// 대상의 월드 위치를 스크린 좌표(또는 캔버스 로컬 좌표)로 변환.
    /// </summary>
    Vector2 CalcScreenPos()
    {
        Vector3 worldPos = GetAnchorWorldPosition();

        // 월드 → 스크린
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        screen += screenOffset;

        // 화면 클램프
        if (clampToScreen)
        {
            Vector2 min = clampPadding;
            Vector2 max = new Vector2(Screen.width, Screen.height) - clampPadding; // ← 명시적 Vector2

            screen.x = Mathf.Clamp(screen.x, min.x, max.x);
            screen.y = Mathf.Clamp(screen.y, min.y, max.y);
        }

        // Canvas 좌표계로 변환
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Overlay는 스크린 좌표 == 앵커드 좌표
            return screen;
        }
        else
        {
            // ScreenSpace-Camera / WorldSpace
            RectTransform canvasRect = canvas.transform as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screen, canvas.worldCamera, out Vector2 local))
            {
                return local;
            }
            // 실패 시 이전 위치 유지
            return currentAnchored;
        }
    }

    /// <summary>
    /// 앵커 모드에 따른 월드 위치 계산.
    /// </summary>
    Vector3 GetAnchorWorldPosition()
    {
        if (!target) return Vector3.zero;

        if (anchorMode == AnchorMode.BoundsTop)
        {
            if (TryGetWorldBounds(target, out Bounds b))
                return new Vector3(b.center.x, b.max.y + extraHeight, b.center.z);

            // 렌더러/콜라이더가 없으면 Transform 기준으로만 보정
            return target.position + new Vector3(0f, Mathf.Max(extraHeight, 0f), 0f);
        }
        else // Transform
        {
            return target.position + worldOffset;
        }
    }

    /// <summary>
    /// 위치 즉시 적용(스무딩 없음).
    /// </summary>
    void SetAnchoredPositionImmediate(Vector2 p)
    {
        currentAnchored = p;
        rect.anchoredPosition = p;
    }

    /// <summary>
    /// 위치 적용(옵션: 스무딩).
    /// </summary>
    void SetAnchoredPosition(Vector2 p)
    {
        if (followLerp <= 0f)
        {
            SetAnchoredPositionImmediate(p);
        }
        else
        {
            float t = 1f - Mathf.Exp(-followLerp * Time.unscaledDeltaTime); // 지수 감쇠형 보간
            currentAnchored = Vector2.Lerp(currentAnchored, p, t);
            rect.anchoredPosition = currentAnchored;
        }
    }
    #endregion

    #region === Helpers ===
    /// <summary>
    /// 버튼에 콜백 연결(Null 안전).
    /// </summary>
    void Wire(Button b, System.Action a)
    {
        if (!b) return;
        b.onClick.RemoveAllListeners();
        if (a != null) b.onClick.AddListener(() => a());

        b.gameObject.SetActive(a != null); // ✅ 핵심
        var cg = b.GetComponentInParent<CanvasGroup>();
        if (cg) cg.interactable = true;
    }

    /// <summary>
    /// 버튼 콜백 제거(Null 안전).
    /// </summary>
    static void UnwireButton(Button btn)
    {
        if (!btn) return;
        btn.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// 이 프레임에 툴바 위치 갱신이 가능한지 검사.
    /// </summary>
    bool IsRenderable()
    {
        return gameObject.activeSelf && target && cam && canvas && rect;
    }

    /// <summary>
    /// 대상 트리의 렌더러/콜라이더 바운즈를 합산.
    /// </summary>
    static bool TryGetWorldBounds(Transform t, out Bounds bounds)
    {
        bounds = default;
        bool has = false;

        // 1) Renderer 우선(시각적 기준)
        var rs = t.GetComponentsInChildren<Renderer>(includeInactive: false);
        foreach (var r in rs)
        {
            if (!r) continue;
            if (!has) { bounds = r.bounds; has = true; }
            else bounds.Encapsulate(r.bounds);
        }

        // 2) 없으면 Collider로 대체
        if (!has)
        {
            var cs = t.GetComponentsInChildren<Collider>(includeInactive: false);
            foreach (var c in cs)
            {
                if (!c) continue;
                if (!has) { bounds = c.bounds; has = true; }
                else bounds.Encapsulate(c.bounds);
            }
        }

        return has;
    }
    #endregion
}
