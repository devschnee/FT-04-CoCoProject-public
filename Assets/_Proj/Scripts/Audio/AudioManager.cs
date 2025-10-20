using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System;
using DG.Tweening;


/// <summary>
/// AudioManager.cs : 음악을 재생하는 실질적인 스크립트
/// SceneAudio : 씬의 배경음, 인트로가 있으면 인트로 bool 체크 후 인트로 음악클립도 넣어야함
/// AudioEvents : AudioEventListener에서 효과음 재생을 이벤트로 틀어주는 구조여서 다리역할을 하는 정적 이벤트
/// AudioEventListener : 효과음이 AudioManager로 들어가 재생되기 위한 이벤트 틀
/// AudioEnumKey : Enum으로 ...Library에서 직렬화로 효과음 종류와 각 종류 별 클립을 넣기 위한 효과음 종류
/// ...Library : 각 카테고리별 오디오 모음집
/// 
/// *중요 : 위 구조는 학원에서 프로그래밍반(나 포함6명) 사람들과 폴가이즈를 레퍼런스로 한 게임을 제작할때 만든 구조임. 그치만 이제 새로 만들 게임은 기획반 학생들(4명)과 함께 기업에서 미션을 줘서 기획쪽이 기획한 게임을 만드는 프로젝트임. 게임은 안드로이드 모바일. 소코반을 기반한 스토리 게임이며, 주인공인 안드로이드가 건망증으로 본인의 강아지(코코두기)를 기억을 못함. 코코두기는 각 소코반 스테이지를 클리어하면서 주인의 기억칩을 찾는 게임. 스테이지 클리어 시 메인 스토리 컷신이 나오고, 소코반 스테이지 내에 특정 오브젝트가 있어서 이걸 획득하면 주인과의 서브 스토리(추억) 컷신이 나옴.
/// 
/// 리펙토링 할 사항 : 위 구조니 기존에는 AudioMixer에 BGM, SFX만 있었고 각 Volume 부분을 expose해서 2개의 볼륨 파라미터만 만들었다면, 이번에는 Master, BGM, SFX, Ambient, Cutscene을 사용하고 각 Volume 부분을 expose해서 5개의 파라미터를 가지로 볼륨 조절을 할 수 있게 만들 생각. 그래서 이번 프로젝트는 빌드해서 기업쪽에게 넘기는 방식이니 옵션매니저를 따로 만들고 기존에는 AudioManager에서 각 항목 볼륨을 저장했다면 단일책임원칙을 준수하기 위해 볼륨 저장은 옵션쪽으로 보낼 생각이고, PlayerPrefs로 저장하던 방식을 이젠 옵션 내의 여러 옵션들이 있으니 여러 옵션들과 함께 json으로 저장할 생각 추가적으로 좀더 완성도 있는(보안, 로직, 안 써봤던거 연습) 오디오를 원해서 만들면서 리펙토링 할 생각
/// 
/// PlayBGM : 배경음, 컷신음 재생 메서드
/// PlayOneShotSFX : 저빈도 SFX (UI 효과음, 카운트다운, 특수 이벤트, 게임 시작/종료, 라운드 승리/탈락)
/// PlayPooledSFX : 고빈도 SFX (캐릭터 각종 효과음, 충돌 효과음, 아이템?(아이템 갯수에 따라 저빈도 고빈도 갈듯))
/// 셋팅 
/// 
/// </summary>
/// 

[Serializable]
public struct AudioGroupMapping
{
    public AudioType type;
    public AudioMixerGroup group;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("AudioSource")]
    [SerializeField] AudioSource audioManagerSource; // 이 오브젝트에 붙어 있는 AudioSource
    private AudioSource cutsceneSource;
    private AudioSource _audioSource;

    [Header("AudioMixer & Parameter")]
    [SerializeField] AudioMixer mixer;
    private string MasterVolume = "MasterVolumeParam";
    private string BGMVolume = "BGMVolumeParam";
    private string SFXVolume = "SFXVolumeParam";
    private string AmbientVolume = "AmbientVolumeParam";
    private string CutsceneVolume = "CutsceneVolumeParam";
    private string VoiceVolume = "VoiceVolumeParam";

    [Header("AudioGroup")]
    [SerializeField] AudioGroupMapping[] audioGroupMappings;
    [SerializeField] AudioMixerGroup sfxGroup; // SFX 그룹
    [SerializeField] AudioMixerGroup ambientGroup; // Ambient 그룹
    [SerializeField] AudioMixerGroup cutsceneGroup; // Cutscene 그룹
    [SerializeField] AudioMixerGroup voiceGroup; // Voice 그룹

    [Header("AudioLibrary")]
    [SerializeField] BGMLibrary bgmLibrary;
    [SerializeField] SFXLibrary sfxLibrary;
    [SerializeField] AmbientLibrary ambientLibrary;
    [SerializeField] CutsceneLibrary cutsceneLibrary;
    [SerializeField] VoiceLibrary voiceLibrary;

    // 볼륨
    const string PREF_BGM_VOLUME = "BGMVolumeLinear"; // 0~1 저장
    const string PREF_SFX_VOLUME = "SFXVolumeLinear"; // 0~1 저장

    //private float currentBGMLinear = 0.8f;
    //private float currentSFXLinear = 0.8f;

    // SFX 오브젝트 풀링
    private int poolSize = 30;
    private Queue<AudioSource> pool = new Queue<AudioSource>();
    //
    private Dictionary<AudioType, AudioMixerGroup> audioGroupDic;

    void Awake()
    {
        if (Instance != null && Instance != this) 
        {
            Debug.LogWarning("중복 감지 제거됨");
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("AudioManager 생성");

        // SFX 오브젝트 풀링
        for (int i = 0; i < poolSize; i++)
        {
            var gObj = new GameObject("PooledAudioSource_" + i);
            gObj.transform.parent = transform;
            var src = gObj.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = sfxGroup;
            gObj.SetActive(false);
            pool.Enqueue(src);
        }
        //
        // 오디오 outputAudioMixerGroup 맵핑
        audioGroupDic = new Dictionary<AudioType, AudioMixerGroup>();
        foreach (var map in audioGroupMappings)
        {
            audioGroupDic[map.type] = map.group;
        }
        //
    }

    private void Start()
    {
        // 볼륨 로드 & 적용
        float savedBGM = PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 0.8f);
        float savedSFX = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 0.8f);
        //currentBGMLinear = PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 0.8f);
        //currentSFXLinear = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 0.8f);
        SetBGMVolumeLinear(savedBGM);
        SetSFXVolumeLinear(savedSFX);
    }

    #region 오디오 재생
    public void PlayAudio<T>(AudioType type, T key, int index = -1, bool pooled = false, Vector3? position = null) where T : Enum
    {
        if (type == AudioType.BGM || type == AudioType.Cutscene) return; 

        AudioClip clip = GetClip<T>(type, key, index);
        if (clip == null) return;

        AudioMixerGroup outputGroup = audioGroupDic[type];

        if (pooled == true)
        {
            PlayPooledSFXClip(clip, outputGroup, position);
        }
        else
        {
            PlayOneShotSFXClip(clip, outputGroup, position);
        }

    }
    public void PlayBGM<T>(AudioType type, T key, int index = -1, bool loop = false, float fadeIn = 0, float fadeOut = 0) where T : Enum
    {
        if (type == AudioType.SFX || type == AudioType.Ambient || type == AudioType.Voice) return;

        AudioClip clip = GetClip<T>(type, key, index);
        if (clip == null) return;

        AudioMixerGroup outputGroup = audioGroupDic[type];

        if (type == AudioType.BGM || type == AudioType.Cutscene)
        {
            PlayBGMClip(clip, outputGroup, loop, fadeIn, fadeOut);
        }

    }
    #endregion

    #region 배경음 + 컷씬음
    public void PlayBGMClip(AudioClip clip, AudioMixerGroup group, bool loop = true, float fadeIn = 0, float fadeOut = 0)
    {
        // 문제
        if (audioManagerSource.clip == clip && audioManagerSource.isPlaying) return;

        _audioSource = GetAudioSource(group);
        _audioSource.outputAudioMixerGroup = group;

        _audioSource.DOKill();

        // 현재 음악 페이드 아웃
        _audioSource.DOFade(0f, fadeOut).OnComplete(() =>
        {
            _audioSource.clip = clip;
            _audioSource.loop = loop;
            _audioSource.Play();

            // 새 음악 페이드 인
            _audioSource.DOFade(1f, fadeIn);
        });

    }
    #endregion

    #region SFX, Ambient, Voice
    private void PlayOneShotSFXClip(AudioClip clip, AudioMixerGroup group, Vector3? position = null)
    {
        GameObject gObj = new GameObject("SFX_" + clip.name);
    }

    private void PlayPooledSFXClip(AudioClip clip, AudioMixerGroup group, Vector3? position = null)
    {

    }
    #endregion

    #region 오디오클립분리
    private AudioClip GetClip<T>(AudioType type, T key, int index) where T : Enum
    {
        switch (type)
        {
            case AudioType.BGM:
                return bgmLibrary.GetClip((BGMKey)(object)key, index);
            case AudioType.SFX:
                return sfxLibrary.GetClip((SFXKey)(object)key, index);
            case AudioType.Ambient:
                return ambientLibrary.GetClip((AmbientKey)(object)key, index);
            case AudioType.Cutscene:
                return cutsceneLibrary.GetClip((CutsceneKey)(object)key, index);
            case AudioType.Voice:
                return voiceLibrary.GetClip((VoiceKey)(object)key, index);
            default:
                return null;
        }
    }
    #endregion

    #region 배경음, 컷신음 오디오소스 분리
    private AudioSource GetAudioSource(AudioMixerGroup group)
    {
        if (group == cutsceneGroup)
        {
            if (cutsceneSource == null)
            {
                GameObject gObj = new GameObject("CutsceneAudio");
                gObj.transform.parent = transform;
                cutsceneSource = gObj.AddComponent<AudioSource>();
                cutsceneSource.outputAudioMixerGroup = cutsceneGroup;
            }
            return cutsceneSource;
        }
        return audioManagerSource;
    }
    #endregion

    // 볼륨 값 저장은 나중에 json으로 저장할 예정 옵션매니저에서 관리하는 걸로
    #region 볼륨 값 저장 및 조절
    public void SetBGMVolumeLinear(float linear)
    {
        // 0 -> -80dB(거의 무음), 그 외는 로그 변환
        //currentBGMLinear = Mathf.Clamp01(linear);
        float dB = (linear <= 0.0001f) ? -80f : Mathf.Log10(linear) * 20f;
        mixer.SetFloat(BGMVolume, dB);
        PlayerPrefs.SetFloat(PREF_BGM_VOLUME, Mathf.Clamp01(linear));
        PlayerPrefs.Save();
    }

    public void SetSFXVolumeLinear(float linear)
    {
        //currentSFXLinear = Mathf.Clamp01(linear);
        float dB = (linear <= 0.0001f) ? -80f : Mathf.Log10(linear) * 20f;
        mixer.SetFloat(SFXVolume, dB);
        PlayerPrefs.SetFloat(PREF_SFX_VOLUME, Mathf.Clamp01(linear));
        PlayerPrefs.Save();
    }

    public float GetBGMVolumeLinear()
    {
        return PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 0.8f);
    }
    //public float GetBGMVolumeLinear() => currentBGMLinear;

    public float GetSFXVolumeLinear()
    {
        return PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 0.8f);
    }
    //public float GetSFXVolumeLinear() => currentSFXLinear;
    #endregion

}