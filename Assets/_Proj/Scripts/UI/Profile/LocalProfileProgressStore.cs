using System;
using System.Collections.Generic;

// 인터페이스랑 1:1로 맞춘 테스트용 구현
public class LocalProfileProgressStore : IProfileProgressStore
{
    // 메모리에만 들고 있을 캐시
    private readonly Dictionary<ProfileType, HashSet<int>> _cache =
        new Dictionary<ProfileType, HashSet<int>>();

    // ★ 인터페이스에 있는 시그니처랑 100% 같아야 함
    public Dictionary<ProfileType, HashSet<int>> LoadAll()
    {
        // 이미 초기화돼 있으면 그대로 돌려줌
        if (_cache.Count > 0)
            return _cache;

        // 모든 타입에 대해 빈 셋 만들어두기
        foreach (ProfileType t in Enum.GetValues(typeof(ProfileType)))
        {
            _cache[t] = new HashSet<int>();
        }

        // 여기서 테스트용으로 몇 개 열어보자
        // 아이콘 0, 2 해금
        _cache[ProfileType.icon].Add(0);
        _cache[ProfileType.icon].Add(2);

        // 동물 1번 해금
        _cache[ProfileType.animal].Add(1);

        return _cache;
    }

    public bool IsUnlocked(ProfileType type, int id)
    {
        var all = LoadAll(); // 항상 여기서 가져오면 null 안 나옴
        return all.TryGetValue(type, out var set) && set.Contains(id);
    }

    public void Unlock(ProfileType type, int id)
    {
        var all = LoadAll();
        if (!all.TryGetValue(type, out var set))
        {
            set = new HashSet<int>();
            all[type] = set;
        }
        set.Add(id);
    }
}