using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// - 드래그 가능한 오브젝트에 붙는 공통 컴포넌트
/// - 하이라이트 / 무효 표시 색상
/// - 위치/회전 PlayerPrefs 저장/로드
/// - 렌더러 자동 수집 + PropertyBlock 기본 사용(드로우콜/GC 안전)
/// </summary>
[DisallowMultipleComponent]
public class Draggable : MonoBehaviour
{
    #region === Inspector ===

    [Header("Highlight (선택/보관 상태 표현)")]
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Color highlightColor = new(1f, 0.9f, 0.3f, 1f);
    [SerializeField] private Color invalidColor = new(1f, 0.2f, 0.2f, 1f);

    [Header("Transform 저장/로드")]
    [SerializeField, Tooltip("Start 시 PlayerPrefs에서 자동 로드할지 여부")]
    private bool loadSavedTransformOnStart = true;

    [SerializeField, Tooltip("수동으로 저장 키를 지정하고 싶을 때")]
    private string customSaveKey = "";

    [Header("머티리얼 처리")]
    [SerializeField, Tooltip("MaterialPropertyBlock 사용 권장(머티리얼 인스턴스 생성 방지)")]
    private bool useMaterialPropertyBlock = true;

    [SerializeField, Tooltip("셰이더 색상 프로퍼티명(비워두면 자동 탐색)")]
    private string colorPropName = "";

    [Header("키 전략")]
    [SerializeField, Tooltip("저장 키에 전체 트랜스폼 경로를 포함할지 여부")]
    private bool useFullTransformPathForKey = false;

    #endregion

    #region === State ===

    private bool highlighted;
    private bool invalid;

    private MaterialPropertyBlock mpb;

    // 렌더러별 원본색 / 컬러 프로퍼티 캐시
    private struct RCache { public int colorId; public Color original; }
    private readonly List<RCache> _rCache = new();

    private static readonly string[] kColorProps = { "_BaseColor", "_Color", "_TintColor" };

    #endregion

    #region === Unity ===

    private void Awake()
    {
        EnsureRenderers();
        BuildRendererCache();

        if (loadSavedTransformOnStart)
            LoadTransformIfAny();

        ApplyVisual();
    }

    private void OnValidate()
    {
        EnsureRenderers();
        BuildRendererCache();
        ApplyVisual();
    }

    private void OnDestroy()
    {
        // PropertyBlock 덮은 것 제거(선택)
        if (useMaterialPropertyBlock && renderers != null)
        {
            foreach (var r in renderers)
                if (r) r.SetPropertyBlock(null);
        }
    }

    #endregion

    #region === Public API (Highlight & Validity) ===

    public void SetHighlighted(bool on)
    {
        if (highlighted == on) return;
        highlighted = on;
        ApplyVisual();
    }

    public void SetInvalid(bool on)
    {
        if (invalid == on) return;
        invalid = on;
        ApplyVisual();
    }

    #endregion

    #region === Public API (Persistence) ===

    public void SavePosition() => SaveTransform();

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

    public void LoadPositionIfAny() => LoadTransformIfAny();

    public void LoadTransformIfAny()
    {
        if (PlayerPrefs.HasKey(BuildKey("x")))
        {
            float x = PlayerPrefs.GetFloat(BuildKey("x"), transform.position.x);
            float y = PlayerPrefs.GetFloat(BuildKey("y"), transform.position.y);
            float z = PlayerPrefs.GetFloat(BuildKey("z"), transform.position.z);
            transform.position = new Vector3(x, y, z);
        }

        if (PlayerPrefs.HasKey(BuildKey("qx")) &&
            PlayerPrefs.HasKey(BuildKey("qy")) &&
            PlayerPrefs.HasKey(BuildKey("qz")) &&
            PlayerPrefs.HasKey(BuildKey("qw")))
        {
            var q = new Quaternion(
                PlayerPrefs.GetFloat(BuildKey("qx"), transform.rotation.x),
                PlayerPrefs.GetFloat(BuildKey("qy"), transform.rotation.y),
                PlayerPrefs.GetFloat(BuildKey("qz"), transform.rotation.z),
                PlayerPrefs.GetFloat(BuildKey("qw"), transform.rotation.w)
            );
            transform.rotation = q;
        }
        else if (PlayerPrefs.HasKey(BuildKey("ry"))) // 구버전 호환
        {
            var euler = new Vector3(
                PlayerPrefs.GetFloat(BuildKey("rx"), transform.eulerAngles.x),
                PlayerPrefs.GetFloat(BuildKey("ry"), transform.eulerAngles.y),
                PlayerPrefs.GetFloat(BuildKey("rz"), transform.eulerAngles.z)
            );
            transform.eulerAngles = euler;
        }

        Physics.SyncTransforms();
    }

    public void DeleteSavedPosition()
    {
        string[] keys = { "x", "y", "z", "qx", "qy", "qz", "qw", "rx", "ry", "rz" };
        foreach (var k in keys) PlayerPrefs.DeleteKey(BuildKey(k));
        PlayerPrefs.Save();
    }

    #endregion

    #region === Visual ===

    private void ApplyVisual()
    {
        if (renderers == null || renderers.Length == 0) return;

        bool useOriginal = !invalid && !highlighted;
        Color tint = useOriginal ? Color.white : (invalid ? invalidColor : highlightColor);

        if (useMaterialPropertyBlock)
        {
            EnsureMPB();
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (!r) continue;

                var cache = _rCache.Count > i ? _rCache[i] : default;
                if (cache.colorId == 0) continue; // 색상 속성 없음

                mpb.Clear();
                mpb.SetColor(cache.colorId, useOriginal ? cache.original : tint);
                r.SetPropertyBlock(mpb);
            }
        }
        else
        {
            // fallback: 머티리얼 인스턴싱 발생 가능
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (!r) continue;

                var cache = _rCache.Count > i ? _rCache[i] : default;
                if (cache.colorId == 0) continue;

                var mat = Application.isPlaying ? r.material : r.sharedMaterial;
                if (!mat) continue;

                mat.SetColor(cache.colorId, useOriginal ? cache.original : tint);
            }
        }
    }

    private void BuildRendererCache()
    {
        if (renderers == null || renderers.Length == 0)
        {
            _rCache.Clear();
            return;
        }

        _rCache.Clear();
        _rCache.Capacity = renderers.Length;

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r) { _rCache.Add(default); continue; }

            // 1) 사용할 재질 참조(원본 색 뽑기용): sharedMaterial 우선
            var matForRead = r.sharedMaterial ? r.sharedMaterial : (Application.isPlaying ? r.material : null);

            // 2) 컬러 프로퍼티 id 결정(명시 → 후보 자동 탐색)
            int pid = 0;
            if (!string.IsNullOrEmpty(colorPropName) && matForRead && matForRead.HasProperty(colorPropName))
            {
                pid = Shader.PropertyToID(colorPropName);
            }
            else if (matForRead)
            {
                foreach (var p in kColorProps)
                {
                    if (matForRead.HasProperty(p)) { pid = Shader.PropertyToID(p); break; }
                }
            }

            // 3) 원래 색
            Color original = Color.white;
            if (pid != 0 && matForRead) original = matForRead.GetColor(pid);

            _rCache.Add(new RCache { colorId = pid, original = original });
        }
    }

    private void EnsureRenderers()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
    }

    private void EnsureMPB()
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
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
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = $"{t.name}/{path}";
        }
        return path;
    }

    #endregion
}
