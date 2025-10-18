using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "TileRegistry")]
public class TileRegistry : ScriptableObject
{
    [Serializable] 
    public class TileBinding
    {
        public string code; // "G", "W", "P", "I", "B" ...
        public GameObject prefab;
        public bool blocking; // 벽 등
        public bool water; // 물
        public bool ice; // 빙판
        public bool pit; // 구덩이
        public bool swamp; // 늪
    }

    [Serializable]
    public class EntityBinding
    {
        public string type; // "spawn", "goal", "box", "door", "bridge", ...
        public GameObject prefab;
        
    }

    public List<TileBinding> tiles;
    public List<EntityBinding> entities;

    Dictionary<string, TileBinding> tmap;
    Dictionary<string, EntityBinding> emap;

    public void Init()
    {
        if (tmap == null)
        {
            tmap = new();
            foreach(var t in tiles)
            {
                if (!string.IsNullOrEmpty(t.code))
                {
                    tmap[t.code] = t;
                }
            }
        }
        if (emap == null)
        {
            emap = new();
            foreach (var e in entities)
            {
                if (!string.IsNullOrEmpty(e.type))
                {
                    emap[e.type] = e;
                }
            }
        }
    }

    public TileBinding GetTile(string code)
    {
        Init();
        return tmap != null && tmap.TryGetValue(code, out var x) ? x : null;
    }

    public EntityBinding GetEntity(string type) { 
        Init();
        return emap != null && emap.TryGetValue(type, out var x) ? x : null;
    }

    public bool HasCode(string code)
    {
        Init(); 
        return tmap != null && tmap.ContainsKey(code);
    }

    public bool HasEntity(string type)
    {
        Init();
        return emap != null && emap.ContainsKey(type);
    }
}
