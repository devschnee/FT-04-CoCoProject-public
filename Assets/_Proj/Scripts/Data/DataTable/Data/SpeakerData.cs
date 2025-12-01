using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpeakerData
{
    public SpeakerId speaker_id;
    public string display_name;
    public string portrait_set_prefix;

    [NonSerialized] private Dictionary<string, Sprite> portraitCache = new();


    public enum SpeakerId
    {
        coco, hog, tortoise, buffalo, android_owner, helper
    }


     public Sprite GetPortrait(IResourceLoader loader, string portraitPath)
    {
        if (string.IsNullOrEmpty(portraitPath))
            return null;

        // 이미 로드된 캐시 있으면 바로 반환
        if (portraitCache.TryGetValue(portraitPath, out var cached))
            return cached;

        // 없으면 Resources에서 새로 로드
        var sprite = loader.LoadSprite(portraitPath);
        if (sprite != null)
        {
            portraitCache[portraitPath] = sprite;
            return sprite;
        }

        Debug.LogWarning($"[SpeakerData] Sprite not found: {portraitPath}");
        return null;
    }
}
