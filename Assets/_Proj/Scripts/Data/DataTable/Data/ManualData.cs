using System;
using UnityEngine;

[Serializable]
public class ManualData
{
    public int manual_id;
    public string manual_name;
    public string manual_img;
    public string manual_desc;


    [NonSerialized] public Sprite icon;
    public Sprite GetIcon(IResourceLoader loader)
    {
        if (icon == null && !string.IsNullOrEmpty(manual_img))
            icon = loader.LoadSprite(manual_img);
        return icon;
    }
}
