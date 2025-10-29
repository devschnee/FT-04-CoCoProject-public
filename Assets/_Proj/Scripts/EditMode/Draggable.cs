using UnityEngine;

[DisallowMultipleComponent]
public class Draggable : MonoBehaviour
{
    #region === Inspector ===
    [Header("Highlight (optional)")]
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Color highlightColor = new(1f, 0.9f, 0.3f, 1f);
    [SerializeField] private Color invalidColor = new(1f, 0.2f, 0.2f, 1f);

    [Header("Transform Persistence")]
    [SerializeField, Tooltip("시작 시 PlayerPrefs에 저장된 위치/회전을 자동 로드")]
    private bool loadSavedTransformOnStart = true;

    [SerializeField, Tooltip("저장 키 직접 지정(비우면 'SceneName/ObjectName' 사용)")]
    private string customSaveKey = "";

    [Header("Advanced")]
    [SerializeField, Tooltip("머티리얼 인스턴스 생성 없이 색을 바꾸기 위해 PropertyBlock 사용 권장")]
    private bool useMaterialPropertyBlock = true;

    [SerializeField, Tooltip("색상 속성명(대부분 _Color). 커스텀 셰이더면 여기에 맞춰 변경")]
    private string colorPropName = "_Color";

    [SerializeField, Tooltip("키 충돌을 줄이기 위해 ObjectName 대신 전체 트랜스폼 경로를 포함")]
    private bool useFullTransformPathForKey = false;
    #endregion

    #region === State ===
    private Color[] originalColors;       // 렌더러별 원본 색상(간단화: 첫 머티리얼 기준)
    private bool highlighted;
    private bool invalid;

    private MaterialPropertyBlock mpb;
    private int colorPropId = -1;
    private static readonly string[] kColorPropCandidates = { "_BaseColor", "_Color", "_TintColor" };
    #endregion

    #region === Unity ===
    private void Awake()
    {
        EnsureRenderers();
        CacheOriginalColors();
        InitPropertyBlock();

        if (loadSavedTransformOnStart)
            LoadTransformIfAny();

        ApplyVisual();
    }

    private void OnValidate()
    {
        EnsureRenderers();
        // 에디터에서도 즉시 반영
        if (colorPropId < 0) colorPropId = Shader.PropertyToID(colorPropName);
        ApplyVisual();
    }

    private void OnDestroy()
    {
        // PropertyBlock으로 칠했을 때, 파괴 시 원복(안전)
        if (useMaterialPropertyBlock && renderers != null)
        {
            foreach (var r in renderers)
            {
                if (!r) continue;
                r.SetPropertyBlock(null);
            }
        }
    }
    #endregion

    #region === Public API (Highlight & Validity) ===
    public void SetHighlighted(bool on)
    {
        highlighted = on;
        ApplyVisual();
    }

    public void SetInvalid(bool on)
    {
        invalid = on;
        ApplyVisual();
    }
    #endregion

    #region === Public API (Persistence) ===
    /// <summary>현재 위치/회전을 PlayerPrefs에 저장합니다.</summary>
    public void SavePosition() => SaveTransform(); // 호환 유지

    public void SaveTransform()
    {
        Vector3 p = transform.position;
        Quaternion q = transform.rotation;

        PlayerPrefs.SetFloat(BuildKey("x"), p.x);
        PlayerPrefs.SetFloat(BuildKey("y"), p.y);
        PlayerPrefs.SetFloat(BuildKey("z"), p.z);

        PlayerPrefs.SetFloat(BuildKey("qx"), q.x);
        PlayerPrefs.SetFloat(BuildKey("qy"), q.y);
        PlayerPrefs.SetFloat(BuildKey("qz"), q.z);
        PlayerPrefs.SetFloat(BuildKey("qw"), q.w);

        PlayerPrefs.Save();
    }

    /// <summary>저장된 위치/회전이 있으면 불러옵니다.</summary>
    public void LoadPositionIfAny() => LoadTransformIfAny(); // 호환 유지

    public void LoadTransformIfAny()
    {
        string kx = BuildKey("x");
        string ky = BuildKey("y");
        string kz = BuildKey("z");

        // 위치
        if (PlayerPrefs.HasKey(kx))
        {
            float x = PlayerPrefs.GetFloat(kx, transform.position.x);
            float y = PlayerPrefs.GetFloat(ky, transform.position.y);
            float z = PlayerPrefs.GetFloat(kz, transform.position.z);
            transform.position = new Vector3(x, y, z);
        }

        // 회전(쿼터니언 우선)
        string kQx = BuildKey("qx");
        string kQy = BuildKey("qy");
        string kQz = BuildKey("qz");
        string kQw = BuildKey("qw");

        if (PlayerPrefs.HasKey(kQx) && PlayerPrefs.HasKey(kQy) &&
            PlayerPrefs.HasKey(kQz) && PlayerPrefs.HasKey(kQw))
        {
            var q = new Quaternion(
                PlayerPrefs.GetFloat(kQx, transform.rotation.x),
                PlayerPrefs.GetFloat(kQy, transform.rotation.y),
                PlayerPrefs.GetFloat(kQz, transform.rotation.z),
                PlayerPrefs.GetFloat(kQw, transform.rotation.w)
            );
            transform.rotation = q;
        }
        else
        {
            // 과거 Euler 호환(rx,ry,rz)
            string kRx = BuildKey("rx");
            string kRy = BuildKey("ry");
            string kRz = BuildKey("rz");
            if (PlayerPrefs.HasKey(kRy))
            {
                var euler = new Vector3(
                    PlayerPrefs.GetFloat(kRx, transform.eulerAngles.x),
                    PlayerPrefs.GetFloat(kRy, transform.eulerAngles.y),
                    PlayerPrefs.GetFloat(kRz, transform.eulerAngles.z)
                );
                transform.eulerAngles = euler;
            }
        }

        Physics.SyncTransforms();
        //LSH
        InLobbyManager.Instance.NewMap();
    }

    /// <summary>이 오브젝트의 저장된 위치/회전 키를 삭제합니다.</summary>
    public void DeleteSavedPosition()
    {
        PlayerPrefs.DeleteKey(BuildKey("x"));
        PlayerPrefs.DeleteKey(BuildKey("y"));
        PlayerPrefs.DeleteKey(BuildKey("z"));

        PlayerPrefs.DeleteKey(BuildKey("qx"));
        PlayerPrefs.DeleteKey(BuildKey("qy"));
        PlayerPrefs.DeleteKey(BuildKey("qz"));
        PlayerPrefs.DeleteKey(BuildKey("qw"));

        PlayerPrefs.DeleteKey(BuildKey("rx"));
        PlayerPrefs.DeleteKey(BuildKey("ry"));
        PlayerPrefs.DeleteKey(BuildKey("rz"));
        PlayerPrefs.Save();
    }
    #endregion

    #region === Visual ===
    private void ApplyVisual()
    {
        if (renderers == null || renderers.Length == 0) return;

        var useOriginal = !invalid && !highlighted;
        var color = useOriginal ? Color.white : (invalid ? invalidColor : highlightColor);

        // 각 렌더러마다 프로퍼티 탐지
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r) continue;

            // 1) 우선순위: 명시된 colorPropName가 유효하면 그걸 사용
            string propToUse = null;
            var matForCheck = r.sharedMaterial ? r.sharedMaterial : (Application.isPlaying ? r.material : null);
            if (matForCheck && matForCheck.HasProperty(colorPropName))
                propToUse = colorPropName;
            else
                propToUse = DetectColorProp(r); // 2) 자동 탐지

            if (useMaterialPropertyBlock && propToUse != null)
            {
                // PropertyBlock 경로
                EnsureMPB();
                int pid = Shader.PropertyToID(propToUse);
                if (useOriginal && originalColors != null && i < originalColors.Length)
                    mpb.SetColor(pid, originalColors[i]);
                else
                    mpb.SetColor(pid, color);
                r.SetPropertyBlock(mpb);
            }
            else
            {
                // Fallback: material.color (메모리 증가 가능)
                bool isPlaying = Application.isPlaying;
                var mat = isPlaying ? r.material : r.sharedMaterial;
                if (!mat) continue;

                if (useOriginal && originalColors != null && i < originalColors.Length)
                    mat.color = originalColors[i];
                else
                    mat.color = color;
            }
        }
    }
    private string DetectColorProp(Renderer r)
    {
        var mat = r ? (r.sharedMaterial ? r.sharedMaterial : (Application.isPlaying ? r.material : null)) : null;
        if (!mat) return null;
        foreach (var p in kColorPropCandidates)
            if (mat.HasProperty(p)) return p;
        return null;
    }
    private void EnsureRenderers()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
    }

    private void CacheOriginalColors()
    {
        if (renderers == null || renderers.Length == 0)
        {
            originalColors = null;
            return;
        }

        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r)
            {
                originalColors[i] = Color.white;
                continue;
            }

            // sharedMaterial 우선: 에디터에서도 안전
            var mat = r.sharedMaterial ? r.sharedMaterial : (Application.isPlaying ? r.material : null);
            originalColors[i] = mat && mat.HasProperty(colorPropName) ? mat.GetColor(colorPropName) : Color.white;
        }
    }

    private void InitPropertyBlock()
    {
        colorPropId = Shader.PropertyToID(colorPropName);
        if (useMaterialPropertyBlock && mpb == null)
            mpb = new MaterialPropertyBlock();
    }

    private void EnsureMPB()
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        if (colorPropId < 0) colorPropId = Shader.PropertyToID(colorPropName);
    }
    #endregion

    #region === Key Helpers ===
    private string BuildKey(string suffix)
    {
        string baseKey;
        if (!string.IsNullOrEmpty(customSaveKey))
        {
            baseKey = customSaveKey;
        }
        else
        {
            var sceneName = gameObject.scene.name;
            var objPath = useFullTransformPathForKey ? GetFullPath(transform) : gameObject.name;
            baseKey = $"{sceneName}/{objPath}";
        }
        return $"{baseKey}:{suffix}";
    }

    private static string GetFullPath(Transform t)
    {
        // Root/Parent/Child 형식 경로(동일 이름 오브젝트 충돌↓)
        var path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = $"{t.name}/{path}";
        }
        return path;
    }
    #endregion
}
