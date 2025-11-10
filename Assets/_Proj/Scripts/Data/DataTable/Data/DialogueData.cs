using System;

[Serializable]
public class DialogueData
{
    public string dialogue_id;
    public int seq;
    public SpeakerPosition speaker_position;
    public SpeakerType speaker_id;
    public EmotionType emotion;
    public string text;
    public float char_delay;
    public SoundType sound_type;
    public string sound_key;
}

public enum SpeakerPosition
{
    left, right
}

public enum SpeakerType
{
    coco, hog, tortoise, buffale, android_owner
}

public enum EmotionType
{
    Neutral, Happy, Sad, Angry, Surprised
}

public enum SoundType
{
    sfx, sfx_none, bgm
}