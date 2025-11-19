using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
/// <summary>
/// BGM, Cutscene 합칠 것,
/// SFX, Ambient 합칠 것
/// 하지만 재생은 나눌 생각
/// SFX와 Ambient에서는 클립이 없더라도(또는 !isPlaying) 소스 볼륨을 조절해야함.
/// SFX, Ambient 랜덤 volume, 
/// 
/// 1119.
/// 1. 축소 전 기존 오디오 시스템들 백업.
/// 2. 오디오 시스템 칼질 => 모든 Voice영역 삭제 - DialogueGroup에 DialogueSFX로 대체
/// 3. Cutscene은 영상 재생이니 해당 영역 오디오소스 만 만들고 나머지 다 삭제.
/// 4. 오디오믹서 그룹도 3개로 축소 => Master, BGM, SFX 근데 일단 다른 것들 삭제는 안함
/// </summary>
[Serializable]
public struct AudioGroupMapping
{
    public AudioType type;
    public AudioMixerGroup group;
}
//[DefaultExecutionOrder(-99)]
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
    [SerializeField] private UILibrary uiLibrary;

    [Header("AudioGroupChildren")]
    private BGMGroup bgmGroup;
    private SFXGroup sfxGroup;
    private AmbientGroup ambientGroup;
    private CutsceneGroup cutsceneGroup;
    private UIGroup uiGroup;
    private DialogueGroup diaGroup;

    private Dictionary<AudioType, AudioMixerGroup> groupMap;
    private AudioLibraryProvider libraryProvider;
    private AudioVolumeHandler volumeHandler;
    private IAudioController[] audioGroups;
    private SceneAudio sAudio;

    public AudioClip BGMClip { get; private set;}

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 인스펙터에서 설정한 타입별 오디오믹서 그룹을 실질적으로 해당 그룹에 연결.
        groupMap = new Dictionary<AudioType, AudioMixerGroup>();
        foreach (var map in groupMappings)
        {
            groupMap[map.type] = map.group;
        }

        AudioGroupProvider = this;

        libraryProvider = new AudioLibraryProvider(bgmLibrary, sfxLibrary, ambientLibrary, uiLibrary);
        volumeHandler = new AudioVolumeHandler(mixer);

        // AudioGroupMapping
        bgmGroup = GetComponentInChildren<BGMGroup>();
        sfxGroup = GetComponentInChildren<SFXGroup>();
        ambientGroup = GetComponentInChildren<AmbientGroup>();
        cutsceneGroup = GetComponentInChildren<CutsceneGroup>();
        uiGroup = GetComponentInChildren<UIGroup>();
        diaGroup = GetComponentInChildren<DialogueGroup>();

        audioGroups = new IAudioController[6] { bgmGroup, sfxGroup, ambientGroup, cutsceneGroup, uiGroup, diaGroup };
        // 각 오디오 그룹 초기화 작업
        foreach (var aG in audioGroups)
        {
            aG.Init();
        }
    }

    private void OnEnable()
    {
        if (SettingManager.Instance == null)
        {
            var settingGO = new GameObject("SettingManager");
            settingGO.AddComponent<SettingManager>();
        }
        var volumeData = SettingManager.Instance.settingData;
        volumeHandler.ApplyVolumes(volumeData.audio);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 믹서 가져오기
    public AudioMixer GetMixer()
    {
        return mixer;
    }
    // 맵핑한 그룹 가져오기
    public AudioMixerGroup GetGroup(AudioType type)
    {
        return groupMap[type];
    }
    // VideoPlayer에서 CutsceneGroup 영역 오디오 소스 가져오기
    public AudioSource GetAudioSourceForVideoPlayer()
    {
        return cutsceneGroup.GetCutsceneSource();
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
            case UIKey uk:
                PlayAudio(uk, index);
                break;
            default: throw new Exception("Key: BGMKey, SFXKey, AmbientKey, CutsceneKey, VoiceKey, UIKey");
        }
    }

    public void PlayDialogueAudio(AudioType type, string audioFileName)
    {
        diaGroup.PlayDialogue(type, audioFileName);
    }

    public void PlayBGMForResources(AudioClip clip, float fadeIn, float fadeOut, bool loop)
    {
        bgmGroup.PlayBGMForResources(clip, fadeIn, fadeOut, loop);
    }

    // 각 씬에 있는 메인 BGM을 이벤트로 재생
    private void OnSceneLoaded(Scene scne, LoadSceneMode mode)
    {
        sAudio = FindFirstObjectByType<SceneAudio>();
        if (sAudio != null)
        {
            sAudio.StartBGM();
        }
    }

    #region 오디오 재생 분기
    private void PlayAudio(BGMKey key, int index = -1, float fadeIn = 1f, float fadeOut = 1f, bool loop = true)
    {
        var clip = libraryProvider.GetClip(AudioType.BGM, key, index);
        if (clip == null) return;
        BGMClip = clip;
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
    private void PlayAudio(UIKey key, int index = -1)
    {
        var clip = libraryProvider.GetClip(AudioType.UI, key, index);
        if (clip == null) return;
        // ����
        uiGroup.PlayVoice(clip);
    }
    #endregion

    public AudioClip GetBGMClip()
    {
        return BGMClip;
    }

    // 오디오 그룹 제어
    public void PlayAllAudioGroup()
    {
        foreach (var aG in audioGroups)
        {
            aG.PlayPlayer();
        }
    }
    public void PauseAllAudioGroup()
    {
        foreach (var aG in audioGroups)
        {
            aG.PausePlayer();
        }
    }
    public void ResumeAllAudioGroup()
    {
        foreach (var aG in audioGroups)
        {
            aG.ResumePlayer();
        }
    }
    public void StopAllAudioGroup()
    {
        foreach (var aG in audioGroups)
        {
            aG.StopPlayer();
        }
    }
    public void ResetAllAudioGroup()
    {
        foreach (var aG in audioGroups)
        {
            aG.ResetPlayer();
        }
    }
    public void EnterDialogue()
    {
        foreach (var aG in audioGroups)
        {
            if (aG is DialogueGroup == false) aG.SetVolumeHalf();
            else aG.ResetPlayer();
        }
    }
    public void ExitDialogue()
    {
        foreach (var aG in audioGroups)
        {
            if (aG is DialogueGroup == false) aG.SetVolumeNormal();
            else aG.ResetPlayer();
        }
    }
    public void EnterCutscene()
    {
        foreach (var aG in audioGroups)
        {
            if (aG is CutsceneGroup == false) aG.PausePlayer();
        }
    }
    public void ExitCutscene()
    {
        foreach (var aG in audioGroups)
        {
            if (aG is CutsceneGroup == true)
            {
                aG.PausePlayer();
                aG.ResetPlayer();
            }
            else
            {
                aG.ResumePlayer();
            }
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

