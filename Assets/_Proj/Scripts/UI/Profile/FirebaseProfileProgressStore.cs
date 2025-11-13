using System;
using System.Collections.Generic;
using UnityEngine;

public class FirebaseProfileProgressStore : MonoBehaviour, IProfileProgressStore
{
    private Dictionary<ProfileType, HashSet<int>> _cache;
    private bool _loaded;

    public Dictionary<ProfileType, HashSet<int>> LoadAll()
    {
        if (_loaded) return _cache;

        _cache = new Dictionary<ProfileType, HashSet<int>>();
        foreach (ProfileType t in Enum.GetValues(typeof(ProfileType)))
        {
            // 테스트용: 0만 열기
            _cache[t] = new HashSet<int> { 0 };
        }

        _loaded = true;
        return _cache;
    }

    public bool IsUnlocked(ProfileType type, int id)
    {
        if (!_loaded) LoadAll();
        return _cache.TryGetValue(type, out var set) && set.Contains(id);
    }

    public void Unlock(ProfileType type, int id)
    {
        if (!_loaded) LoadAll();
        if (!_cache.TryGetValue(type, out var set))
        {
            set = new HashSet<int>();
            _cache[type] = set;
        }
        if (set.Add(id))
        {
            // 여기서 실제 Firebase 저장 호출
            Debug.Log($"[FirebaseProfileProgressStore] saved {type}/{id}");
        }
    }
}