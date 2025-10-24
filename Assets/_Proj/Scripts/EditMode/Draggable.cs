using UnityEngine;

[DisallowMultipleComponent]
public class Draggable : MonoBehaviour
{
    #region === Inspector ===
    [Header("Highlight (optional)")]
    [SerializeField] Renderer[] renderers;                              // 하이라이트를 적용할 렌더러들(비우면 자동 수집)
    [SerializeField] Color highlightColor = new(1f, 0.9f, 0.3f, 1f);    // 선택(노란색)
    [SerializeField] Color invalidColor = new(1f, 0.2f, 0.2f, 1f);    // 배치 불가(빨간색)

    [Header("Position Persistence")]
    [SerializeField, Tooltip("시작 시 PlayerPrefs에 저장된 위치를 자동 로드")]
    bool loadSavedPositionOnStart = true;

    [SerializeField, Tooltip("저장 키 직접 지정(비우면 'SceneName/ObjectName' 사용)")]
    string customSaveKey = "";
    #endregion

    #region === State ===
    Color[] _originalColors;           // 렌더러별 원래 색상
    bool _highlighted = false;         // 선택 상태
    bool _invalid = false;             // 배치 불가 상태(겹침 등)
    #endregion

    #region === Unity Lifecycle ===
    void Awake()
    {
        EnsureRenderers();
        CacheOriginalColors();

        if (loadSavedPositionOnStart)
            LoadPositionIfAny();

        ApplyVisual(); // 초기 시각 상태 반영
    }

    void OnValidate()
    {
        // 에디터에서 값 변경 시 자동 보정
        EnsureRenderers();
        // _originalColors는 런타임 때 채워지므로, 에디터 갱신은 현재 머티리얼 상태 기준으로만 색상 적용
        ApplyVisual();
    }
    #endregion

    #region === Public API (Highlight & Validity) ===
    /// <summary>선택(하이라이트) 상태 토글</summary>
    public void SetHighlighted(bool on)
    {
        _highlighted = on;
        ApplyVisual();
    }

    /// <summary>배치 불가(Invalid) 상태 토글</summary>
    public void SetInvalid(bool on)
    {
        _invalid = on;
        ApplyVisual();
    }
    #endregion

    #region === Public API (Persistence) ===
    /// <summary>현재 위치를 PlayerPrefs에 저장</summary>
    public void SavePosition()
    {
        Vector3 p = transform.position;
        PlayerPrefs.SetFloat(BuildKey("x"), p.x);
        PlayerPrefs.SetFloat(BuildKey("y"), p.y);
        PlayerPrefs.SetFloat(BuildKey("z"), p.z);
        PlayerPrefs.Save();
    }

    /// <summary>PlayerPrefs에 위치가 있으면 불러옴</summary>
    public void LoadPositionIfAny()
    {
        string kx = BuildKey("x");
        if (!PlayerPrefs.HasKey(kx)) return;

        float x = PlayerPrefs.GetFloat(kx, transform.position.x);
        float y = PlayerPrefs.GetFloat(BuildKey("y"), transform.position.y);
        float z = PlayerPrefs.GetFloat(BuildKey("z"), transform.position.z);
        transform.position = new Vector3(x, y, z);
    }
    #endregion

    #region === Visual Helpers ===
    /// <summary>렌더러 배열 보장(비어 있으면 자식에서 자동 수집)</summary>
    void EnsureRenderers()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();
    }

    /// <summary>원래 색상 캐시</summary>
    void CacheOriginalColors()
    {
        if (renderers == null || renderers.Length == 0) return;

        _originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r) continue;

            // 런타임에 material 접근 시 머티리얼 인스턴스가 생성됨. 최초 캐시에만 접근.
            _originalColors[i] = r.material.color;
        }
    }

    /// <summary>현재 상태에 따른 색상을 렌더러에 적용</summary>
    void ApplyVisual()
    {
        if (renderers == null || renderers.Length == 0) return;

        // 적용할 목표 색상 결정
        Color target;
        if (_invalid) target = invalidColor;
        else if (_highlighted) target = highlightColor;
        else
        {
            // 원래 색상 복원 (캐시가 유효할 때만)
            target = Color.white;
        }

        bool isPlaying = Application.isPlaying;
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r) continue;

            // 에디터에서는 sharedMaterial을 써서 머티리얼 인스턴스 남발 방지
            var mat = isPlaying ? r.material : r.sharedMaterial;

            if (!_invalid && !_highlighted && _originalColors != null && _originalColors.Length == renderers.Length)
            {
                // 원래 색상으로 복원
                mat.color = _originalColors[i];
            }
            else
            {
                // 상태 색상 적용
                mat.color = target;
            }
        }
    }
    #endregion

    #region === Key Helpers ===
    /// <summary>저장 키 생성: (customSaveKey or SceneName/ObjectName):suffix</summary>
    string BuildKey(string suffix)
    {
        string baseKey = string.IsNullOrEmpty(customSaveKey)
            ? $"{gameObject.scene.name}/{gameObject.name}"
            : customSaveKey;
        return $"{baseKey}:{suffix}";
    }
    #endregion
}
