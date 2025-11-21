using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioPool
{
    private readonly Transform parent;
    private readonly AudioMixerGroup defaultGroup;
    private readonly Queue<AudioSource> pool = new Queue<AudioSource>();

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
            src.volume = 1f;
            src.pitch = 1f;
            src.gameObject.SetActive(false);
            pool.Enqueue(src);
        }
    }

    public AudioSource GetSource()
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning("���� ����Կ�");
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

    // Ǯ�� ��� ���� �� �ݺ� ����ϴ� ���ϵ� Ǯ���� ������ ex) ȯ���� �߿��� �� �帣�� �Ҹ�
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
    public void SetPoolVolumeHalf()
    {
        foreach (var src in pool)
        {
            if (src != null) 
            {
                if (DOTween.IsTweening(src, true))
                {
                    src.DOKill();
                }
                src.DOFade(0.3f, 0.5f);
            }
        }
    }
    public void SetPoolVolumeNormal()
    {
        foreach (var src in pool)
        {
            if (src != null) 
            {
                if (DOTween.IsTweening(src, true))
                {
                    src.DOKill();
                }
                src.DOFade(1, 0.5f);
            }
        }
    }
    public void SetPoolVolumeZero()
    {
        foreach (var src in pool)
        {
            if (src != null) 
            {
                if (DOTween.IsTweening(src, true))
                {
                    src.DOKill();
                }
                src.DOFade(0, 0.5f);
            }
        }
    }

    public IEnumerator ReturnAfterDelay(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnSource(src);
    }
    
}
