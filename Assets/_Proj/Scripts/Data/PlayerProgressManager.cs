using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerProgressManager : MonoBehaviour
{
    public static PlayerProgressManager Instance;

    public static event Action OnProgressUpdated;

    private Dictionary<string, StageProgressData> stageProgressDict = new();
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 로드될 때마다 진행도 알림 재발행
        LoadProgress();
        OnProgressUpdated?.Invoke();
    }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else Destroy(gameObject);
    }

    public StageProgressData GetStageProgress(string stageId)
    {
        if (!stageProgressDict.ContainsKey(stageId))
            stageProgressDict[stageId] = new StageProgressData { stageId = stageId };
        return stageProgressDict[stageId];
    }

    public void UpdateStageTreasure(string stageId, bool[] newlyCollected)
    {
        var progress = GetStageProgress(stageId);

        int newCount = newlyCollected.Count(x => x);
        int prevCount = progress.treasureCollected.Count(x => x);

        // 더 많이 모았을 때만 갱신
        if (newCount > prevCount)
        {
            for (int i = 0; i < 3; i++)
            {
                if (newlyCollected[i])
                    progress.treasureCollected[i] = true;
            }

            SaveProgress();
            Debug.Log($"[PlayerProgressManager] Stage '{stageId}' 진행도 갱신됨 → {newCount}개 보물");
            OnProgressUpdated?.Invoke();
        }
        else
        {
            Debug.Log($"[PlayerProgressManager] Stage '{stageId}' 진행도 갱신 안 함 (기존 {prevCount}, 새 {newCount})");
        }
    }

    void SaveProgress()
    {
        //Todo : Firebase 연동시 변경해야 할 부분

        var wrapper = new Wrapper(stageProgressDict);
        string json = JsonUtility.ToJson(wrapper, true);
        PlayerPrefs.SetString("StageProgress", json);
        PlayerPrefs.Save(); // 반드시 저장
        Debug.Log($"[SaveProgress] 저장됨:\n{json}");
    }

    public void LoadProgress()
    {
        //Todo : Firebase 연동시 변경해야 할 부분
        if (PlayerPrefs.HasKey("StageProgress"))
        {
            string json = PlayerPrefs.GetString("StageProgress");
            Debug.Log($"[LoadProgress] 로드된 JSON:\n{json}");

            var wrapper = JsonUtility.FromJson<Wrapper>(json);
            if (wrapper != null && wrapper.list != null)
                stageProgressDict = wrapper.ToDictionary();
            else
                stageProgressDict = new Dictionary<string, StageProgressData>();
        }
        else
        {
            stageProgressDict = new Dictionary<string, StageProgressData>();
        }
    }

    [Serializable]
    private class Wrapper
    {
        public List<StageProgressData> list = new();

        public Wrapper() { }

        public Wrapper(Dictionary<string, StageProgressData> dict)
        {
            list = new List<StageProgressData>(dict.Values);
        }

        public Dictionary<string, StageProgressData> ToDictionary()
        {
            var d = new Dictionary<string, StageProgressData>();
            foreach (var e in list)
                d[e.stageId] = e;
            return d;
        }
    }
}