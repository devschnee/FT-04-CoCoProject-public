
// 사실 오버코딩을 한 부분이 많지만 나중을 위해서.
public enum AudioType
{
    BGM = 0,
    SFX,
    Ambient,
    Cutscene,
    Voice,
    UI,
    Master,
    DialogueBGM,
    DialogueSFX
}

public enum BGMKey
{
    Main = 0,
    Title,
    Intro,
    Chapter01,
    Chapter02,
    Chapter03,
}

public enum SFXKey
{
    CocodoogyFootstep = 0,
    MasterFootstep,
    Master_AndroidFootstep,
    PigFootstep

}

public enum AmbientKey
{
    Birdsong = 0
}

// public enum CutsceneKey
// {
//     CutsceneId01 = 0
// }

// public enum VoiceKey
// {
//     Cocodoogy = 0,
//     Master,
//     Android,
//     Pig,
//     Bird

// }

public enum UIKey
{
    UIClick = 0,
    UIOpen,
    UIClose
}

public enum DialogueKey
{
    BGM = 0,
    SFX,
    Voice
}
