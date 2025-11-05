using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlaceableStore : MonoBehaviour
{
    public static PlaceableStore I { get; private set; }

    [Header("Databases")]
    [SerializeField] private HomeDatabase homeDB;
    [SerializeField] private AnimalDatabase animalDB;
    [SerializeField] private DecoDatabase decoDB;

    // 버전 업 시 PREF_KEY_PREFIX만 바꾸면 이전 데이터와 충돌 없음
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
    private class Wrapper { public List<Placed> items; }

    // ---- 캐시 (로딩 시 성능 향상) ----
    private Dictionary<int, HomeData> _homeById;
    private Dictionary<int, AnimalData> _animalById;
    private Dictionary<int, DecoData> _decoById;

    private ResourcesLoader _loader;

    private string PrefKeyForCurrentScene =>
        PREF_KEY_PREFIX + SceneManager.GetActiveScene().name;

    private void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;

        _loader = new ResourcesLoader();
        BuildDbCaches();
        LoadAndSpawnAll(); // 씬 진입 시 자동 복원
    }

    // ─────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────

    /// <summary>씬 내 PlaceableTag들을 스캔해 모두 저장</summary>
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
            if (tag.GetComponent<InventoryTempMarker>()) continue; // 임시 배치는 제외

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

    /// <summary>저장된 내용을 읽어 프리팹을 모두 스폰</summary>
    public void LoadAndSpawnAll()
    {
        string key = PrefKeyForCurrentScene;
        if (!PlayerPrefs.HasKey(key)) return;

        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json)) return;

        var w = JsonUtility.FromJson<Wrapper>(json);
        if (w?.items == null || w.items.Count == 0) return;

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
                ok++;
            }
            else
            {
                fail++;
            }
        }

        Debug.Log($"[PlaceableStore] Restored {ok} objects (failed {fail}). (sceneKey={key})");
    }

    /// <summary>현재 씬에 저장된 플레이스먼트 데이터 삭제</summary>
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

    // ─────────────────────────────────────────────────────────────
    // Internals
    // ─────────────────────────────────────────────────────────────

    private void BuildDbCaches()
    {
        _homeById = BuildDict(homeDB?.homeList, d => d.home_id);
        _animalById = BuildDict(animalDB?.animalList, d => d.animal_id);
        _decoById = BuildDict(decoDB?.decoList, d => d.deco_id);
    }

    private static Dictionary<int, T> BuildDict<T>(IList<T> list, Func<T, int> keySelector)
        where T : class
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
}
