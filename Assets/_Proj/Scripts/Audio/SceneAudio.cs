using System.Collections;
using UnityEngine;

// BGM은 SFX와 달리 여기서 음악파일을 넣고 실행하는 구조
// 특히 AudioManager는 DDOL이니 이 스크립트가 붙은 오브젝트를 다른 씬 마다 넣어서 다른 노래를 집어 넣으면 알아서 재생 해줌.
// SFX는 스크립터블 오브젝트로 할 생각, 이 스크립트는 해당 씬 배경음 설정하는 것임

public class SceneAudio : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] BGMKey key;
    [SerializeField] int bgmClipIndex;
    [SerializeField] float fadeInTime;
    [SerializeField] float fadeOutTime;
    [SerializeField] bool loop;

    [Header("IntroBGM")]
    [SerializeField] AudioClip introBGM;
    [SerializeField] float introDuration;
    [SerializeField] bool useIntro;

    //[Header("SceneMainCamera")]
    //[SerializeField] private Camera cam;

    //void Start()
    //{
    //    if (AudioManager.Instance != null)
    //    {
    //        if (useIntro && introBGM != null)
    //        {
    //            StartCoroutine(PlayIntroBGM());
    //        }
    //        else
    //        {
    //            AudioManager.Instance.PlayBGM<BGMKey>(AudioType.BGM, key, bgmClipIndex, fadeInTime, fadeOutTime, loop);
    //        }
    //    }
    //}

    //private IEnumerator PlayIntroBGM()
    //{
    //    AudioManager.Instance.PlayBGM<BGMKey>(AudioType.BGM, key, bgmClipIndex, fadeInTime, fadeOutTime, false);

    //    yield return new WaitForSeconds(introDuration);

    //    AudioManager.Instance.PlayBGM<BGMKey>(AudioType.BGM, key, bgmClipIndex, fadeInTime, fadeOutTime, loop);
    //    //if (cam != null)
    //    //{
    //    //    new WaitForSeconds(1f);
    //    //    var aL = cam.GetComponent<AudioListener>();
    //    //    aL.enabled = false;
    //    //}
    //}
}


