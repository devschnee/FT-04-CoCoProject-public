using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ManualParser
{
    public static void Import(string csvPath)
    {
        string textCsvPath = "Assets/_Proj/Data/CSV/tbl_text_mst.csv";
        var textDict = TextParser.Import(textCsvPath);

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length <= 1) return;

        var db = ScriptableObject.CreateInstance<ManualDatabase>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            var v = System.Text.RegularExpressions.Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            if (v.Length < 4)
            {
                Debug.LogWarning($"[ManualParser] {i}행 데이터 부족 → 스킵");
                continue;
            }

            if (!int.TryParse(v[0].Trim('\uFEFF'), out int id))
            {
                Debug.LogWarning($"[ManualParser] ID 변환 실패 → {v[0]}");
                continue;
            }

            string rawName = v[1];
            string finalName = TextParser.Resolve(rawName, textDict);

            string rawDesc = v[3];
            string finalDesc = TextParser.Resolve(rawDesc, textDict);

            db.manualList.Add(new ManualData
            {
                manual_id = id,
                manual_name = finalName,
                manual_img = v[2],
                manual_desc = finalDesc,
            });
        }

        string assetPath = "Assets/_Proj/Data/ScriptableObject/Manual/ManualDatabase.asset";
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ManualParser] 변환 완료 → {assetPath}");
    }
}
