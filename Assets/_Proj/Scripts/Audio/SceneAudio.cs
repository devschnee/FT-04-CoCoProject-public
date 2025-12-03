using System.Collections;
using UnityEngine;

public enum BGMType
{
    Main = 0,
    Other,
    Stage
}
public class SceneAudio : MonoBehaviour
{   
    [Header("SceneType")]
    [SerializeField] BGMType bgmType;

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


    private string currentStageId;

    // [Header("SceneMainCamera")]
    // [SerializeField] private Camera cam;

    private void Start()
    {
        //StartBGM();    
    }

    public void StartBGM()
    {
       if (AudioManager.Instance != null)
       {
            Debug.Log("씬오디오 시작해요");

            if (bgmType == BGMType.Main)
            {
                StartCoroutine(PlayMainSceneBGM());
            }
            else if (bgmType == BGMType.Other)
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
            else if (bgmType == BGMType.Stage)
            {
                currentStageId = FirebaseManager.Instance.selectStageID;
                Debug.Log($"1.currentStageId : {currentStageId}");
                // var data = DataManager.Instance.Stage.GetData(currentStageId);
                // Debug.Log($"2.data : {data}");
                // string bgmId = data.stage_bgm;
                // Debug.Log($"3.bgmId : {bgmId}");
                // AudioManager.Instance.PlayBGMForResources(bgmId, 0.5f, 0.5f, true);

                AudioClip clip = DataManager.Instance.Stage.GetAudioClip(currentStageId);
                Debug.Log($"clip name : {clip.name}, {clip}");
                AudioManager.Instance.PlayBGMForResources(clip, fadeInTime, fadeOutTime, true);
            }
       }
    }

    private IEnumerator PlayMainSceneBGM()
    {
        while (true)
        {
            AudioManager.Instance.PlayAudio(BGMKey.Main, -1, fadeInTime, fadeOutTime, false, forcePlay: true);
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


