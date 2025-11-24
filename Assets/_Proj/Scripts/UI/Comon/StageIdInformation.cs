using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageIdInformation : MonoBehaviour
{
    public static StageIdInformation Instance { get; private set; }
    public event Action OnLoadMainScene;

    public PanelRouter pr;
    public string stageIdInfo;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        stageIdInfo = null;
    }
    void OnEnable()
    {
        OnLoadMainScene += OpenStagePanel;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        OnLoadMainScene -= OpenStagePanel;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 메인 씬 로딩됐을 때만
        if (scene.name == "Main")
        {
            pr = FindAnyObjectByType<PanelRouter>(); // 여기서 재연결
            OnLoadMainScene?.Invoke();
            print("씬 로딩됨 → 이벤트 실행!");
        }
    }

    public void OpenStagePanel()
    {
        print("메서드실행하지롱");
        if(stageIdInfo != null)
        {
            if (stageIdInfo.Contains("stage_1"))
            {
                pr.ToggleStage("chapter_1");
                pr.selectStageDimOverlay.gameObject.SetActive(false);
            }
            else if (stageIdInfo.Contains("stage_2"))
            {
                pr.ToggleStage("chapter_2");
                pr.selectStageDimOverlay.gameObject.SetActive(false);
            }
            else if (stageIdInfo.Contains("stage_3"))
            {
                pr.ToggleStage("chapter_3");
                pr.selectStageDimOverlay.gameObject.SetActive(false);
            }
        }
    }
}
