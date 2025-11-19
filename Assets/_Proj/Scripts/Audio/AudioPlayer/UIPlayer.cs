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
        currentSource.volume = 1;
    }

    public void PlayAudio(AudioClip clip)
    {
        //if (currentSource.isPlaying && currentSource.clip == clip) return;
        currentSource.PlayOneShot(clip);
    }
}
