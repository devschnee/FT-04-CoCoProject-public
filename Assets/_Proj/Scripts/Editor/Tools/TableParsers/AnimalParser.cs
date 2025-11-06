using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AnimalParser
{
    public static void Import(string csvPath)
    {
        string textCsvPath = "Assets/_Proj/Data/CSV/tbl_text_mst.csv";
        var textDict = TextParser.Import(textCsvPath);

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length <= 1) return;

        var db = ScriptableObject.CreateInstance<AnimalDatabase>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            var v = System.Text.RegularExpressions.Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            if (v.Length < 8)
            {
                Debug.LogWarning($"[AnimalParser] {i}행 데이터 부족 → 스킵");
                continue;
            }

            if (!int.TryParse(v[0].Trim('\uFEFF'), out int id))
            {
                Debug.LogWarning($"[AnimalParser] ID 변환 실패 → {v[0]}");
                continue;
            }

            Enum.TryParse(v[4], true, out AnimalType category);
            Enum.TryParse(v[5], true, out AnimalTag tag);
            Enum.TryParse(v[6], true, out AnimalAcquire acquire);

            string rawName = v[1];
            string finalName = TextParser.Resolve(rawName, textDict);

            db.animalList.Add(new AnimalData
            {
                animal_id = id,
                animal_name = finalName,
                animal_type = category,
                animal_tag = tag,
                animal_prefab = v[4],
                animal_icon = v[5],
                animal_acquire = acquire,
                animal_desc = v[7]
            });
        }

        string assetPath = "Assets/_Proj/Data/ScriptableObject/Animal/AnimalDatabase.asset";
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AnimalParser] 변환 완료 → {assetPath}");
    }
}
