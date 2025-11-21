using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

public class AmbientPlayer : AudioPlayerControl
{
    private readonly AudioMixer mixer;
    private readonly Transform myTrans;
    private readonly AudioPool audioPool;
    private MonoBehaviour coroutineHost;
    public AudioSource currentSource;

    public AmbientPlayer(AudioMixer mixer, Transform myTrans, AudioPool pool)
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
            GameObject gObj = new GameObject($"AmbientPlayer");
            gObj.transform.parent = myTrans;
            currentSource = gObj.AddComponent<AudioSource>();
            activeSources.Add(currentSource);
            Debug.Log($"{activeSources}�� {currentSource.name} ���� ����");
        }
        currentSource.outputAudioMixerGroup = group;
        currentSource.clip = clip;
        currentSource.loop = loop;

        // ��ġ�� �ִٸ� 3D �����
        if (pos.HasValue)
        {
            currentSource.transform.position = pos.Value;
            currentSource.spatialBlend = 1f;

            #region Ŀ����
            //src.rolloffMode = AudioRolloffMode.Custom;
            //AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.9f, 0.5f), new Keyframe(0.3f, 0.3f), new Keyframe(1f, 0f));
            //src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
            //src.minDistance = 0.3f;  // float �� �̳��� �׻� �ִ� ����
            //src.maxDistance = 5f; // float �� �̻��� �� �鸲
            #endregion

            #region �븻
            currentSource.rolloffMode = AudioRolloffMode.Logarithmic; // �ڿ������� ����
            currentSource.minDistance = 1f;  // float �� �̳��� �׻� �ִ� ����
            currentSource.maxDistance = 50f; // float �� �̻��� �� �鸲
            #endregion
        }
        else currentSource.spatialBlend = 0f;

        currentSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
        currentSource.volume = UnityEngine.Random.Range(0.95f, 1f);

        // ��� �� ����
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
    public override void SetVolumeZero()
    {
        base.SetVolumeZero();
    }

    private void NewDestroy(GameObject gObj, float length)
    {
        AudioSource aS = gObj.GetComponent<AudioSource>();
        UnityEngine.Object.Destroy (gObj, length);
        if (gObj.IsDestroyed())
        {
            Debug.Log($"{activeSources}�� {aS.name} ������");
            activeSources.Remove(aS);
        }
    }

}
