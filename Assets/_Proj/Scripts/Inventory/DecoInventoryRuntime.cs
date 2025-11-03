using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실제로 플레이어가 "몇 개를 들고 있는지"만 관리하는 런타임 인벤토리.
/// - 키: decoId (int)
/// - 값: count (int)
/// - PlayerPrefs 로 저장/불러오기
/// - DB(DecoDatabase) 와 1:1로 맞물려서 돌아감
/// </summary>
public class DecoInventoryRuntime : MonoBehaviour
{
    public static DecoInventoryRuntime I { get; private set; }

    [Header("Database")]
    [SerializeField] private DecoDatabase database;

    [Serializable]
    public class StartEntry
    {
        public int decoId;
        public int count = 1;
    }

    [Header("Initial Items (for test)")]
    [SerializeField] private List<StartEntry> startItems = new();  // 테스트용 초기 수량

    // 실제 들고 있는 수량
    private readonly Dictionary<int, int> _counts = new();

    /// <summary>인벤 내용이 바뀔 때마다 (decoId, newCount) 를 알려준다.</summary>
    public event Action<int, int> OnChanged;

    private const string PREF_KEY_PREFIX = "DecoInv_";

    private void Awake()
    {
        // 싱글턴
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;

        _counts.Clear();

        // 1) 저장된 것 먼저 불러옴
        LoadAll();

        // 2) 인스펙터에서 세팅해둔 startItems로 최소 개수 보장
        foreach (var e in startItems)
        {
            if (e.decoId <= 0) continue;

            int cur = Count(e.decoId);
            if (cur < e.count)
            {
                _counts[e.decoId] = e.count;
                OnChanged?.Invoke(e.decoId, e.count);
            }
        }

        // 3) 최종 상태 저장
        SaveAll();
    }

    /// <summary>참조용 DB</summary>
    public DecoDatabase DB => database;

    /// <summary>DB에서 데이터 1개 가져오기</summary>
    public DecoData GetData(int decoId)
    {
        if (!database) return null;
        return database.decoList.Find(d => d.deco_id == decoId);
    }

    /// <summary>해당 decoId의 현재 보유 수량</summary>
    public int Count(int decoId)
    {
        return _counts.TryGetValue(decoId, out var c) ? c : 0;
    }

    /// <summary>해당 decoId를 n개 추가</summary>
    public void Add(int decoId, int n = 1)
    {
        if (decoId <= 0 || n <= 0) return;
        int newCount = Count(decoId) + n;
        _counts[decoId] = newCount;
        OnChanged?.Invoke(decoId, newCount);
    }

    /// <summary>해당 decoId를 n개 소비 (성공하면 true)</summary>
    public bool TryConsume(int decoId, int n = 1)
    {
        if (decoId <= 0 || n <= 0) return false;

        int cur = Count(decoId);
        if (cur < n) return false;

        int newCount = cur - n;
        _counts[decoId] = newCount;
        OnChanged?.Invoke(decoId, newCount);
        return true;
    }

    /// <summary>모든 (id, count) 쌍을 리스트로 반환 (편집모드 스냅샷용)</summary>
    public List<(int id, int count)> GetAllCounts()
    {
        var list = new List<(int, int)>(_counts.Count);
        foreach (var kv in _counts)
            list.Add((kv.Key, kv.Value));
        return list;
    }

    /// <summary>
    /// EditModeController 가 저장해둔 스냅샷으로 되돌리기
    /// </summary>
    public void RestoreFromSnapshot(List<EditModeController.InventorySnapshot> snap)
    {
        _counts.Clear();
        foreach (var s in snap)
        {
            _counts[s.id] = s.count;
            OnChanged?.Invoke(s.id, s.count);
        }
    }

    /// <summary>
    /// PlayerPrefs 로 전체 저장
    /// </summary>
    public void SaveAll()
    {
        if (database)
        {
            // DB가 있으면 DB의 목록대로 저장
            foreach (var d in database.decoList)
            {
                if (d == null) continue;
                int c = Count(d.deco_id);
                PlayerPrefs.SetInt(PREF_KEY_PREFIX + d.deco_id, c);
            }
        }
        else
        {
            // DB가 없으면 현재 Dictionary 에 있는 것만 저장
            foreach (var kv in _counts)
                PlayerPrefs.SetInt(PREF_KEY_PREFIX + kv.Key, kv.Value);
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// PlayerPrefs 에서 값들을 불러온다.
    /// 하나라도 불러오면 true.
    /// </summary>
    private bool LoadAll()
    {
        bool any = false;

        if (database)
        {
            // DB가 있으면 DB에 있는 id만큼만 로드
            foreach (var d in database.decoList)
            {
                if (d == null) continue;
                string key = PREF_KEY_PREFIX + d.deco_id;
                if (PlayerPrefs.HasKey(key))
                {
                    int c = PlayerPrefs.GetInt(key, 0);
                    _counts[d.deco_id] = c;
                    any = true;
                }
            }
        }

        // 불러온 값이 있으면 한 번에 이벤트 쏴서 UI 갱신
        if (any)
        {
            foreach (var kv in _counts)
                OnChanged?.Invoke(kv.Key, kv.Value);
        }

        return any;
    }
}
