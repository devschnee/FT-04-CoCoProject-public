using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpeakerData
{
    public string speaker_id;
    public string display_name;
    public string portrait_set_prefix;
    [NonSerialized] public Dictionary<string, Sprite> portraits = new();

    public Sprite GetPortrait(EmotionType emotion, IResourceLoader loader)
    {
        string emotionString = emotion.ToString();
        // 런타임 이미지 파일 이름: prefix + emotion + .png (예: coco_Happy.png)
        string path = portrait_set_prefix + emotionString;

        if (!portraits.ContainsKey(emotionString))
        {
            Sprite sp = loader.LoadSprite(path);
            if(sp != null)
            {
                portraits.Add(emotionString, sp);
            }
            return sp;
        }
        return portraits[emotionString];
    }
}
