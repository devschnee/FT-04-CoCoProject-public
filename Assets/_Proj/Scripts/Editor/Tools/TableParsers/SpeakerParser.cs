using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using static SpeakerData;

public class SpeakerParser
{
    public static void Import(string csvPath)
    {
        string textCsvPath = "Assets/_Proj/Data/CSV/tbl_text_mst.csv";
        var textDict = TextParser.Import(textCsvPath);

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length <= 1) return;

        var db = ScriptableObject.CreateInstance<SpeakerDatabase>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            var v = System.Text.RegularExpressions.Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            if (v.Length < 3)
            {
                Debug.LogWarning($"[SpeakerParser] {i}행 데이터 부족 → 스킵");
                continue;
            }

            if (!Enum.TryParse(v[0], true, out SpeakerId speakerType))
            {
                Debug.LogWarning($"[SpeakerParser] ID 변환 실패 → {v[0]}");
                continue;
            }



            db.speakerList.Add(new SpeakerData
            {
                speaker_id = speakerType,
                display_name = v[1],
                portrait_set_prefix = v[2],
            });
        }

        string assetPath = "Assets/_Proj/Data/ScriptableObject/Speaker/SpeakerDatabase.asset";
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SpeakerParser] 변환 완료 → {assetPath}");
    }
}
