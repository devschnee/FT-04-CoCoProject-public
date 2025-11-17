using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

public class VideoPlayerController : MonoBehaviour
{
    public VideoPlayer player;
    public void PlayCutscene(string stageId, bool isStart)
    {
        string url = isStart ?
            DataManager.Instance.Stage.GetStartCutsceneUrl(stageId) :
            DataManager.Instance.Stage.GetEndCutsceneUrl(stageId);

        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[Cutscene] No cutscene for stage " + stageId);
            return;
        }

        StartCoroutine(PlayRoutine(url));
    }

    IEnumerator PlayRoutine(string url)
    {
        Debug.Log("[Cutscene] Loading: " + url);

        player.source = VideoSource.Url;
        player.url = url;

        player.prepareCompleted += OnPrepared;
        player.errorReceived += OnError;

        player.Prepare();

        while (!player.isPrepared)
            yield return null;

        player.Play();
    }

    void OnPrepared(VideoPlayer vp)
    {
        Debug.Log("[Cutscene] Ready. Playing.");
    }

    void OnError(VideoPlayer vp, string msg)
    {
        Debug.LogError("[Cutscene] ERROR: " + msg);
    }

}
