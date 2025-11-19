using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

public class BGMPlayer : AudioPlayerControl
{
    private readonly AudioMixer mixer;
    private readonly Transform myTrans;
    public AudioSource currentSource;

    //private const string BGMFolderPath = "Sound/BGM/";

    public BGMPlayer(AudioMixer mixer, Transform myTrans, AudioMixerGroup group)
    {
        this.mixer = mixer;
        this.myTrans = myTrans;
        GameObject gObj = new GameObject($"BGMPlayer");
        gObj.transform.parent = myTrans;
        currentSource = gObj.AddComponent<AudioSource>();
        activeSources.Add(currentSource);
        currentSource.outputAudioMixerGroup = group;
        currentSource.volume = 1;
    }

    public void PlayAudio(AudioClip clip, float fadeIn, float fadeOut, bool loop)
    {
        // if (currentSource == null)
        // {
        //     GameObject gObj = new GameObject($"BGMPlayer");
        //     gObj.transform.parent = myTrans;
        //     currentSource = gObj.AddComponent<AudioSource>();
        //     activeSources.Add(currentSource);
        //     currentSource.outputAudioMixerGroup = group;
        //     //Object.DontDestroyOnLoad(gObj);
        // }

        if (currentSource.isPlaying && currentSource.clip == clip) return;

        currentSource.DOKill();
        currentSource.DOFade(0f, fadeOut).OnComplete(() =>
        {
            currentSource.clip = clip;
            currentSource.loop = loop;
            //currentSource.volume = 0f;
            currentSource.Play();
            currentSource.DOFade(1f, fadeIn);
        });
    }
    
    // public void PlayBGMForResources(string audioFileName, float fadeIn, float fadeOut, bool loop)
    // {
    //     if (!string.IsNullOrEmpty(audioFileName))
    //     {
    //         AudioClip bgmClip = Resources.Load<AudioClip>(audioFileName);
    //         if (currentSource.clip == bgmClip && currentSource.isPlaying) return;
    //         if (bgmClip != null)
    //         {
    //             currentSource.DOKill();
    //             currentSource.DOFade(0f, fadeOut).OnComplete(() =>
    //             {
    //                 currentSource.clip = bgmClip;
    //                 currentSource.loop = loop;
    //                 //currentSource.volume = 0f;
    //                 currentSource.Play();
    //                 currentSource.DOFade(1f, fadeIn);
    //             });
    //         }
    //     }
    // }

    public void PlayBGMForResources(AudioClip clip, float fadeIn, float fadeOut, bool loop)
    {
        if (currentSource.clip == clip && currentSource.isPlaying)
        {
            Debug.Log($"BGMPlayer : 리턴시킨다");
            return;
        }
        if (clip != null)
        {
            currentSource.DOKill();
            currentSource.DOFade(0f, fadeOut).OnComplete(() =>
            {
                currentSource.clip = clip;
                currentSource.loop = loop;
                //currentSource.volume = 0f;
                currentSource.Play();
                currentSource.DOFade(1f, fadeIn);
            });
        }
        else
        {
            Debug.Log($"BGMPlayer : AudioClip 없음 {clip.name}");
        }
    }

    public override void PlayAll()
    {
        base.PlayAll();
    }
    public override void PauseAll()
    {
        base.PauseAll();
    }
    public override void ResumeAll()
    {
        base.ResumeAll();
    }
    public override void StopAll()
    {
        base.StopAll();
    }
    public override void ResetAll()
    {
        base.ResetAll();
    }
    public override void SetVolumeHalf()
    {
        base.SetVolumeHalf();
    }
    public override void SetVolumeNormal()
    {
        base.SetVolumeNormal();
    }
}

