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
            Debug.Log($"{activeSources}에 {currentSource.name} 집어 넣음");
        }
        currentSource.outputAudioMixerGroup = group;
        currentSource.clip = clip;
        currentSource.loop = loop;

        // 위치가 있다면 3D 사운드로
        if (pos.HasValue)
        {
            currentSource.transform.position = pos.Value;
            currentSource.spatialBlend = 1f;

            #region 커스텀
            //src.rolloffMode = AudioRolloffMode.Custom;
            //AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.9f, 0.5f), new Keyframe(0.3f, 0.3f), new Keyframe(1f, 0f));
            //src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
            //src.minDistance = 0.3f;  // float 값 이내는 항상 최대 볼륨
            //src.maxDistance = 5f; // float 값 이상은 안 들림
            #endregion

            #region 노말
            currentSource.rolloffMode = AudioRolloffMode.Logarithmic; // 자연스럽게 감소
            currentSource.minDistance = 1f;  // float 값 이내는 항상 최대 볼륨
            currentSource.maxDistance = 50f; // float 값 이상은 안 들림
            #endregion
        }
        else currentSource.spatialBlend = 0f;

        currentSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
        currentSource.volume = UnityEngine.Random.Range(0.95f, 1f);

        // 재생 및 삭제
        if (pooled)
        {
            currentSource.Play();
            coroutineHost.StartCoroutine(audioPool.ReturnAfterDelay(currentSource, clip.length));
        }
        if (!pooled)
        {
            //play
            currentSource.Play();
            // loop일때 정지 및 빼는거 추가 해야함
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

    private void NewDestroy(GameObject gObj, float length)
    {
        AudioSource aS = gObj.GetComponent<AudioSource>();
        UnityEngine.Object.Destroy (gObj, length);
        if (gObj.IsDestroyed())
        {
            Debug.Log($"{activeSources}에 {aS.name} 삭제함");
            activeSources.Remove(aS);
        }
    }

}
