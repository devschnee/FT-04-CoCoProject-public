using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class QuestParser
{
    public static void Import(string csvPath)
    {
        string textCsvPath = "Assets/_Proj/Data/CSV/tbl_text_mst.csv";
        var textDict = TextParser.Import(textCsvPath);

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length <= 1) return;

        var db = ScriptableObject.CreateInstance<QuestDatabase>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            var v = line.Split(',');

            if (v.Length < 9)
            {
                Debug.LogWarning($"[QuestParser] {i}행 데이터 부족 → 스킵");
                continue;
            }

            if (!int.TryParse(v[0].Trim('\uFEFF'), out int id))
            {
                Debug.LogWarning($"[QuestParser] ID 변환 실패 → {v[0]}");
                continue;
            }

            int.TryParse(v[5], out int value);
            int.TryParse(v[6], out int reward_item);
            int.TryParse(v[7], out int reward_count);

            Enum.TryParse(v[2], true, out QuestType category);
            Enum.TryParse(v[3], true, out QuestObject obj);

            string rawName = v[1];
            string finalName = TextParser.Resolve(rawName, textDict);

            db.questList.Add(new QuestData
            {
                quest_id = id,
                quest_name = finalName,
                quest_type = category,
                quest_object = obj,
                quest_obj_desc = v[4],
                quest_value = value,
                quest_reward_item = reward_item,
                quest_reward_item_count = reward_count,
                quest_desc = v[8]
            });
        }

        string assetPath = "Assets/_Proj/Data/ScriptableObject/Quest/QuestDatabase.asset";
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AnimalParser] 변환 완료 → {assetPath}");
    }
}
