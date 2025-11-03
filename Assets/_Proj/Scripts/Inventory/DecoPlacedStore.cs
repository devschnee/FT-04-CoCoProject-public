using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 편집모드에서 "확정(OK)"된 데코들을 PlayerPrefs 에 저장해두고
/// 다음에 씬이 열릴 때 다시 깔아주는 역할.
/// 
/// 흐름:
/// - EditModeController 가 저장할 때 → SaveAllFromScene() 호출
/// - 이 스토어는 PlaceableTag_Deco 가 있는 애들만 스캔해서 저장
/// - InventoryTempMarker 가 붙은 애(아직 OK 안 한 임시물)는 저장하지 않음
/// </summary>
public class DecoPlacedStore : MonoBehaviour
{
    public static DecoPlacedStore I { get; private set; }

    [SerializeField] private DecoDatabase database;

    private const string PREF_KEY = "DecoPlacedStore_v1";

    [Serializable]
    public struct Placed
    {
        public int decoId;
        public Vector3 pos;
        public Quaternion rot;
    }

    private void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;

        // 시작할 때 저장돼 있던 거 다시 깔기
        LoadAndSpawnAll();
    }

    /// <summary>
    /// 현재 씬에 있는 "확정된" 데코들을 전부 저장한다.
    /// (인벤에서 꺼냈는데 아직 OK 안 누른 애들은 제외)
    /// </summary>
    public void SaveAllFromScene()
    {
        // 씬에 있는 PlaceableTag_Deco 전부 찾기
#if UNITY_2022_2_OR_NEWER
        var tags = FindObjectsByType<PlaceableTag_Deco>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var tags = FindObjectsOfType<PlaceableTag_Deco>();
#endif

        var list = new List<Placed>();

        foreach (var tag in tags)
        {
            if (!tag.gameObject.activeInHierarchy) continue;

            // 아직 OK 안 한 임시물은 저장 금지
            if (tag.GetComponent<InventoryTempMarker>()) continue;

            list.Add(new Placed
            {
                decoId = tag.decoId,
                pos = tag.transform.position,
                rot = tag.transform.rotation
            });
        }

        // JSON 으로 저장
        string json = JsonUtility.ToJson(new Wrapper { items = list }, prettyPrint: false);
        PlayerPrefs.SetString(PREF_KEY, json);
        PlayerPrefs.Save();

        Debug.Log($"[DecoPlacedStore] Saved {list.Count} placed objects.");
    }

    /// <summary>
    /// PlayerPrefs 에서 저장된 데코들을 읽어와서 다시 씬에 깐다.
    /// </summary>
    private void LoadAndSpawnAll()
    {
        if (!PlayerPrefs.HasKey(PREF_KEY)) return;

        string json = PlayerPrefs.GetString(PREF_KEY, "");
        if (string.IsNullOrEmpty(json)) return;

        var w = JsonUtility.FromJson<Wrapper>(json);
        if (w == null || w.items == null) return;

        foreach (var p in w.items)
        {
            // DB에서 decoId에 해당하는 데이터 찾기
            var data = database
                ? database.decoList.Find(d => d != null && d.deco_id == p.decoId)
                : null;

            if (data == null)
            {
                Debug.LogWarning($"[DecoPlacedStore] decoId={p.decoId} 를 DB에서 찾지 못했습니다.");
                continue;
            }

            var prefab = DataManager.Instance.Deco.GetPrefab(data.deco_id);
            if (!prefab)
            {
                Debug.LogWarning($"[DecoPlacedStore] decoId={p.decoId} 에 prefab이 없습니다.");
                continue;
            }

            // 실제 스폰
            var go = Instantiate(prefab, p.pos, p.rot);
            go.name = data.deco_name;

            // 태그 다시 붙이기
            var tag = go.GetComponent<PlaceableTag_Deco>();
            if (!tag) tag = go.AddComponent<PlaceableTag_Deco>();
            tag.decoId = p.decoId;

            // 드래그도 다시
            var drag = go.GetComponent<Draggable>();
            if (!drag) drag = go.AddComponent<Draggable>();
        }

        Debug.Log($"[DecoPlacedStore] Restored {w.items.Count} placed objects.");
    }

    [Serializable]
    private class Wrapper
    {
        public List<Placed> items;
    }
}
