using System.Collections;
using UnityEngine;

public enum BGMType
{
    Main = 0,
    Other
}
public class SceneAudio : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] BGMKey key;
    [SerializeField] int bgmClipIndex;
    [SerializeField] float fadeInTime;
    [SerializeField] float fadeOutTime;
    [SerializeField] bool loop;

    [Header("IntroBGM")]
    [SerializeField] int introClipIndex;
    [SerializeField] float introDuration;
    [SerializeField] bool useIntro;

    [SerializeField] BGMType bGMType;

    // [Header("SceneMainCamera")]
    // [SerializeField] private Camera cam;

    public void StartBGM()
    {
       if (AudioManager.Instance != null)
       {
            if (bGMType == BGMType.Main)
            {
                StartCoroutine(PlayMainSceneBGM());
            }
            else
            {
                if (useIntro)
                {
                    StartCoroutine(PlayIntroBGM());
                }
                else
                {
                    AudioManager.Instance.PlayAudio(key, bgmClipIndex, fadeInTime, fadeOutTime, loop);
                }
            }
       }
    }

    private IEnumerator PlayMainSceneBGM()
    {
        while (true)
        {
            AudioManager.Instance.PlayAudio(BGMKey.Main, -1, fadeInTime, fadeOutTime, false);
            var clip = AudioManager.Instance.GetBGMClip();
            Debug.Log($"재생 중인 배경음 : {clip.name}, 길이 : {clip.length}");
            yield return new WaitForSeconds(clip.length);
        }
    }

    public void StopSceneAudioCoroutine()
    {
        StopAllCoroutines();
    }
    private IEnumerator PlayIntroBGM()
    {
       AudioManager.Instance.PlayAudio(key, introClipIndex, fadeInTime, fadeOutTime, false);

       yield return new WaitForSeconds(introDuration);

       AudioManager.Instance.PlayAudio(key, bgmClipIndex, fadeInTime, fadeOutTime, loop);
       //if (cam != null)
       //{
       //    new WaitForSeconds(1f);
       //    var aL = cam.GetComponent<AudioListener>();
       //    aL.enabled = false;
       //}
    }
}


