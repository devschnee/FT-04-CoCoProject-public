using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// AudioEvents.cs에서 구독 듣고 AudioManager에게 실행 부탁.
// key(음악 뭐할지), pooled(오브젝트 풀링인지), position(2d 소리할지 3d 소리할지)
//public class AudioEventListener : MonoBehaviour
//{
//    public static AudioEventListener Instance { get; private set; }
//    // DDOL하나 때문에 일단 싱글톤으로 만들었음, 싱글톤으로 접근은 안하고 이벤트 수신만

//    private void Awake()
//    {
//        if (Instance != null && Instance != this)
//        {
//            Debug.LogWarning("중복 감지 제거됨");
//            Destroy(gameObject);
//            return;
//        }
//        Instance = this;
//        DontDestroyOnLoad(gameObject);
//        Debug.Log("AudioEventListener 생성");
//    }

//    private void OnEnable()
//    {
//        AudioEvents.OnPlaySFX += HandlePlaySFX;
//    }

//    private void OnDisable()
//    {
//        AudioEvents.OnPlaySFX -= HandlePlaySFX;
//    }

//    private void HandlePlaySFX(SFXKey key, Vector3 pos, bool pooled)
//    {
//        PlaySFX(key, pooled, pos);
//    }

//    public void PlaySFX(SFXKey key, bool pooled = false, Vector3? pos = null)
//    {
//        int index = AudioManager.Instance.GetIndex(key);
//        // 로컬 즉시 재생
//        if (pooled) AudioManager.Instance.PlayPooledSFX(key, pos, index);
//        else AudioManager.Instance.PlayOneShotSFX(key, pos, index);
//    }
//}

