using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

public class UIPlayer : AudioPlayerControl
{
    private readonly AudioMixer mixer;
    private readonly Transform myTrans;
    private AudioSource currentSource;

    public UIPlayer(AudioMixer mixer, Transform myTrans, AudioMixerGroup group)
    {
        this.mixer = mixer;
        this.myTrans = myTrans;
        GameObject gObj = new GameObject($"UIPlayer");
        gObj.transform.parent = myTrans;
        currentSource = gObj.AddComponent<AudioSource>();
        activeSources.Add(currentSource);
        currentSource.outputAudioMixerGroup = group;
        currentSource.loop = false;
        currentSource.volume = 0.34f;
        initVolume = currentSource.volume;
    }

    public void PlayAudio(AudioClip clip)
    {
        //if (currentSource.isPlaying && currentSource.clip == clip) return;
        currentSource.PlayOneShot(clip);
    }

    public override void PlayAll() { }
    public override void PauseAll() { }
    public override void ResumeAll() { }
    public override void StopAll() { }
    public override void ResetAll(float volumeValue)
    {
        foreach (var src in activeSources)
        {
            src.volume = volumeValue;
        }
    }
    public override void SetVolumeHalf() { }
    public override void SetVolumeNormal() { }
    public override void SetVolumeZero() { }
}
