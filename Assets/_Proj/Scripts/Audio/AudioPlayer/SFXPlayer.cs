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
        }
        currentSource.outputAudioMixerGroup = group;
        currentSource.clip = clip;
        currentSource.loop = loop;

        //  3D 옵션
        if (pos.HasValue)
        {
            currentSource.transform.position = pos.Value;
            currentSource.spatialBlend = 1f;

            #region 사용자지정버전
            //src.rolloffMode = AudioRolloffMode.Custom;
            //AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.9f, 0.5f), new Keyframe(0.3f, 0.3f), new Keyframe(1f, 0f));
            //src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
            //src.minDistance = 0.3f;  // float �� �̳��� �׻� �ִ� ����
            //src.maxDistance = 5f; // float �� �̻��� �� �鸲
            #endregion

            #region 일반버전
            currentSource.rolloffMode = AudioRolloffMode.Logarithmic; // �ڿ������� ����
            currentSource.minDistance = 1f;  // float �� �̳��� �׻� �ִ� ����
            currentSource.maxDistance = 50f; // float �� �̻��� �� �鸲
            #endregion
        }
        else currentSource.spatialBlend = 0f;

        currentSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
        currentSource.volume = UnityEngine.Random.Range(0.95f, 1f);

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
    }
    public override void SetVolumeNormal()
    {
        base.SetVolumeNormal();
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
