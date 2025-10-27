using System;
using System.IO;
using UnityEngine;

[Serializable]
public class SettingData // 게임 설정처럼 옵션 저장하고 싶으면 데이터 넣으세요
{
    public AudioVolumeData audio = new AudioVolumeData();
}

[DefaultExecutionOrder(-100)]
public class SettingManager : MonoBehaviour
{
    public SettingData settingData;
    public static SettingManager Instance { get; private set; }

    // 플랫폼 분할 윈도우와 나머지들. 윈도우 저장 경로 내문서->코코두기->option.json
#if UNITY_STANDALONE_WIN
    private static readonly string savePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "CocoDoogy", "option.json");
#else
    private static readonly string savePath = Path.Combine(Application.persistentDataPath, "options.json");
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    public void SaveSettings()
    {
        string directory = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        string json = JsonUtility.ToJson(settingData);
        File.WriteAllText(savePath, json);
    }

    public void LoadSettings()
    {
        if (!File.Exists(savePath))
        {
            // 파일이 없을 시 새로 생성
            settingData = new SettingData { audio = new AudioVolumeData() };
            SaveSettings();
            return;
        }

        string json = File.ReadAllText(savePath);
        settingData = JsonUtility.FromJson<SettingData>(json);
    }

    private void OnApplicationQuit()
    {
        SaveSettings();
    }

}
