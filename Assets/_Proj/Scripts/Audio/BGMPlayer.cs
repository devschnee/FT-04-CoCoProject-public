using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

public class BGMPlayer
{
    private readonly AudioMixer mixer;
    private AudioSource currentSource;
    public Transform parentPos;

    public BGMPlayer(AudioMixer mixer)
    {
        this.mixer = mixer;
    }

    public void Play(AudioClip clip, AudioType type, AudioMixerGroup group, float fadeIn, float fadeOut, bool loop)
    {
        if (!(type == AudioType.BGM)) return;
        if (currentSource == null)
        {
            GameObject gObj = new GameObject("BGMPlayer");
            gObj.transform.parent = parentPos;
            currentSource = gObj.AddComponent<AudioSource>();
            currentSource.outputAudioMixerGroup = group;
            Object.DontDestroyOnLoad(gObj);
        }

        if (currentSource.isPlaying && currentSource.clip == clip) return;

        currentSource.DOKill();
        currentSource.DOFade(0f, fadeOut).OnComplete(() =>
        {
            currentSource.clip = clip;
            currentSource.loop = loop;
            currentSource.volume = 0f;
            currentSource.Play();
            currentSource.DOFade(1f, fadeIn);
        });
    }

    public void Pause()
    {
        if (currentSource == null) return;
        currentSource.Pause();
    }

    public void Resume()
    {
        if (currentSource == null) return;
        currentSource.UnPause();
    }
}

