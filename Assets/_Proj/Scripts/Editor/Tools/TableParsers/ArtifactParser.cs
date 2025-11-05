using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ArtifactParser
{
    public static void Import(string csvPath)
    {
        string textCsvPath = "Assets/_Proj/Data/CSV/tbl_text_mst.csv";
        var textDict = TextParser.Import(textCsvPath);

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length <= 1) return;

        var db = ScriptableObject.CreateInstance<ArtifactDatabase>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            var v = line.Split(',');

            if (v.Length < 4)
            {
                Debug.LogWarning($"[ArtifactParser] {i}행 데이터 부족 → 스킵");
                continue;
            }

            if (!int.TryParse(v[0].Trim('\uFEFF'), out int id))
            {
                Debug.LogWarning($"[ArtifactParser] ID 변환 실패 → {v[0]}");
                continue;
            }

            Enum.TryParse(v[3], true, out ArtifactType category);

            string rawName = v[1];
            string finalName = TextParser.Resolve(rawName, textDict);

            db.artifactList.Add(new ArtifactData
            {
                artifact_id = id,
                artifact_name = finalName,
                artifact_icon = v[2],
                artifact_type = category,
            });
        }

        string assetPath = "Assets/_Proj/Data/ScriptableObject/Artifact/ArtifactDatabase.asset";
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ArtifactParser] 변환 완료 → {assetPath}");
    }
}