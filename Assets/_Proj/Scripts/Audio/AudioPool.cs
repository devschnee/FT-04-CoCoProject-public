
# region 수술
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum SFXMode
{
    OutGame = 0,
    InGame
}

public class AudioPool
{
    private readonly Transform parent;
    private readonly AudioMixerGroup defaultGroup;

    // 대기 중인 소스들
    private readonly Queue<AudioSource> pool = new Queue<AudioSource>();
    // 현재 사용 중인 소스들
    private readonly List<AudioSource> activePool = new List<AudioSource>();
    // 초기 생성된 풀(설정 변경용)
    private readonly List<AudioSource> poolList = new List<AudioSource>();

    public AudioPool(Transform parent, AudioMixerGroup defaultGroup, int size)
    {
        this.parent = parent;
        this.defaultGroup = defaultGroup;

        for (int i = 0; i < size; i++)
        {
            var src = CreatePooledSource($"PooledAudio_{i}", isExtra: false);
            poolList.Add(src);
            pool.Enqueue(src);
        }
    }

    /// <summary>
    /// 공통 AudioSource 생성 함수
    /// </summary>
    private AudioSource CreatePooledSource(string name, bool isExtra)
    {
        var gObj = new GameObject(name);
        gObj.transform.SetParent(parent);
        var src = gObj.AddComponent<AudioSource>();

        src.gameObject.tag = isExtra ? "newPooled" : "pooled";
        src.outputAudioMixerGroup = defaultGroup;
        src.volume = 1f;
        src.pitch = 1f;
        src.spatialBlend = 1f;

        SetOutGameMode(src); // 기본값은 OutGame 세팅 (필요하면 나중에 SettingPool에서 다시 설정)

        gObj.SetActive(false);
        return src;
    }

    /// <summary>
    /// 풀에서 하나 꺼내기
    /// </summary>
    public AudioSource GetSource()
    {
        AudioSource src = null;

        // Destroy 된 놈들이 큐에 남아 있을 수 있으니, null 필터링
        while (pool.Count > 0)
        {
            src = pool.Dequeue();
            if (src != null) break; // Unity null 체크
        }

        // 큐에 유효한 소스가 하나도 없으면 새로 생성
        if (src == null)
        {
            Debug.LogWarning("오디오 풀 부족 새 AudioSource 생성");
            src = CreatePooledSource("NewPooledAudio", isExtra: true);
        }

        activePool.Add(src);
        if (src != null)
        {
            src.gameObject.SetActive(true);
        }
        return src;
    }

    /// <summary>
    /// 사용 후 반환
    /// </summary>
    public void ReturnSource(AudioSource src)
    {
        if (src == null) return; // 이미 Destroy 됐으면 무시

        src.Stop();
        src.gameObject.SetActive(false);
        src.clip = null;
        activePool.Remove(src);

        // Destroy 된 애는 다시 큐에 넣지 않기
        if (src != null)
        {
            pool.Enqueue(src);
        }
    }

    public IEnumerator ReturnAfterDelay(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay);
        // 코루틴 도중에 Destroy 됐을 수도 있으니 체크
        if (src != null)
        {
            ReturnSource(src);
        }
    }

    //인게임 아웃게임 세팅
    private void SetInGameMode(AudioSource src)
    {
        if (src == null) return;

        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = 1.5f;
        src.maxDistance = 180f;
    }
    private void SetOutGameMode(AudioSource src)
    {
        if (src == null) return;

        src.rolloffMode = AudioRolloffMode.Custom;
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(50f, 0.877f),
            new Keyframe(60f, 0.59f),
            new Keyframe(80f, 0.34f),
            new Keyframe(128f, 0.125f),
            new Keyframe(200f, 0.002f)
        );
        src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
        src.minDistance = 40f;
        src.maxDistance = 200f;
    }

    public void PlayPool()
    {
        // 중간에 Destroy 된 애들 제거
        CleanupActiveList();

        foreach (var src in activePool)
        {
            if (src != null && !src.isPlaying) src.Play();
        }
    }

    public void PausePool()
    {
        CleanupActiveList();

        foreach (var src in activePool)
        {
            if (src != null && src.isPlaying) src.Pause();
        }
    }

    public void ResumePool()
    {
        CleanupActiveList();

        foreach (var src in activePool)
        {
            if (src != null && !src.isPlaying) src.UnPause();
        }
    }

    public void StopPool()
    {
        CleanupActiveList();

        foreach (var src in activePool)
        {
            if (src != null && src.isPlaying)
            {
                src.DOFade(0, 0.3f);
                src.Stop();
            }
        }
    }

    /// <summary>
    /// 볼륨/루프/클립 초기화 + newPooled 정리
    /// </summary>
    public void ResetPool(float volumeValue)
    {
        // activePool 정리
        CleanupActiveList();

        foreach (var src in activePool)
        {
            if (src != null)
            {
                if (src.isPlaying) src.Stop();
                src.loop = false;
                src.volume = volumeValue;
                src.pitch = 1f;
                src.clip = null;
            }
        }

        // 큐 안의 newPooled 정리 + Destroy된 레퍼런스 제거
        var tempQueue = new Queue<AudioSource>();

        while (pool.Count > 0)
        {
            var src = pool.Dequeue();

            if (src == null) continue; // Destroy 된 애는 버림

            if (src.CompareTag("newPooled"))
            {
                // 여기서 실제 GameObject는 삭제하지만,
                // 큐에서도 완전히 제거되므로 이후 Dequeue에서 다시 안 나옴
                GameObject go = src.gameObject;
                UnityEngine.GameObject.Destroy(go);
                continue;
            }

            tempQueue.Enqueue(src);
        }

        pool.Clear();
        foreach (var src in tempQueue)
        {
            pool.Enqueue(src);
        }

        // activePool 비워두고, 다음 재생 때 다시 GetSource()로 사용하게 함
        activePool.Clear();
    }

    /// <summary>
    /// 초기 풀에 볼륨/거리 셋팅
    /// </summary>
    public void SettingPool(float volumeValue, SFXMode sfxMode)
    {
        foreach (var src in poolList)
        {
            if (src != null)
            {
                src.volume = volumeValue;

                switch (sfxMode)
                {
                    case SFXMode.InGame:
                        SetInGameMode(src);
                        break;
                    case SFXMode.OutGame:
                        SetOutGameMode(src);
                        break;
                }
            }
        }
    }

    public void SetPoolVolumeHalf()
    {
        CleanupActiveList();

        foreach (var src in activePool)
        {
            if (src != null)
            {
                if (DOTween.IsTweening(src, true))
                {
                    src.DOKill();
                }
                src.DOFade(0.3f, 0.3f);
            }
        }
    }

    public void SetPoolVolumeNormal()
    {
        CleanupActiveList();

        foreach (var src in activePool)
        {
            if (src != null)
            {
                if (DOTween.IsTweening(src, true))
                {
                    src.DOKill();
                }
                src.DOFade(1, 0.3f);
            }
        }
    }

    public void SetPoolVolumeZero()
    {
        CleanupActiveList();

        foreach (var src in activePool)
        {
            if (src != null)
            {
                if (DOTween.IsTweening(src, true))
                {
                    src.DOKill();
                }
                src.DOFade(0, 0.2f);
            }
        }
    }

    /// <summary>
    /// 특정 클립을 재생 중인 Pool 안에 상태를 제어하는 메서드
    /// int mode : 0 = 재생, 1 = 멈춤, 2 = 재개, 3 = 멈추고 재생 위치를 처음으로, 4 = 멈추고 클립 빼기, 5 = 볼륨 조절
    /// mode 4, 5는 float volumeValue 파라미터를 넣을 수 있음
    /// </summary>
    public void CustomPoolSourceInPool(AudioClip clip, int mode, float? volumeValue = null)
    {
        CleanupActiveList();

        foreach (var src in activePool)
        {
            if (src == null) continue;
            if (src.clip != clip) continue;

            switch (mode)
            {
                case 0:
                    src.Play();
                    break;
                case 1:
                    src.Pause();
                    break;
                case 2:
                    src.UnPause();
                    break;
                case 3:
                    src.Stop();
                    break;
                case 4:
                    src.DOFade(0, 0.3f).OnComplete(() =>
                    {
                        if (src == null) return;
                        src.Stop();
                        src.clip = null;
                        src.volume = volumeValue ?? 1f;
                    });
                    break;
                case 5:
                    if (volumeValue != null)
                    {
                        src.volume = (float)volumeValue;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// activePool에서 Destroy된 AudioSource들 정리
    /// </summary>
    private void CleanupActiveList()
    {
        activePool.RemoveAll(src => src == null);
    }

    /// <summary>
    /// 완전 삭제용 (씬 정리 시 호출하고 싶으면 사용)
    /// </summary>
    public void ClearAll()
    {
        foreach (var src in pool)
        {
            if (src != null)
                UnityEngine.GameObject.Destroy(src.gameObject);
        }
        foreach (var src in activePool)
        {
            if (src != null)
                UnityEngine.GameObject.Destroy(src.gameObject);
        }

        pool.Clear();
        activePool.Clear();
        poolList.Clear();
    }
}

#endregion