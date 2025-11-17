using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System.Collections;

public static class CutscenePathBuilder
{
    public static string BuildStreamingAssetsPath(string relativePath)
    {
        return Path.Combine(Application.streamingAssetsPath, relativePath);
    }

    public static string BuildUrl(string relativePath)
    {
        string streamingPath = BuildStreamingAssetsPath(relativePath);

        // Android 내부 APK 경로는 jar:로 시작하므로 VideoPlayer 직접 접근 불가
        if (Application.platform == RuntimePlatform.Android)
        {
            // persistentDataPath로 복사해서 file:// 경로로 접근
            string persistentFile = Path.Combine(Application.persistentDataPath, relativePath);
            return "file://" + persistentFile;
        }
        else
        {
            // Standalone, Editor, iOS는 파일 직접 접근 가능
            if (!streamingPath.StartsWith("file://"))
                streamingPath = "file://" + streamingPath;

            return streamingPath;
        }
    }
}