using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// 배치물 Placeable(집/동물/조경) 저장·복원 매니저
/// - Firebase(UserData.Local.lobby) 데이터로 복원
/// - Firebase 데이터가 없으면 기본 집(defaultHomeId) 자동 생성
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

    [Serializable]
    public struct Placed
    {
        public PlaceableCategory cat;
        public int id;
        public Vector3 pos;
        public Quaternion rot;
    }

    // PlayerPrefs JSON용 Wrapper 였던 것. 구조는 남겨둠 (필요 시 재사용 가능)
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

    #region === Public API : 저장 ===

    /// <summary>
    /// 현재 씬에 배치된 PlaceableTag를 읽어서 Placed 리스트로 반환.
    /// - Firebase 저장(TrySaveLobbyToFirebase)에서 사용.
    /// </summary>
    public List<Placed> CollectPlacedFromScene()
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

            // 인벤에서 꺼냈지만 아직 OK 안 누른 임시물은 저장하지 않음
            if (tag.GetComponent<InventoryTempMarker>()) continue;

            list.Add(new Placed
            {
                cat = tag.category,
                id = tag.id,
                pos = tag.transform.position,
                rot = tag.transform.rotation
            });
        }

        return list;
    }

    /// <summary>
    /// [사용 중단] 기존 PlayerPrefs 저장용 함수.
    /// 멀티 계정에서 다른 유저 배치가 섞이는 문제 때문에 내용 비움.
    /// </summary>
    public void SaveAllFromScene()
    {
        // 이제 저장은 EditModeController.TrySaveLobbyToFirebase()에서
        // Firebase(UserData.Local.lobby) 경로로만 처리한다.
        // 이 메서드는 호환성을 위해 남겨두었지만 아무 것도 하지 않음.
    }

    /// <summary>
    /// [사용 중단] 기존 PlayerPrefs 삭제용 함수. 현재는 아무 것도 하지 않음.
    /// </summary>
    public void ClearSaved()
    {
        // PlayerPrefs 기반 저장 방식을 더 이상 사용하지 않는다.
    }

    #endregion

    #region === Public API : 복원 진입점 ===

    /// <summary>
    /// 저장 데이터를 읽어 모두 스폰 + 기본 집 보장 + 중복 집 정리
    /// - 1순위: UserData.Local.lobby (Firebase) 데이터가 있으면 그걸 사용
    /// - 2순위: 없으면 기본 집(defaultHomeId)만 생성
    /// </summary>
    public void LoadAndSpawnAll()
    {
        // 0) Firebase Lobby 데이터가 있으면 그것부터 시도
        if (TryLoadFromFirebaseLobby())
        {
            Debug.Log("[PlaceableStore] Firebase Lobby 데이터로 복원 완료");
            return;
        }

        // 1) Firebase 데이터도 없으면 기본 집을 바로 생성
        EnsureSingleHomeExists();

        // 2) 혹시 중복된 집이 있으면 1채만 남김
        CullExtraHomes();
    }

    #endregion

    #region === Internals: Firebase Lobby 경로 ===

    /// <summary>
    /// UserData.Local.lobby(props) 기반으로 로비 배치 복원 시도.
    /// 성공하면 true, Firebase 데이터가 없거나 비어 있으면 false.
    /// </summary>
    private bool TryLoadFromFirebaseLobby()
    {
        if (UserData.Local == null || UserData.Local.lobby == null)
            return false;

        var lobby = UserData.Local.lobby;

        // props 자체가 없거나 비어 있으면 Firebase 쪽에 배치 데이터 없는 것으로 간주
        if (lobby.props == null || lobby.props.Count == 0)
            return false;

        // Lobby.props → Placed 리스트로 변환
        var placedFromServer = lobby.ToPlacedList();
        if (placedFromServer == null || placedFromServer.Count == 0)
        {
            // props는 있었지만 실제 배치가 0개인 경우: 집만 기본 생성
            EnsureSingleHomeExists();
            CullExtraHomes();
            return true;
        }

        // NavMesh 재빌드
        RebuildNavMeshIfExists();

        // 리스트 기반으로 스폰
        SpawnFromPlacedList(placedFromServer);

        // 집 1채 보장 + 중복 제거
        EnsureSingleHomeExists();
        CullExtraHomes();

        Debug.Log($"[PlaceableStore] Firebase Lobby 데이터로 {placedFromServer.Count}개 복원");
        return true;
    }

    /// <summary>
    /// NavMeshSurface가 있으면 한 번 빌드해줌 ("IsLandPlane" 기준)
    /// </summary>
    private void RebuildNavMeshIfExists()
    {
        GameObject gObj = GameObject.Find("IsLandPlane");
        if (!gObj) return;

        var nMS = gObj.GetComponent<NavMeshSurface>();
        if (!nMS) return;

        nMS.BuildNavMesh();
    }

    #endregion

    #region === Internals: Placed 리스트 → 실제 오브젝트 스폰 ===

    /// <summary>
    /// Placed 리스트를 받아 실제 게임오브젝트들을 Instantiate 한다.
    /// - TryGetPrefabAndName()으로 Prefab 결정
    /// - PlaceableTag, Draggable, NavMeshObstacle/AnimalBehaviour/tag 등 셋업
    /// </summary>
    public void SpawnFromPlacedList(List<Placed> items)
    {
        if (items == null) return;

        int ok = 0, fail = 0;

        foreach (var p in items)
        {
            if (!TryGetPrefabAndName(p.cat, p.id, out var prefab, out var name))
            {
                fail++;
                continue;
            }

            var go = Instantiate(prefab, p.pos, p.rot);
            go.name = name;

            var tag = go.GetComponent<PlaceableTag>() ?? go.AddComponent<PlaceableTag>();
            tag.category = p.cat;
            tag.id = p.id;

            if (!go.GetComponent<Draggable>()) go.AddComponent<Draggable>();

            // LSH 추가: 카테고리별 컴포넌트 세팅 (기존 LoadAndSpawnAll 로직 재사용)
            switch (p.cat)
            {
                case PlaceableCategory.Home:
                    if (go.GetComponent<NavMeshModifier>() == null) go.AddComponent<NavMeshModifier>();
                    var homeModifier = go.GetComponent<NavMeshModifier>();
                    homeModifier.overrideArea = true;
                    homeModifier.area = 1;
                    break;

                case PlaceableCategory.Animal:
                    go.tag = "Animal";
                    break;

                case PlaceableCategory.Deco:
                    if (go.GetComponent<NavMeshModifier>() == null)
                        go.AddComponent<NavMeshModifier>();
                    var decoModifier = go.GetComponent<NavMeshModifier>();
                    decoModifier.overrideArea = true;
                    decoModifier.area = 1;
                    go.tag = "Decoration";
                    break;
            }

            ok++;
        }

        Debug.Log($"[PlaceableStore] SpawnFromPlacedList: {ok}개 복원, 실패 {fail}개");
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
                // ✅ 회전 규칙
                // id 40001 → Y = 180도
                // id 40002 → Y = 90도
                // id 40003 → Y = 0도
                // id 40004 → Y = 0도
                // (그 외 id는 0도 기본)
                float yRot = 0f;

                if (hd.home_id == 40001)
                    yRot = 180f;
                else if (hd.home_id == 40002)
                    yRot = 90f;
                // 40003, 40004는 0f 그대로

                Quaternion rot = Quaternion.Euler(0f, yRot, 0f);

                var go = Instantiate(prefab, Vector3.zero, rot);
                go.name = string.IsNullOrEmpty(hd.home_name) ? $"Home {hd.home_id}" : hd.home_name;

                var tag = go.GetComponent<PlaceableTag>() ?? go.AddComponent<PlaceableTag>();
                tag.category = PlaceableCategory.Home;
                tag.id = hd.home_id;

                if (!go.GetComponent<Draggable>()) go.AddComponent<Draggable>();

                Debug.Log($"[PlaceableStore] Spawned default Home (id={hd.home_id}, rotY={rot.eulerAngles.y}).");
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
