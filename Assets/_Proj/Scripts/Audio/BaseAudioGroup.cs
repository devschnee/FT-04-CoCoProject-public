using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public abstract class BaseAudioGroup : MonoBehaviour, IAudioController
{
    protected AudioMixer mixer;
    protected AudioMixerGroup group;
    
    public virtual void Init()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
    }
    public abstract void PostInit();
    public abstract void PlayPlayer();
    public abstract void PausePlayer();
    public abstract void ResumePlayer();
    public abstract void StopPlayer();
    public abstract void ResetPlayer();
    public abstract void SetVolumeHalf();
    public abstract void SetVolumeNormal();
    public abstract void SetVolumeZero();
}
