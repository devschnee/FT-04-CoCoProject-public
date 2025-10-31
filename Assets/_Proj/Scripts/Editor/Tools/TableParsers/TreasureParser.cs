using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TreasureParser
{
    public static void Import(string csvPath)
    {
        string textCsvPath = "Assets/_Proj/Data/CSV/tbl_text_mst.csv";
        var textDict = TextParser.Import(textCsvPath);

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length <= 1) return;

        var db = ScriptableObject.CreateInstance<TreasureDatabase>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            var v = line.Split(',');

            if (v.Length < 4)
            {
                Debug.LogWarning($"[TreasureParser] {i}행 데이터 부족 → 스킵");
                continue;
            }

            string id = v[0].Trim('\uFEFF');
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[ChapterParser] ID 누락 → {i}행");
                continue;
            }

            int.TryParse(v[2], out int reward_id);
            int.TryParse(v[3], out int count);

            Enum.TryParse(v[1], true, out TreasureType type);

            db.treasureList.Add(new TreasureData
            {
                treasure_id = id,
                treasureType = type,
                reward_id = reward_id,
                count = count,
            });
        }

        string assetPath = "Assets/_Proj/Data/ScriptableObject/Treasure/TreasureDatabase.asset";
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[TreasureParser] 변환 완료 → {assetPath}");
    }
}
