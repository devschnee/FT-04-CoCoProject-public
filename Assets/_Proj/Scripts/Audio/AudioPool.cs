using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioPool
{
    private readonly Queue<AudioSource> pool = new();
    private readonly Transform parent;
    private readonly AudioMixerGroup defaultGroup;

    public AudioPool(Transform parent, AudioMixerGroup defaultGroup, int size)
    {
        this.parent = parent;
        this.defaultGroup = defaultGroup;
        var poolParent = new GameObject("PoolParent");
        poolParent.transform.SetParent(parent);

        for (int i = 0; i < size; i++)
        {
            var src = new GameObject($"PooledAudio_{i}").AddComponent<AudioSource>();
            src.outputAudioMixerGroup = defaultGroup;
            src.transform.SetParent(poolParent.transform);
            src.gameObject.SetActive(false);
            pool.Enqueue(src);
        }
    }

    public AudioSource GetSource()
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning("새로 만들께용");
            return new GameObject("NewPooledAudio").AddComponent<AudioSource>();
        }

        var src = pool.Dequeue();
        src.gameObject.SetActive(true);
        return src;
    }

    public void ReturnSource(AudioSource src)
    {
        src.Stop();
        src.gameObject.SetActive(false);
        pool.Enqueue(src);
    }

    // 풀로 재생 파일 중 반복 재생하는 파일들 풀에서 꺼내기 ex) 환경음 중에서 물 흐르는 소리
    public void StopSFX(AudioSource src)
    {
        if (src.loop == true)
        {
            ReturnSource(src);
        }
    }

    public IEnumerator ReturnAfterDelay(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnSource(src);
    }
}
