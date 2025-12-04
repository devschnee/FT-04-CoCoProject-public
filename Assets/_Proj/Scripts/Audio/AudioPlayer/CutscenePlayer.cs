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
            src.volume = initVolume;
        }
    }
    public override void PauseAll()
    {
    }
    public override void ResumeAll()
    {
    }
    public override void StopAll()
    {
        foreach (var src in activeSources)
        {
            src.volume = 0f;
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
                src.volume = 0f;
                src.pitch = 1f;
                src.clip = null;
            }
        }
    }
    public override void SetVolumeHalf()
    {
    }
    public override void SetVolumeNormal()
    {
    }

    public override void SetVolumeZero()
    {
    }
}
