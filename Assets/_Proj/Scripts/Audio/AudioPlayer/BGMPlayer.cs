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
        currentSource.volume = 0.8f;
        initVolume = currentSource.volume;
    }

    public void PlayAudio(AudioClip clip, float fadeIn, float fadeOut, bool loop, bool forcePlay = false)
    {

        if (!forcePlay && currentSource.isPlaying && currentSource.clip == clip) return;

        currentSource.DOKill();
        currentSource.DOFade(0f, fadeOut).OnComplete(() =>
        {
            Debug.Log($"BGMPlayer : DoTween fadeOut 끝 재생 시작");
            currentSource.clip = clip;
            currentSource.loop = loop;
            //currentSource.volume = 0f;
            currentSource.Play();
            currentSource.DOFade(initVolume, fadeIn);
        });
    }

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
                currentSource.DOFade(initVolume, fadeIn);
            });
        }
        else
        {
            Debug.Log($"BGMPlayer : AudioClip 없음");
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
    public override void ResetAll(float volumeValue)
    {
        base.ResetAll(volumeValue);
    }
    public override void SetVolumeHalf()
    {
        base.SetVolumeHalf();
    }
    public override void SetVolumeNormal()
    {
        base.SetVolumeNormal();
    }
    public override void SetVolumeZero()
    {
        base.SetVolumeZero();
    }
}

