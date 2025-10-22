using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

public class BGMPlayer
{
    private readonly AudioMixer mixer;
    private readonly Transform myTrans;
    private AudioSource currentSource;

    public BGMPlayer(AudioMixer mixer, Transform myTrans)
    {
        this.mixer = mixer;
        this.myTrans = myTrans;
    }

    public void PlayAudio(AudioClip clip, AudioMixerGroup group, float fadeIn, float fadeOut, bool loop)
    {
        if (currentSource == null)
        {
            GameObject gObj = new GameObject($"BGM_{clip.name}");
            gObj.transform.parent = myTrans;
            currentSource = gObj.AddComponent<AudioSource>();
            currentSource.outputAudioMixerGroup = group;
            //Object.DontDestroyOnLoad(gObj);
        }

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
    // 각 오디오Player는 재생 담당인데 상태전환이 여기에 붙는게 맞을까? 오디오 팀장인 Group에서 해야하는거 아닌가?
    //public void Play()
    //{

    //}

    //public void Pause()
    //{
    //    if (currentSource == null) return;
    //    currentSource.Pause();
    //}

    //public void Resume()
    //{
    //    if (currentSource == null) return;
    //    currentSource.UnPause();
    //}
}

