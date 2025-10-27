using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

//[Serializable]
//public struct OldAudioGroupMapping
//{
//    public AudioType type;
//    public AudioMixerGroup group;
//}

//public class OldAudioManager2 : MonoBehaviour
//{
//    public static OldAudioManager2 Instance { get; private set; }

//    [Header("Mixer & Group Settings")]
//    [SerializeField] private AudioMixer mixer;
//    [SerializeField] private OldAudioGroupMapping[] groupMappings;

//    [Header("Audio Libraries")]
//    [SerializeField] private BGMLibrary bgmLibrary;
//    [SerializeField] private SFXLibrary sfxLibrary;
//    [SerializeField] private AmbientLibrary ambientLibrary;
//    [SerializeField] private CutsceneLibrary cutsceneLibrary;
//    [SerializeField] private VoiceLibrary voiceLibrary;

//    [Header("Pooling Settings")]
//    [SerializeField] private int poolSize = 30;

//    private Dictionary<AudioType, AudioMixerGroup> groupMap;
//    //private Dictionary<AudioType, List<AudioSource>> activeSources;
//    private AudioLibraryProvider libraryProvider;
//    private AudioPool audioPool;
//    private BGMPlayer bgmPlayer;
//    private CutscenePlayer cutscenePlayer;
//    private OptionVolumeManager volumeManager;

//    void Awake()
//    {
//        if (Instance != null && Instance != this)
//        {
//            Destroy(gameObject);
//            return;
//        }
//        Instance = this;
//        DontDestroyOnLoad(gameObject);

//        groupMap = new Dictionary<AudioType, AudioMixerGroup>();
//        foreach (var map in groupMappings)
//        {
//            groupMap[map.type] = map.group;
//        }

//        libraryProvider = new AudioLibraryProvider(bgmLibrary, sfxLibrary, ambientLibrary, cutsceneLibrary, voiceLibrary);
//        audioPool = new AudioPool(transform, groupMap[AudioType.SFX], poolSize);
//        bgmPlayer = new BGMPlayer(mixer, transform);
//        cutscenePlayer = new CutscenePlayer(mixer);
//        cutscenePlayer.parentPos = transform;
//        volumeManager = new OptionVolumeManager(mixer);
//    }

//    // BGM, Cutscene 재생
//    public void PlayBGM<T>(AudioType type, T key, int index = -1, float fadeIn = 1f, float fadeOut = 1f, bool loop = true) where T : Enum
//    {
//        if (type == AudioType.SFX || type == AudioType.Voice || type == AudioType.Ambient) return;

//        var clip = libraryProvider.GetClip(type, key, index);
//        if (clip == null) return;

//        if (type == AudioType.BGM) bgmPlayer.Play(clip, type, groupMap[type], fadeIn, fadeOut, loop);

//        if (type == AudioType.Cutscene) cutscenePlayer.Play(clip, type, groupMap[type], fadeIn, fadeOut, loop);
//    }

//    // SFX, Voice, Ambient 재생
//    public void PlaySFX<T>(AudioType type, T key, int index = -1, bool loop = false, bool pooled = false, Vector3? pos = null) where T : Enum
//    {
//        if (type == AudioType.BGM || type == AudioType.Cutscene) return;

//        var clip = libraryProvider.GetClip(type, key, index);
//        if (clip == null) return;

//        var src = pooled ? audioPool.GetSource() : new GameObject($"SFX_{clip.name}").AddComponent<AudioSource>();
//        src.outputAudioMixerGroup = groupMap[type];

//        // 위치가 있다면 3D 사운드로
//        if (pos.HasValue)
//        {
//            src.transform.position = pos.Value;
//            src.spatialBlend = 1f;
//            //커스텀
//            //src.rolloffMode = AudioRolloffMode.Custom;
//            //AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.9f, 0.5f), new Keyframe(0.3f, 0.3f), new Keyframe(1f, 0f));
//            //src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
//            //src.minDistance = 0.3f;  // float 값 이내는 항상 최대 볼륨
//            //src.maxDistance = 5f; // float 값 이상은 안 들림
//            // 보통
//            src.rolloffMode = AudioRolloffMode.Logarithmic; // 자연스럽게 감소
//            src.minDistance = 1f;  // float 값 이내는 항상 최대 볼륨
//            src.maxDistance = 50f; // float 값 이상은 안 들림
//        }
//        else src.spatialBlend = 0f;

//        src.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
//        src.volume = UnityEngine.Random.Range(0.95f, 1f);

//        //RegisterSource(type, src);

//        // 재생 및 삭제
//        if (pooled)
//        {
//            if (loop == true)
//            {
//                src.loop = true;
//                src.clip = clip;
//                src.Play();
//                // loop일때 정지 및 빼는거 추가 해야함
//            }
//            else
//            {
//                src.PlayOneShot(clip);
//                StartCoroutine(audioPool.ReturnAfterDelay(src, clip.length));
//            }
//        }
//        if (!pooled)
//        {
//            if (!loop) Destroy(src.gameObject, clip.length);
//            // loop일때 정지 및 빼는거 추가 해야함
//        }

//    }

//    // 오디오 컨트롤 // 중요한건 실행되고 있는 오디오를 정지시킬 수 있는 로직이어야함
//    // 배경음과 컷신은 해당 오브젝트에서 재생되는 것이니 그 오브젝트를 정지시키면 되지않을까?
//    public void PauseBGM()
//    {
//        bgmPlayer.Pause();
//    }
//    public void ResumeBGM()
//    {
//        bgmPlayer.Resume();
//    }

//    public void PauseCutscene()
//    {
//        cutscenePlayer.Pause();
//    }
//    public void ResumeCutscene()
//    {
//        cutscenePlayer.Resume();
//    }

//    // PlaySFX으로 재생된 클립들 오디오 타입 저장
//    //private void RegisterSource(AudioType type, AudioSource src)
//    //{
//    //    if (!activeSources.ContainsKey(type))
//    //    {
//    //        activeSources[type] = new List<AudioSource>();
//    //        activeSources[type].Add(src);
//    //    }
//    //}
//    //private void UnregisterSource(AudioType type, AudioSource src)
//    //{
//    //    if (activeSources.ContainsKey(type))
//    //        activeSources[type].Remove(src);
//    //}

//    // 볼륨
//    public void SetVolume(string channel, float linear)
//    {
//        volumeManager.SetVolume(channel, linear);
//    }

//    public float GetVolume(string channel)
//    {
//        return volumeManager.GetVolume(channel);
//    }
//}
