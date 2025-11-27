using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ChapterParser
{
    public static void Import(string csvPath)
    {
        string textCsvPath = "Assets/_Proj/Data/CSV/tbl_text_mst.csv";
        var textDict = TextParser.Import(textCsvPath);

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length <= 1) return;

        var db = ScriptableObject.CreateInstance<ChapterDatabase>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            //CSV 포맷 그대로 두고, 정규식으로 필드 파싱
            var v = System.Text.RegularExpressions.Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            for (int j = 0; j < v.Length; j++)
                v[j] = v[j].Trim().Trim('"').Trim('\uFEFF');

            if (v.Length < 6)
            {
                Debug.LogWarning($"[ChapterParser] {i}행 데이터 부족 → 스킵");
                continue;
            }

            // 첫 번째 값이 비어있을 경우 스킵
            string id = v[0].Trim('\uFEFF');
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[ChapterParser] ID 누락 → {i}행");
                continue;
            }

            string rawName = v[1];
            string finalName = TextParser.Resolve(rawName, textDict);

            db.chapterList.Add(new ChapterData
            {
               chapter_id = id,
               chapter_name = finalName,
               chapter_desc = v[2],
               chapter_img = v[3],
               chapter_staglist = v[4].Replace("\"", "").TrimEnd(',').Split(','),
               chapter_bg = v[5],

            });
        }

        string assetPath = "Assets/_Proj/Data/ScriptableObject/Chapter/ChapterDatabase.asset";
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ChapterParser] 변환 완료 → {assetPath}");
    }
}
