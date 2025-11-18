using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

public class BGMPlayer : AudioPlayerControl
{
    private readonly AudioMixer mixer;
    private readonly Transform myTrans;
    public AudioSource currentSource;

    public BGMPlayer(AudioMixer mixer, Transform myTrans)
    {
        this.mixer = mixer;
        this.myTrans = myTrans;
    }

    public void PlayAudio(AudioClip clip, AudioMixerGroup group, float fadeIn, float fadeOut, bool loop)
    {
        if (currentSource == null)
        {
            GameObject gObj = new GameObject($"BGMPlayer");
            gObj.transform.parent = myTrans;
            currentSource = gObj.AddComponent<AudioSource>();
            activeSources.Add(currentSource);
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
    public override void ResetAll()
    {
        base.ResetAll();
    }



    // �� �����Player�� ��� ����ε� ������ȯ�� ���⿡ �ٴ°� ������? ����� ������ Group���� �ؾ��ϴ°� �ƴѰ�? ���� ����� ������ Group���� ����
}

