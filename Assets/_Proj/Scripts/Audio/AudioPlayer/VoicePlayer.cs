using UnityEngine;
using UnityEngine.Audio;

public class VoicePlayer
{
    private readonly AudioMixer mixer;
    private readonly Transform myTrans;
    private AudioSource currentSource;

    public VoicePlayer(AudioMixer mixer, Transform myTrans)
    {
        this.mixer = mixer;
        this.myTrans = myTrans;
    }

    public void PlayAudio(AudioClip clip, AudioMixerGroup group)
    {
        if (currentSource == null)
        {
            GameObject gObj = new GameObject($"Voice_{clip.name}");
            gObj.transform.parent = myTrans;
            currentSource = gObj.AddComponent<AudioSource>();
            currentSource.outputAudioMixerGroup = group;
            //Object.DontDestroyOnLoad(gObj);
        }

        //if (currentSource.isPlaying && currentSource.clip == clip) return;

        currentSource.clip = clip;
        currentSource.Play();
    }
}
