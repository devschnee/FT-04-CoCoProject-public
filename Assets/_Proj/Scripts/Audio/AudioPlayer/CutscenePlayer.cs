using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

public class CutscenePlayer : AudioPlayerControl
{
    private readonly AudioMixer mixer;
    private readonly Transform myTrans;
    private AudioSource currentSource;

    public CutscenePlayer(AudioMixer mixer, Transform myTrans, AudioMixerGroup group)
    {
        this.mixer = mixer;
        this.myTrans = myTrans;
        GameObject gObj = new GameObject($"CutscenePlayer");
        gObj.transform.parent = myTrans;
        currentSource = gObj.AddComponent<AudioSource>();
        activeSources.Add(currentSource);
        currentSource.outputAudioMixerGroup = group;
        currentSource.volume = 0.7f;
        initVolume = currentSource.volume;
    }

    public AudioSource GetCutsceneAS()
    {
        return currentSource;
    }

    public override void PlayAll()
    {
        foreach (var src in activeSources)
        {
            if (src != null)
            {
                if (DOTween.IsTweening(src, true)) src.DOKill();
                if (src.volume != 1)
                {
                    src.DOFade(initVolume, 0.5f);
                }
                else { }
            }
        }
    }
    public override void PauseAll()
    {
        foreach (var src in activeSources)
        {
            if (src != null)
            {
                if (DOTween.IsTweening(src, true)) src.DOKill();
                src.DOFade(0, 0.5f);
            }
        }
    }
    public override void ResumeAll()
    {
        foreach (var src in activeSources)
        {
            if (src != null)
            {
                if (DOTween.IsTweening(src, true)) src.DOKill();
                src.DOFade(initVolume, 0.5f);
            }
        }
    }
    public override void StopAll()
    {
        foreach (var src in activeSources)
        {
            if (src != null) 
            {
                if (DOTween.IsTweening(src, true)) src.DOKill();
                src.DOFade(0, 0.5f);
            } 
        }
    }
    public override void ResetAll(float volumeValue)
    {
        foreach (var src in activeSources)
        {
            if (src != null)
            {
                if (DOTween.IsTweening(src, true)) src.DOKill();
                src.loop = false;
                src.volume = volumeValue;
                src.pitch = 1f;
                src.clip = null;
            }
        }
    }
    public override void SetVolumeHalf()
    {
        foreach (var src in activeSources)
        {
            if (DOTween.IsTweening(src, true)) src.DOKill();
            src.DOFade(0.5f, 0.5f);
        }
    }
    public override void SetVolumeNormal()
    {
        foreach (var src in activeSources)
        {
            if (DOTween.IsTweening(src, true)) src.DOKill();
            src.DOFade(initVolume, 0.5f);
        }
    }

    public override void SetVolumeZero()
    {
        base.SetVolumeZero();
    }
}
