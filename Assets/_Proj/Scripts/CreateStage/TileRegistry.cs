using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// NOTE : [Required] Package Manager에서 Addressables(2.7.4)를 Install
[CreateAssetMenu(menuName = "TileRegistry")]
public class TileRegistry : ScriptableObject
{
    [Serializable] 
    public class TileBinding
    {
        public string code; // "G", "W", "P", "I", "B" ...
        //public GameObject prefab;
        public AssetReferenceGameObject tileRef; // 프리팹의 주소 로드
        public bool blocking; // 벽 등
        public bool water; // 물
        public bool ice; // 빙판
        public bool pit; // 구덩이
        public bool swamp; // 늪

    }

    [Serializable] // 오브젝트/기믹 정의
    public class EntityBinding
    {
        public string type; // "spawn", "goal", "box", "door", "bridge", ...
        //public GameObject prefab;
        public AssetReferenceGameObject entityRef; // 엔터티의 주소 참조
    }

    // 인스펙터에서 볼 데이터 원본
    public List<TileBinding> tiles; 
    public List<EntityBinding> entities;

    // 런타임 조회
    Dictionary<string, TileBinding> tmap;
    Dictionary<string, EntityBinding> emap;

    // 같은 코드 재요청 시 재사용
    readonly Dictionary<string, AsyncOperationHandle<GameObject>> tileHandles = new();
    readonly Dictionary<string, AsyncOperationHandle<GameObject>> entityHandles = new();

    // 키 정규화 (대소문자 구분 없이 사용 "s", "S" 동일 취급)
    static string NormalizeKey(string s) => string.IsNullOrEmpty(s) ? string.Empty : s.Trim().ToUpperInvariant();

    // 맵 변환
    public void Init()
    {
        if (tmap == null)
        {
            tmap = new Dictionary<string, TileBinding>();
            foreach(var t in tiles)
            {
                var key = NormalizeKey(t?.code);
                if(!string.IsNullOrEmpty(key)) tmap[key] = t;
            }
        }
        if (emap == null)
        {
            emap = new Dictionary<string, EntityBinding>();
            foreach (var e in entities)
            {
                var key = NormalizeKey(e?.type);
                if (!string.IsNullOrEmpty(key)) emap[key] = e;
            }
        }
    }

    public TileBinding GetTile(string code)
    {
        Init();
        var key = NormalizeKey(code);
        return tmap != null && tmap.TryGetValue(key, out var x) ? x : null;
    }

    public EntityBinding GetEntity(string type) { 
        Init();
        var key = NormalizeKey(type);
        return emap != null && emap.TryGetValue(key, out var x) ? x : null;
    }

    // 존재 여부 체크
    public bool HasCode(string code)
    {
        Init(); 
        return tmap != null && tmap.ContainsKey(NormalizeKey(code));
    }

    public bool HasEntity(string type)
    {
        Init();
        return emap != null && emap.ContainsKey(NormalizeKey(type));
    }

    // Addressables 비동기 로드
    public AsyncOperationHandle<GameObject> LoadTilePrefabAsync(string code)
    {
        Init();
        var key = NormalizeKey(code);
        if (tileHandles.TryGetValue(key, out var cached) && cached.IsValid()) return cached;
        
        var binding = GetTile(code);
        if(binding != null && binding.tileRef != null && binding.tileRef.RuntimeKeyIsValid())
        {
            var handle = binding.tileRef.LoadAssetAsync<GameObject>();
            tileHandles[key] = handle;
            return handle;
        }
        Debug.LogError($"[TR] 타일 프리팹 코드 로드 불가 {code}");

        // 실패 시 default 반환. 호출 쪽에서 handle.IsValid() or handle.Status 확인 필요.
        return default;
    }

    public AsyncOperationHandle<GameObject> LoadEntityPrefabAsync(string type)
    {
        Init();
        var key = NormalizeKey(type);
        if(entityHandles.TryGetValue(key, out var cached) && cached.IsValid()) return cached;

        var binding = GetEntity(type);
        if(binding != null && binding.entityRef != null && binding.entityRef.RuntimeKeyIsValid())
        {
            var handle = binding.entityRef.LoadAssetAsync<GameObject>();
            entityHandles[key] = handle;
            return handle;
        }
        Debug.LogError($"[TR] 엔터티 프리팹 로드 불가 {type}");
        return default;
    }

    // 로드 해제
    // NOTE : 전달된 handle 사용 안 함. 내부 캐시 전체 해제.
    public void Release(AsyncOperationHandle<GameObject> handle)
    {
        foreach(var pair in tileHandles)
        {
            if (pair.Value.IsValid() && pair.Value.IsDone) Addressables.Release(pair.Value);
        }
        foreach(var pair in entityHandles)
        {
            if (pair.Value.IsValid() && pair.Value.IsDone) Addressables.Release(pair.Value);
        }

        tileHandles.Clear();
        entityHandles.Clear();
    }

#if UNITY_EDITOR
    // 중복 체크
    void OnValidate()
    {
        tmap = null;
        emap = null;

        var seenT = new HashSet<string>();
        foreach(var t in tiles)
        {
            var key = NormalizeKey(t?.code);
            if(string.IsNullOrEmpty(key)) continue;
            if (!seenT.Add(key)) Debug.LogWarning($"[TR] 중복 타일 코드 : {key}",this);
        }
        var seenE = new HashSet<string>();
        foreach(var e in entities)
        {
            var key = NormalizeKey(e?.type);
            if(string.IsNullOrEmpty(key)) continue;
            if(!seenE.Add(key)) Debug.LogWarning($"[TR] 중복 엔티티 타입 : {key}", this);
        }
    }
#endif
}
