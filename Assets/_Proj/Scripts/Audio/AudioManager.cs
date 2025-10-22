using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System;

[Serializable]
public struct AudioGroupMapping
{
    public AudioType type;
    public AudioMixerGroup group;
}
[DefaultExecutionOrder(-100)]
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
    private OptionVolumeManager volumeManager;

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
        volumeManager = new OptionVolumeManager(mixer);

        // AudioGroupMapping
        bgmGroup = GetComponentInChildren<BGMGroup>();
        sfxGroup = GetComponentInChildren<SFXGroup>();
        ambientGroup = GetComponentInChildren<AmbientGroup>();
        cutsceneGroup = GetComponentInChildren<CutsceneGroup>();
        voiceGroup = GetComponentInChildren<VoiceGroup>();
        uiGroup = GetComponentInChildren<UIGroup>();
    }

    public AudioMixer GetMixer()
    {
        return mixer;
    }

    public AudioMixerGroup GetGroup(AudioType type)
    {
        return groupMap[type];
    }

    // 재생
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
            default: throw new Exception("키는 BGMKey, SFXKey, AmbientKey, CutsceneKey, VoiceKey, UIKey");
        }
    }

    #region 오디오재생분기
    private void PlayAudio(BGMKey key, int index = -1, float fadeIn = 1f, float fadeOut = 1f, bool loop = true)
    {
        var clip = libraryProvider.GetClip(AudioType.BGM, key, index);
        if (clip == null) return;
        // 실행
        bgmGroup.PlayBGM(clip, fadeIn, fadeOut, loop);
    }
    private void PlayAudio(SFXKey key, int index = -1, bool loop = false, bool pooled = false, Vector3? pos = null)
    {
        var clip = libraryProvider.GetClip(AudioType.SFX, key, index);
        if (clip == null) return;
        // 실행
        sfxGroup.PlaySFX(clip, loop, pooled, pos);
    }
    private void PlayAudio(AmbientKey key, int index = -1, bool loop = false, bool pooled = false, Vector3? pos = null)
    {
        var clip = libraryProvider.GetClip(AudioType.Ambient, key, index);
        if (clip == null) return;
        // 실행
        ambientGroup.PlayAmbient(clip, loop, pooled, pos);
    }
    private void PlayAudio(CutsceneKey key, int index = -1, float fadeIn = 1f, float fadeOut = 1f, bool loop = true)
    {
        var clip = libraryProvider.GetClip(AudioType.Cutscene, key, index);
        if (clip == null) return;
        // 실행
        cutsceneGroup.PlayCutscene(clip, fadeIn, fadeOut, loop);
    }
    private void PlayAudio(VoiceKey key, int index = -1)
    {
        var clip = libraryProvider.GetClip(AudioType.Voice, key, index);
        if (clip == null) return;
        // 실행
        voiceGroup.PlayVoice(clip);
    }
    private void PlayAudio(UIKey key, int index = -1)
    {
        var clip = libraryProvider.GetClip(AudioType.UI, key, index);
        if (clip == null) return;
        // 실행
        uiGroup.PlayVoice(clip);
    }
    #endregion

    // 오디오 컨트롤 // 중요한건 실행되고 있는 오디오를 정지시킬 수 있는 로직이어야함
    // 배경음과 컷신은 해당 오브젝트에서 재생되는 것이니 그 오브젝트를 정지시키면 되지않을까?


    // 볼륨
    public void SetVolume(string channel, float linear)
    {
        volumeManager.SetVolume(channel, linear);
    }

    public float GetVolume(string channel)
    {
        return volumeManager.GetVolume(channel);
    }

}

