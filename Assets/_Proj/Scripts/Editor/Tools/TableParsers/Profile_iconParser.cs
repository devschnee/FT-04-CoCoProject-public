using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class Profile_iconParser
{
    public static void Import(string csvPath)
    {
        string textCsvPath = "Assets/_Proj/Data/CSV/tbl_text_mst.csv";
        var textDict = TextParser.Import(textCsvPath);

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length <= 1) return;

        var db = ScriptableObject.CreateInstance<Profile_iconDatabase>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            var v = System.Text.RegularExpressions.Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            if (v.Length < 5)
            {
                Debug.LogWarning($"[Profile_iconParser] {i}행 데이터 부족 → 스킵");
                continue;
            }

            if (!int.TryParse(v[0].Trim('\uFEFF'), out int id))
            {
                Debug.LogWarning($"[Profile_iconParser] ID 변환 실패 → {v[0]}");
                continue;
            }

            Enum.TryParse(v[3], true, out IconAcquire acquire);

            string rawName = v[1];
            string finalName = TextParser.Resolve(rawName, textDict);

            db.profileList.Add(new Profile_iconData
            {
                icon_id = id,
                icon_name = finalName,
                icon_image = v[2],
                icon_acquire = acquire,
                icon_desc = v[4]
            });
        }

        string assetPath = "Assets/_Proj/Data/ScriptableObject/Profile_icon/Profile_iconDatabase.asset";
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Profile_iconParser] 변환 완료 → {assetPath}");
    }
}
