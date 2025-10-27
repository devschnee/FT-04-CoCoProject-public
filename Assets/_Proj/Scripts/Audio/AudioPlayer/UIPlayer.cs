using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

public class UIPlayer : AudioPlayerControl
{
    private readonly AudioMixer mixer;
    private readonly Transform myTrans;
    private AudioSource currentSource;

    public UIPlayer(AudioMixer mixer, Transform myTrans)
    {
        this.mixer = mixer;
        this.myTrans = myTrans;
    }

    public void PlayAudio(AudioClip clip, AudioMixerGroup group)
    {
        if (currentSource == null)
        {
            GameObject gObj = new GameObject($"UIPlayer");
            gObj.transform.parent = myTrans;
            currentSource = gObj.AddComponent<AudioSource>();
            activeSources.Add(currentSource);
            currentSource.outputAudioMixerGroup = group;
            //Object.DontDestroyOnLoad(gObj);
        }

        //if (currentSource.isPlaying && currentSource.clip == clip) return;
        currentSource.PlayOneShot(clip);
    }
}
