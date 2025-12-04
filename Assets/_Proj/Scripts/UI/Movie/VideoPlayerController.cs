using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.Android;
using UnityEngine.UI;

public class VideoPlayerController : MonoBehaviour
{
    public static VideoPlayerController Instance;

    public VideoPlayer player;

    //LSH 1117 추가
    private AudioSource cutsceneSource;

    private bool isPlaying = false;

    void Awake()
    {



        Instance = this;

        // 중복등록 방지
        player.loopPointReached -= OnFinished;
        player.errorReceived -= OnError;

        // 이벤트 등록
        player.loopPointReached += OnFinished;
        player.errorReceived += OnError;

        //LSH 1117 추가
        cutsceneSource = AudioManager.Instance.GetAudioSourceForVideoPlayer();
        cutsceneSource.playOnAwake = false;
        cutsceneSource.loop = false;
        cutsceneSource.spatialBlend = 0f;

        StageUIManager.Instance.videoImage.GetComponentInChildren<Button>().onClick.AddListener(() => OnFinished(player));
    }
    //LSH 1117 추가
    void Start()
    {
        player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        player.EnableAudioTrack(0, true);
        player.SetTargetAudioSource(0, cutsceneSource);
    }

    //-------------------------------------------
    // 1) StageManager가 요구하는 코루틴 방식
    //-------------------------------------------
    public IEnumerator PlayCutscene(string url, System.Action onFinish = null)
    {
        yield return PlayRoutine(url, onFinish);
    }

    IEnumerator PlayRoutine(string url, System.Action onFinish = null)
    {
        Debug.Log("[Cutscene] Load: " + url);

        // 중복 재생 처리: 현재 재생 중이면 강제로 멈춤
        if (isPlaying)
        {
            Debug.Log("[Cutscene] Warning: Already playing. Stopping previous."); 
            player.loopPointReached -= OnFinished;
            player.Stop();
            player.loopPointReached += OnFinished;
            isPlaying = false;
            // 약간의 대기(옵션)
            yield return null;
        }

        isPlaying = true;

        AudioManager.Instance.EnterCutscene();

        player.Stop();
        ClearVideoFrame();
        player.source = VideoSource.Url;
        player.url = url;

        player.Prepare();

        float timeout = 5f;
        while (!player.isPrepared)
        {
            timeout -= Time.deltaTime;
            if (timeout < 0)
            {
                Debug.LogError("[Cutscene] Prepare Timeout!");
                isPlaying = false;
                onFinish?.Invoke();
                yield break;
            }
            yield return null;
        }

        Debug.Log("[Cutscene] Playing");
        player.Play();

        // 끝날 때까지 대기 (OnFinished에서 isPlaying = false로 변경됨)
        while (isPlaying)
            yield return null;

        onFinish?.Invoke();
    }

    void OnFinished(VideoPlayer vp)
    {
        if (!isPlaying) return;

        Debug.Log("[Cutscene] Finished.");
        isPlaying = false;

        StageUIManager.Instance.videoImage.SetActive(false);
        AudioManager.Instance.ExitCutscene();
    }

    void OnError(VideoPlayer vp, string msg)
    {
        Debug.LogError("[Cutscene] ERROR: " + msg);
        isPlaying = false;
    }
    public void ClearVideoFrame()
    {
        if (player.targetTexture != null)
        {
            RenderTexture rt = player.targetTexture;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;
        }
    }
}