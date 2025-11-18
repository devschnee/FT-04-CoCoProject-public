using System;
using UnityEngine;

[Serializable]
public class StageData
{
    public string stage_id;
    public string stage_name;
    public string stage_img;
    public string stage_desc;
    public string map_id;
    public string start_cutscene;
    public string start_talk;
    public string end_talk;
    public string end_cutscene;
    public string treasure_01_id;
    public string treasure_02_id;
    public string treasure_03_id;
    public string dialogue_box_1;
    public string dialogue_box_2;
    public string dialogue_box_3;
    public string dialogue_box_4;
    public string dialogue_box_5;
    public string dialogue_box_6;
    public string dialogue_box_7;
    public string dialogue_box_8;
    public string dialogue_box_9;
    public string stage_bgm;

    [NonSerialized] public Sprite icon;
    [NonSerialized] public AudioClip audio;

    public Sprite GetIcon(IResourceLoader loader)
    {
        if (icon == null && !string.IsNullOrEmpty(stage_img))
            icon = loader.LoadSprite(stage_img);
        return icon;
    }

    public string GetStartCutscenePath()
    {
        if (string.IsNullOrEmpty(start_cutscene))
            return null;

        return start_cutscene;
    }

    public string GetEndCutscenePath()
    {
        if (string.IsNullOrEmpty(end_cutscene))
            return null;

        return end_cutscene;
    }
    public AudioClip GetAudio(IResourceLoader loader)
    {
        if (audio == null && !string.IsNullOrEmpty(stage_bgm))
            audio = loader.LoadAudio(stage_bgm);
        return audio;
    }
}
