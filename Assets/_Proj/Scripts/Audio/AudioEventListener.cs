using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// AudioEvents.cs에서 구독 듣고 AudioManager에게 실행 부탁.
// key(음악 뭐할지), pooled(오브젝트 풀링인지), position(2d 소리할지 3d 소리할지)
public class AudioEventListener : MonoBehaviour
{
    public static AudioEventListener Instance { get; private set; }
    // DDOL하나 때문에 일단 싱글톤으로 만들었음, 싱글톤으로 접근은 안하고 이벤트 수신만
    // 근데 AudioManager에 붙인다면 굳이 싱글톤 필요해?

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("중복 감지 제거됨");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("AudioEventListener 생성");
    }

    private void OnEnable()
    {
        AudioEvents.OnPlayAudio += HandlePlayAudio;
    }

    private void OnDisable()
    {
        AudioEvents.OnPlayAudio -= HandlePlayAudio;
    }

    private void HandlePlayAudio(Enum key, int index = -1, float fadeIn = 0, float fadeOut = 0, bool loop = false, bool pooled = false, Vector3? pos = null)
    {
        PlayAudio(key, index, fadeIn, fadeOut, loop, pooled, pos);
    }

    public void PlayAudio(Enum key, int index = -1, float fadeIn = 0, float fadeOut = 0, bool loop = false, bool pooled = false, Vector3? pos = null)
    {
        AudioManager.Instance.PlayAudio(key, index, fadeIn, fadeOut, loop, pooled, pos);
    }
}

