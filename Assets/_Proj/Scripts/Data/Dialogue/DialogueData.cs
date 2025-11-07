using System;

[Serializable]
public class DialogueData
{
    public string dialogue_id;
    public int seq;
    public string speaker_position;
    public string speaker_id;
    public EmotionType emotion;
    public string text;
    public float char_delay;
    public string sound_type;
    public string sound_key;
}

public enum EmotionType
{
    Neutral, Happy, Sad, Angry, Surprised
}
