using System;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class AudioVolumeData
{
    public float Master = 1f;
    public float BGM = 1f;
    public float SFX = 1f;
    // public float Ambient = 1f; // 합칠 시 삭제
    // public float Cutscene = 1f; // 합칠 시 삭제
    // public float Voice = 1f;
    //public bool MuteMaster = false;
    
}

public class AudioVolumeHandler
{
    private readonly AudioMixer mixer;
    private const string MasterParam = "MasterVolumeParam";
    private const string BGMParam = "BGMVolumeParam";
    private const string SFXParam = "SFXVolumeParam";
    // private const string AmbientParam = "AmbientVolumeParam";
    // private const string CutsceneParam = "CutsceneVolumeParam";
    // private const string VoiceParam = "VoiceVolumeParam";

    public AudioVolumeHandler(AudioMixer mixer)
    {
        this.mixer = mixer;
    }
    
    public void ApplyVolumes(AudioVolumeData data)
    {
        //if (data.MuteMaster) mixer.SetFloat(MasterParam, -80f);
        //else 
        SetVolume(AudioType.Master, data.Master);
        SetVolume(AudioType.BGM, data.BGM);
        SetVolume(AudioType.SFX, data.SFX);
        SetVolume(AudioType.Ambient, data.SFX);
        SetVolume(AudioType.Cutscene, data.BGM);
        //SetVolume(AudioType.Voice, data.Voice);
    }

    public void SetVolume(AudioType type, float linear)
    {
        float dB = (linear <= 0.0001f) ? -80f : Mathf.Log10(linear) * 20f;
        switch (type)
        {
            case AudioType.BGM:
                mixer.SetFloat(BGMParam, dB);
                break;
            case AudioType.SFX:
                mixer.SetFloat(SFXParam, dB);
                break;
            // case AudioType.Ambient:
            //     mixer.SetFloat(AmbientParam, dB);
            //     break;
            // case AudioType.Cutscene:
            //     mixer.SetFloat(CutsceneParam, dB);
            //     break;
            // case AudioType.Voice:
            //     mixer.SetFloat(VoiceParam, dB);
            //     break;
            case AudioType.Master:
                mixer.SetFloat(MasterParam, dB);
                break;
        }
    }

    // public void MuteMaster(bool mute)
    // {
        
    // }

    // ���� �������� �� �������� �� �ƴ��ݾ� ���� �� ���ø� �ϸ� ����
    //public float GetVolume(AudioType type)
    //{
    //    string paramName;
    //    switch (type)
    //    {
    //        case AudioType.BGM:
    //            paramName = BGMParam;
    //            break;
    //        case AudioType.SFX:
    //            paramName = SFXParam;
    //            break;
    //        case AudioType.Ambient:
    //            paramName = AmbientParam;
    //            break;
    //        case AudioType.Cutscene:
    //            paramName = CutsceneParam;
    //            break;
    //        case AudioType.Voice:
    //            paramName = VoiceParam;
    //            break;
    //        case AudioType.Master:
    //            paramName = MasterParam;
    //            break;
    //        default: throw new Exception("Type: BGM, SFX, Ambient, Cutscene, Voice, Master");
    //    }

    //    if (mixer.GetFloat(paramName, out float dB))
    //    {
    //        float linear = Mathf.Pow(10, dB / 20f);
    //        return Mathf.Clamp01(linear);
    //    }
    //    return 1f;
    //}
}
