using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpeakerData
{
    public SpeakerId speaker_id;
    public string display_name;
    public string portrait_set_prefix;
    [NonSerialized] public Sprite icon;

    public enum SpeakerId
    {
        coco, hog, tortoise, buffalo, android_owner
    }


    public Sprite GetPortrait(IResourceLoader loader)
    {
        if (icon == null && !string.IsNullOrEmpty(portrait_set_prefix))
            icon = loader.LoadSprite(portrait_set_prefix);
        return icon;
    }
}
