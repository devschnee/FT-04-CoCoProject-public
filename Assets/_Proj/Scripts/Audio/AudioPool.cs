using DG.Tweening;
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
        //var poolParent = new GameObject("PoolParent");
        //poolParent.transform.SetParent(parent);

        for (int i = 0; i < size; i++)
        {
            var src = new GameObject($"PooledAudio_{i}").AddComponent<AudioSource>();
            src.gameObject.tag = "pooled";
            src.outputAudioMixerGroup = defaultGroup;
            src.transform.SetParent(parent);
            src.gameObject.SetActive(false);
            pool.Enqueue(src);
        }
    }

    public AudioSource GetSource()
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning("새로 만들게용");
            var newPool = new GameObject("NewPooledAudio").AddComponent<AudioSource>();
            newPool.transform.SetParent(parent);
            newPool.tag = "newPooled";
            return newPool;
        }

        var src = pool.Dequeue();
        src.gameObject.SetActive(true);
        return src;
    }

    public void ReturnSource(AudioSource src)
    {
        src.Stop();
        src.gameObject.SetActive(false);
        src.clip = null;
        pool.Enqueue(src);
    }

    // 풀로 재생 파일 중 반복 재생하는 파일들 풀에서 꺼내기 ex) 환경음 중에서 물 흐르는 소리
    public void PlayPool()
    {
        foreach (var src in pool)
        {
            if (src != null && !src.isPlaying) src.Play();
        }
    }
    
    public void PausePool()
    {
        foreach (var src in pool)
        {
            if (src != null && src.isPlaying) src.Pause();
        }
    }

    public void ResumePool()
    {
        foreach (var src in pool)
        {
            if (src != null && !src.isPlaying) src.UnPause();
        }
    }

    public void StopPool()
    {
        foreach (var src in pool)
        {
            if (src != null && src.isPlaying) 
            {
                src.DOFade(0, 1f);
                src.Stop();
            }
        }
    }

    public void ResetPool()
    {
        foreach (var src in pool)
        {
            if (src != null)
            {
                if (src.isPlaying) src.Stop();
                src.loop = false;
                src.volume = 1f;
                src.pitch = 1f;
                src.clip = null;
            }
        }
        foreach (var gObj in pool)
        {
            if (gObj.CompareTag("newPooled"))
            {
                GameObject gO;
                gO = gObj.gameObject;
                UnityEngine.GameObject.Destroy(gO);
            }
        }
    }

    public IEnumerator ReturnAfterDelay(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnSource(src);
    }
    
}
