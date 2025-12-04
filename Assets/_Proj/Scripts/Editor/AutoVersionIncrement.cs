using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class AutoVersionIncrement : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    private const int PatchLimit = 9;
    private const int MinorLimit = 9;

    public void OnPreprocessBuild(BuildReport report)
    {
        string version = PlayerSettings.bundleVersion;
        string[] parts = version.Split('.');

        if (parts.Length != 3)
        {
            Debug.LogError($"[AtuoVersionIncrement]Invalid version format: {version}");
            return;
        }

        int major = int.Parse(parts[0]);
        int minor = int.Parse(parts[1]);
        int patch = int.Parse(parts[2]);

        patch++;

        if (patch >= PatchLimit)
        {
            patch = 0;
            minor++;
            Debug.Log("[AtuoVersionIncrement] Patch limit reached → Minor++");
        }

        if (minor > MinorLimit)
        {
            minor = 0;
            major++;

            Debug.Log("[AtuoVersionIncrement] Minor limit reached → Major++");
        }

        // PlayerSettings에 새 버전 저장
        string newVersion = $"{major}.{minor}.{patch}";
        PlayerSettings.bundleVersion = newVersion;

        Debug.Log($"Version updated: {version} → {newVersion}");
        AssetDatabase.SaveAssets();
    }
}
