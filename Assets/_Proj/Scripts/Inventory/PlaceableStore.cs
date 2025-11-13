using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// 배치물 Placeable(집/동물/조경) 저장·복원 매니저
/// - PlayerPrefs에 JSON으로 저장/로드
/// - 세이브 데이터가 없으면 기본 집(defaultHomeId) 자동 생성
/// - 집이 여러 채면 1채만 남기고 정리
/// </summary>
public class PlaceableStore : MonoBehaviour
{
    public static PlaceableStore I { get; private set; }

    #region === Inspector ===

    [Header("Databases")]
    [SerializeField] private HomeDatabase homeDB;
    [SerializeField] private AnimalDatabase animalDB;
    [SerializeField] private DecoDatabase decoDB;

    [Header("Defaults")]
    [Tooltip("세이브 데이터가 없을 때 자동으로 생성할 기본 집 ID")]
    [SerializeField] private int defaultHomeId = 1;

    #endregion

    #region === Save Keys & Types ===

    private const string PREF_KEY_PREFIX = "PlaceableStore_v2::";

    [Serializable]
    public struct Placed
    {
        public PlaceableCategory cat;
        public int id;
        public Vector3 pos;
        public Quaternion rot;
    }

    [Serializable]
    private class Wrapper
    {
        public List<Placed> items;
    }

    #endregion

    #region === Caches ===

    private Dictionary<int, HomeData> _homeById;
    private Dictionary<int, AnimalData> _animalById;
    private Dictionary<int, DecoData> _decoById;

    private ResourcesLoader _loader;

    private string PrefKeyForCurrentScene =>
        PREF_KEY_PREFIX + SceneManager.GetActiveScene().name;

    #endregion

    #region === Unity Lifecycle ===

    private void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;

        _loader = new ResourcesLoader();
        BuildDbCaches();
        LoadAndSpawnAll(); // 저장 복원 + 기본 집 보장
    }

    #endregion

    #region === Public API ===

    /// <summary>현재 씬의 PlaceableTag를 모두 스캔해 위치/회전을 저장</summary>
    public void SaveAllFromScene()
    {
#if UNITY_2022_2_OR_NEWER
        var tags = FindObjectsByType<PlaceableTag>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var tags = FindObjectsOfType<PlaceableTag>();
#endif
        var list = new List<Placed>(capacity: tags.Length);

        for (int i = 0; i < tags.Length; i++)
        {
            var tag = tags[i];
            if (!tag || !tag.gameObject.activeInHierarchy) continue;

            // 임시 배치는 저장 제외 (InventoryTempMarker가 붙어 있으면 스킵)
            if (tag.GetComponent<InventoryTempMarker>()) continue;

            list.Add(new Placed
            {
                cat = tag.category,
                id = tag.id,
                pos = tag.transform.position,
                rot = tag.transform.rotation
            });
        }

        string json = JsonUtility.ToJson(new Wrapper { items = list }, false);
        PlayerPrefs.SetString(PrefKeyForCurrentScene, json);
        PlayerPrefs.Save();

        Debug.Log($"[PlaceableStore] Saved {list.Count} placed objects. (sceneKey={PrefKeyForCurrentScene})");
    }

    /// <summary>저장 데이터를 읽어 모두 스폰 + 기본 집 보장 + 중복 집 정리</summary>
    public void LoadAndSpawnAll()
    {
        string key = PrefKeyForCurrentScene;

        if (!PlayerPrefs.HasKey(key))
        {
            // 저장이 없으면 기본 집을 바로 생성
            EnsureSingleHomeExists();
            return;
        }

        // LSH 추가: NavMesh 재빌드
        GameObject gObj = GameObject.Find("IsLandPlane");
        NavMeshSurface nMS = gObj.GetComponent<NavMeshSurface>();
        nMS.BuildNavMesh();

        string json = PlayerPrefs.GetString(key, "");
        if (!string.IsNullOrEmpty(json))
        {
            var w = JsonUtility.FromJson<Wrapper>(json);
            if (w?.items != null && w.items.Count > 0)
            {
                int ok = 0, fail = 0;

                foreach (var p in w.items)
                {
                    if (TryGetPrefabAndName(p.cat, p.id, out var prefab, out var name))
                    {
                        var go = Instantiate(prefab, p.pos, p.rot);
                        go.name = name;

                        var tag = go.GetComponent<PlaceableTag>() ?? go.AddComponent<PlaceableTag>();
                        tag.category = p.cat;
                        tag.id = p.id;

                        if (!go.GetComponent<Draggable>()) go.AddComponent<Draggable>();

                        // LSH 추가: 카테고리별 컴포넌트 세팅
                        switch (p.cat)
                        {
                            case PlaceableCategory.Home:
                                if (go.GetComponent<NavMeshModifier>() == null) go.AddComponent<NavMeshModifier>();
                                var nMM = go.GetComponent<NavMeshModifier>();
                                nMM.overrideArea = true;
                                nMM.area = 1; 
                                break;
                            case PlaceableCategory.Animal:
                                if (go.GetComponent<AnimalBehaviour>() == null) go.AddComponent<AnimalBehaviour>();
                                go.tag = "Animal";
                                break;
                            case PlaceableCategory.Deco:
                                if (go.GetComponent<NavMeshModifier>() == null) go.AddComponent<NavMeshModifier>();
                                var nMM1 = go.GetComponent<NavMeshModifier>();
                                nMM1.overrideArea = true;
                                nMM1.area = 1; 
                                go.tag = "Decoration";
                                break;
                            default:
                                break;
                        }

                        ok++;
                    }
                    else
                    {
                        fail++;
                    }
                }

                Debug.Log($"[PlaceableStore] Restored {ok} objects (failed {fail}). (sceneKey={key})");
            }
        }

        // 복원 이후에도 최소 1채의 집을 보장
        EnsureSingleHomeExists();

        // 혹시 중복된 집이 있으면 1채만 남김
        CullExtraHomes();
    }

    /// <summary>현재 씬 키의 저장 데이터를 삭제</summary>
    public void ClearSaved()
    {
        string key = PrefKeyForCurrentScene;
        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Debug.Log($"[PlaceableStore] Cleared saved data. (sceneKey={key})");
        }
    }

    #endregion

    #region === Internals: Default Home & Dedup ===

    /// <summary>집이 하나도 없으면 defaultHomeId를 이용해 기본 집을 1채 생성</summary>
    private void EnsureSingleHomeExists()
    {
#if UNITY_2022_2_OR_NEWER
        var tags = FindObjectsByType<PlaceableTag>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var tags = FindObjectsOfType<PlaceableTag>();
#endif
        for (int i = 0; i < tags.Length; i++)
        {
            var t = tags[i];
            if (t && t.category == PlaceableCategory.Home)
                return; // 이미 집이 있음
        }

        // 집이 하나도 없으면 기본 집 생성
        if (_homeById != null && _homeById.TryGetValue(defaultHomeId, out var hd) && hd != null)
        {
            var prefab = hd.GetPrefab(_loader);
            if (prefab)
            {
                var go = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                go.name = string.IsNullOrEmpty(hd.home_name) ? $"Home {hd.home_id}" : hd.home_name;

                var tag = go.GetComponent<PlaceableTag>() ?? go.AddComponent<PlaceableTag>();
                tag.category = PlaceableCategory.Home;
                tag.id = hd.home_id;

                if (!go.GetComponent<Draggable>()) go.AddComponent<Draggable>();

                Debug.Log($"[PlaceableStore] Spawned default Home (id={hd.home_id}).");
            }
            else
            {
                Debug.LogWarning($"[PlaceableStore] Default Home prefab not found for id={defaultHomeId}");
            }
        }
        else
        {
            Debug.LogWarning($"[PlaceableStore] Default Home id={defaultHomeId} not found in DB.");
        }
    }

    /// <summary>집이 2채 이상이면 1채만 남기고 나머지는 제거</summary>
    private void CullExtraHomes()
    {
#if UNITY_2022_2_OR_NEWER
        var tags = FindObjectsByType<PlaceableTag>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var tags = FindObjectsOfType<PlaceableTag>();
#endif
        Transform firstHome = null;

        for (int i = 0; i < tags.Length; i++)
        {
            var t = tags[i];
            if (!t || t.category != PlaceableCategory.Home) continue;

            if (firstHome == null)
            {
                firstHome = t.transform;
                continue;
            }

            // 첫 번째를 제외하고 제거
            Destroy(t.gameObject);
            Debug.Log("[PlaceableStore] Removed extra Home instance.");
        }
    }

    #endregion

    #region === Internals: DB Caches & Lookups ===

    private void BuildDbCaches()
    {
        _homeById = BuildDict(homeDB?.homeList, d => d.home_id);
        _animalById = BuildDict(animalDB?.animalList, d => d.animal_id);
        _decoById = BuildDict(decoDB?.decoList, d => d.deco_id);
    }

    private static Dictionary<int, T> BuildDict<T>(IList<T> list, Func<T, int> keySelector) where T : class
    {
        var dict = new Dictionary<int, T>(capacity: list == null ? 0 : list.Count);
        if (list == null) return dict;

        for (int i = 0; i < list.Count; i++)
        {
            var it = list[i];
            if (it == null) continue;
            int key = keySelector(it);
            if (!dict.ContainsKey(key))
                dict.Add(key, it);
        }
        return dict;
    }

    private bool TryGetPrefabAndName(PlaceableCategory cat, int id, out GameObject prefab, out string displayName)
    {
        prefab = null; displayName = $"Item {id}";

        switch (cat)
        {
            case PlaceableCategory.Home:
                if (_homeById != null && _homeById.TryGetValue(id, out var hd) && hd != null)
                {
                    prefab = hd.GetPrefab(_loader);
                    displayName = string.IsNullOrEmpty(hd.home_name) ? displayName : hd.home_name;
                }
                break;

            case PlaceableCategory.Animal:
                if (_animalById != null && _animalById.TryGetValue(id, out var ad) && ad != null)
                {
                    prefab = ad.GetPrefab(_loader);
                    displayName = string.IsNullOrEmpty(ad.animal_name) ? displayName : ad.animal_name;
                }
                break;

            case PlaceableCategory.Deco:
                if (_decoById != null && _decoById.TryGetValue(id, out var dd) && dd != null)
                {
                    prefab = dd.GetPrefab(_loader);
                    displayName = string.IsNullOrEmpty(dd.deco_name) ? displayName : dd.deco_name;
                }
                break;
        }

        if (!prefab)
        {
            Debug.LogWarning($"[PlaceableStore] Prefab not found for {cat}:{id}");
            return false;
        }
        return true;
    }

    #endregion
}
