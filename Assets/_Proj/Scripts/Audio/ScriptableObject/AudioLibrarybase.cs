using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class AudioLibraryBase<T> : ScriptableObject, IAudioLibrary where T : Enum
{
    [Serializable]
    public struct AudioEntry
    {
        public T key;
        public AudioClip[] clips;
    }

    [SerializeField] AudioEntry[] audioEntries;
    private Dictionary<T, AudioClip[]> _dic;

    protected virtual void OnEnable()
    {
        _dic = new Dictionary<T, AudioClip[]>();
        foreach (var a in audioEntries)
        {
            _dic[a.key] = a.clips;
        }
    }

    public AudioClip GetClip(T key, int index)
    {
        if (_dic.TryGetValue(key, out var clips) && index >= -1 && index < clips.Length)
        {
            if (index == -1) // 랜덤 재생 
            {
                return clips[UnityEngine.Random.Range(0, clips.Length)];
            }
            return clips[index]; // 해당 index 재생
        }

        return null;
    }

    public AudioClip GetClipByEnum(Enum key, int index = -1)
    {
        if (key is T typedKey) return GetClip(typedKey, index);

        return null;
    }
}