using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CodexParser
{
    public static void Import(string csvPath)
    {
        string textCsvPath = "Assets/_Proj/Data/CSV/tbl_text_mst.csv";
        var textDict = TextParser.Import(textCsvPath);

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length <= 1) return;

        var db = ScriptableObject.CreateInstance<CodexDatabase>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            var v = System.Text.RegularExpressions.Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            if (v.Length < 7)
            {
                Debug.LogWarning($"[CodexParser] {i}행 데이터 부족 → 스킵");
                continue;
            }

            for (int j = 0; j < v.Length; j++)
            {
                v[j] = v[j].Trim().Trim('"');
            }

            string id = v[0].Trim('\uFEFF');
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[CodexParser] ID 누락 → {i}행");
                continue;
            }

            Enum.TryParse(v[1], true, out CodexType category);

            int.TryParse(v[2], out int item_id);

            string rawLore = v[3];
            string finalLore = TextParser.Resolve(rawLore, textDict);

            string rawName = v[6];
            string finalName = TextParser.Resolve(rawName, textDict);

            db.codexList.Add(new CodexData
            {
                codex_id = id,
                codex_type = category,
                item_id = item_id,
                codex_lore = finalLore,
                codex_display = v[4],
                codex_icon = v[5],
                codex_name = finalName,
            });
        }

        string assetPath = "Assets/_Proj/Data/ScriptableObject/Codex/CodexDatabase.asset";
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CodexParser] 변환 완료 → {assetPath}");
    }
}
