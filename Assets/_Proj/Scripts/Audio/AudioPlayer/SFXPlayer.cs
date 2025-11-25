using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

public class SFXPlayer : AudioPlayerControl
{
    private readonly AudioMixer mixer;
    private readonly Transform myTrans;
    private readonly AudioPool audioPool;
    private MonoBehaviour coroutineHost;
    private AudioSource currentSource;

    public SFXPlayer(AudioMixer mixer, Transform myTrans, AudioPool pool)
    {
        this.mixer = mixer;
        this.myTrans = myTrans;
        audioPool = pool;
        coroutineHost = myTrans.GetComponent<MonoBehaviour>();
    }

    public void PlayAudio(AudioClip clip, AudioMixerGroup group, bool loop, bool pooled, Vector3? pos = null)
    {
        if (pooled)
        {
            currentSource = audioPool.GetSource();
        }
        else
        {
            GameObject gObj = new GameObject($"SFXPlay");
            gObj.transform.parent = myTrans;
            currentSource = gObj.AddComponent<AudioSource>();
            activeSources.Add(currentSource);
            currentSource.volume = 1f;
            currentSource.pitch = 1f;
            currentSource.rolloffMode = AudioRolloffMode.Custom;
            AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(50f, 0.877f), new Keyframe(60f, 0.59f), new Keyframe(80f, 0.34f), new Keyframe(128f, 0.125f), new Keyframe(200f, 0.002f));
            currentSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
            currentSource.minDistance = 40f;
            currentSource.maxDistance = 200f;
        }
        currentSource.outputAudioMixerGroup = group;
        currentSource.clip = clip;
        currentSource.loop = loop;

        //  3D 옵션
        if (pos.HasValue)
        {
            currentSource.transform.position = pos.Value;
            currentSource.spatialBlend = 1f;
        }
        else currentSource.spatialBlend = 0f;

        // currentSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
        // currentSource.volume = UnityEngine.Random.Range(0.95f, 1f);

        // 풀링이라면
        if (pooled)
        {
            currentSource.Play();
            coroutineHost.StartCoroutine(audioPool.ReturnAfterDelay(currentSource, clip.length));
        }
        if (!pooled)
        {
            //play
            currentSource.Play();
            // loop�϶� ���� �� ���°� �߰� �ؾ���
            if (loop) { }
            else NewDestroy(currentSource.gameObject, clip.length);
        }
    }

    public override void PlayAll()
    {
        base.PlayAll();
        audioPool.PlayPool();
    }
    public override void PauseAll()
    {
        base.PauseAll();
        audioPool.PausePool();
    }
    public override void ResumeAll()
    {
        base.ResumeAll();
        audioPool.ResumePool();
    }
    public override void ResetAll()
    {
        base.ResetAll();
        audioPool.ResetPool();
    }
    public override void StopAll()
    {
        base.StopAll();
        audioPool.StopPool();
    }
    public override void SetVolumeHalf()
    {
        base.SetVolumeHalf();
        audioPool.SetPoolVolumeHalf();
    }
    public override void SetVolumeNormal()
    {
        base.SetVolumeNormal();
        audioPool.SetPoolVolumeNormal();
    }
    public override void SetVolumeZero()
    {
        base.SetVolumeZero();
        audioPool.SetPoolVolumeZero();
    }

    private void NewDestroy(GameObject gObj, float length)
    {
        AudioSource aS = gObj.GetComponent<AudioSource>();
        UnityEngine.Object.Destroy(gObj, length);
        if (gObj.IsDestroyed())
        {
            activeSources.Remove(aS);
        }
    }
    // PlayOneShot()
    // �� : (�ܹ߼� ȿ������ �ſ� ����) (������ AudioSource�� ���� �� ���� �ʿ� ����) (Ǯ�� ���� ����ȭ ����)
    // �� : (�޸𸮿� ����� ä���� �� ���� ���) (loop �Ұ�) (Stop()���� �ߴ� �Ұ��� : ������ ���)
    // ���� �� �����Ǵ� ���� �������.
    // �ʱ�ȭ �� ������ȯ�� ��������

}
