using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System;
/// <summary>
/// BGM, Cutscene 합칠 것,
/// SFX, Ambient 합칠 것
/// 하지만 재생은 불할 할 생각
/// 그래야 내 머리로서는 상태 제어가 편함
/// 
/// </summary>
[Serializable]
public struct AudioGroupMapping
{
    public AudioType type;
    public AudioMixerGroup group;
}
[DefaultExecutionOrder(-99)]
public class AudioManager : MonoBehaviour, IAudioGroupSetting
{
    public static AudioManager Instance { get; private set; }
    public static IAudioGroupSetting AudioGroupProvider { get; private set; }

    [Header("Mixer & Group Settings")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioGroupMapping[] groupMappings;

    [Header("Audio Libraries")]
    [SerializeField] private BGMLibrary bgmLibrary;
    [SerializeField] private SFXLibrary sfxLibrary;
    [SerializeField] private AmbientLibrary ambientLibrary;
    [SerializeField] private CutsceneLibrary cutsceneLibrary;
    [SerializeField] private VoiceLibrary voiceLibrary;
    [SerializeField] private UILibrary uiLibrary;

    [Header("AudioGroupChildren")]
    private BGMGroup bgmGroup;
    private SFXGroup sfxGroup;
    private AmbientGroup ambientGroup;
    private CutsceneGroup cutsceneGroup;
    private VoiceGroup voiceGroup;
    private UIGroup uiGroup;

    private Dictionary<AudioType, AudioMixerGroup> groupMap;
    private AudioLibraryProvider libraryProvider;
    private AudioVolumeHandler volumeHandler;
    private IAudioController[] audioGroups;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        groupMap = new Dictionary<AudioType, AudioMixerGroup>();
        foreach (var map in groupMappings)
        {
            groupMap[map.type] = map.group;
        }

        AudioGroupProvider = this;

        libraryProvider = new AudioLibraryProvider(bgmLibrary, sfxLibrary, ambientLibrary, cutsceneLibrary, voiceLibrary, uiLibrary);
        volumeHandler = new AudioVolumeHandler(mixer);

        // AudioGroupMapping
        bgmGroup = GetComponentInChildren<BGMGroup>();
        sfxGroup = GetComponentInChildren<SFXGroup>();
        ambientGroup = GetComponentInChildren<AmbientGroup>();
        cutsceneGroup = GetComponentInChildren<CutsceneGroup>();
        voiceGroup = GetComponentInChildren<VoiceGroup>();
        uiGroup = GetComponentInChildren<UIGroup>();

        audioGroups = new IAudioController[6] { bgmGroup, sfxGroup, ambientGroup, cutsceneGroup, voiceGroup, uiGroup };

        // ���� �ҷ�����
        if (SettingManager.Instance == null)
        {
            var settingGO = new GameObject("SettingManager");
            settingGO.AddComponent<SettingManager>();
        }
        var volumeData = SettingManager.Instance.settingData;
        volumeHandler.ApplyVolumes(volumeData.audio);
        //
        

    }

    public AudioMixer GetMixer()
    {
        return mixer;
    }

    public AudioMixerGroup GetGroup(AudioType type)
    {
        return groupMap[type];
    }

    // ���
    public void PlayAudio(Enum key, int index = -1, float fadeIn = 0, float fadeOut = 0, bool loop = false, bool pooled = false, Vector3? pos = null)
    {
        switch (key)
        {
            case BGMKey bk:
                PlayAudio(bk, index, fadeIn, fadeOut, loop);
                break;
            case SFXKey sk:
                PlayAudio(sk, index, loop, pooled, pos);
                break;
            case AmbientKey ak:
                PlayAudio(ak, index, loop, pooled, pos);
                break;
            case CutsceneKey ck:
                PlayAudio(ck, index, fadeIn, fadeOut, loop);
                break;
            case VoiceKey vk:
                PlayAudio(vk, index);
                break;
            case UIKey uk:
                PlayAudio(uk, index);
                break;
            default: throw new Exception("Key: BGMKey, SFXKey, AmbientKey, CutsceneKey, VoiceKey, UIKey");
        }
    }

    #region ���������б�
    private void PlayAudio(BGMKey key, int index = -1, float fadeIn = 1f, float fadeOut = 1f, bool loop = true)
    {
        var clip = libraryProvider.GetClip(AudioType.BGM, key, index);
        if (clip == null) return;
        // ����
        bgmGroup.PlayBGM(clip, fadeIn, fadeOut, loop);
    }
    private void PlayAudio(SFXKey key, int index = -1, bool loop = false, bool pooled = false, Vector3? pos = null)
    {
        var clip = libraryProvider.GetClip(AudioType.SFX, key, index);
        if (clip == null) return;
        // ����
        sfxGroup.PlaySFX(clip, loop, pooled, pos);
    }
    private void PlayAudio(AmbientKey key, int index = -1, bool loop = false, bool pooled = false, Vector3? pos = null)
    {
        var clip = libraryProvider.GetClip(AudioType.Ambient, key, index);
        if (clip == null) return;
        // ����
        ambientGroup.PlayAmbient(clip, loop, pooled, pos);
    }
    private void PlayAudio(CutsceneKey key, int index = -1, float fadeIn = 1f, float fadeOut = 1f, bool loop = true)
    {
        var clip = libraryProvider.GetClip(AudioType.Cutscene, key, index);
        if (clip == null) return;
        // ����
        cutsceneGroup.PlayCutscene(clip, fadeIn, fadeOut, loop);
    }
    private void PlayAudio(VoiceKey key, int index = -1)
    {
        var clip = libraryProvider.GetClip(AudioType.Voice, key, index);
        if (clip == null) return;
        // ����
        voiceGroup.PlayVoice(clip);
    }
    private void PlayAudio(UIKey key, int index = -1)
    {
        var clip = libraryProvider.GetClip(AudioType.UI, key, index);
        if (clip == null) return;
        // ����
        uiGroup.PlayVoice(clip);
    }
    #endregion

    // ����� ��Ʈ��
    public void ResetAllAudioGroup()
    {
        foreach (var r in audioGroups)
        {
            r.ResetPlayer();
        }
    }

    public void StopAllAudioGroup()
    {
        foreach (var r in audioGroups)
        {
            r.StopPlayer();
        }
    }

    // ����
    // ���� VolumeUI <-(�ǽð� ����������� ���� �ݿ�)-> AudioVolumeHandler(AudioManager�� ����) AudioManager <-(����������� ����, �ҷ�����)-> SettingManager
    public void SetVolume(AudioType type, float value)
    {
        var data = SettingManager.Instance.settingData.audio;
        switch (type)
        {
            case AudioType.BGM:
                data.BGM = value;
                break;
            case AudioType.SFX:
                data.SFX = value;
                break;
            case AudioType.Ambient:
                data.Ambient = value;
                break;
            case AudioType.Cutscene:
                data.Cutscene = value;
                break;
            case AudioType.Voice:
                data.Voice = value;
                break;
            case AudioType.Master:
                data.Master = value;
                break;
        }
        SettingManager.Instance.settingData.audio = data;

        volumeHandler.SetVolume(type, value);
        SettingManager.Instance.SaveSettings();
    }

    public float GetVolume(AudioType type)
    {
        var data = SettingManager.Instance.settingData.audio;
        switch (type)
        {
            case AudioType.BGM:
                return data.BGM;
            case AudioType.SFX:
                return data.SFX;
            case AudioType.Ambient:
                return data.Ambient;
            case AudioType.Cutscene:
                return data.Cutscene;
            case AudioType.Voice:
                return data.Voice;
            case AudioType.Master:
                return data.Master;
        }
        return -1f;
    }

}

