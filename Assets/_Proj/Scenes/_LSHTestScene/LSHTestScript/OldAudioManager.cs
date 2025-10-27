

/*

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

    private BGMPlayer BGMPlayer;

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

        BGMPlayer.parentPos = transform;

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
*/
