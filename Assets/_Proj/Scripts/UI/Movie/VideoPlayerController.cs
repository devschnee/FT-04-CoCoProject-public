using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using System.Threading.Tasks;

public class VideoPlayerController : MonoBehaviour
{
    public static VideoPlayerController Instance;

    public VideoPlayer player;

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
    }

    //-------------------------------------------
    // 1) StageManager가 요구하는 코루틴 방식
    //-------------------------------------------
    public IEnumerator PlayCutscene(string url, bool waitUntilFinish)
    {
        yield return PlayRoutine(url, waitUntilFinish);
    }

    //-------------------------------------------
    // 2) StageManager가 요구하는 async/await 방식
    //-------------------------------------------
    public async Task PlayAsync(string url)
    {
        var tcs = new TaskCompletionSource<bool>();

        StartCoroutine(PlayRoutine(url, true, () =>
        {
            tcs.TrySetResult(true);
        }));

        await tcs.Task;
    }

    //-------------------------------------------
    // 공통 실행 로직
    //-------------------------------------------
    IEnumerator PlayRoutine(string url, bool waitUntilFinish, System.Action onFinish = null)
    {
        Debug.Log("[Cutscene] Load: " + url);

        // 중복 재생 처리: 현재 재생 중이면 강제로 멈춤
        if (isPlaying)
        {
            Debug.Log("[Cutscene] Warning: Already playing. Stopping previous.");
            player.Stop();
            isPlaying = false;
            // 약간의 대기(옵션)
            yield return null;
        }

        isPlaying = true;

        AudioManager.Instance.StopAllAudioGroup();

        player.Stop();
        player.source = VideoSource.Url;
        player.url = url;
        player.Prepare();

        float timeout = 5f;
        bool prepareFailed = false;
        while (!player.isPrepared)
        {
            timeout -= Time.deltaTime;
            if (timeout < 0)
            {
                Debug.LogError("[Cutscene] Prepare Timeout!");
                prepareFailed = true;
                break;
            }
            yield return null;
        }

        if (prepareFailed)
        {
            // 준비 실패 시 상태 정리 및 콜백 호출
            isPlaying = false;
            onFinish?.Invoke(); // 호출자(PlayAsync의 tcs 등)에게 끝났음을 알림
            yield break;
        }

        Debug.Log("[Cutscene] Playing: " + url);
        player.Play();

        if (waitUntilFinish)
        {
            // isPlaying은 OnFinished / OnError에서 false로 설정됨
            while (isPlaying)
                yield return null;
        }

        onFinish?.Invoke();
    }

    void OnFinished(VideoPlayer vp)
    {
        Debug.Log("[Cutscene] Finished.");
        isPlaying = false;

        StageUIManager.Instance.videoImage.SetActive(false);
        AudioManager.Instance.ResetAllAudioGroup();
    }

    void OnError(VideoPlayer vp, string msg)
    {
        Debug.LogError("[Cutscene] ERROR: " + msg);
        isPlaying = false;
    }
}