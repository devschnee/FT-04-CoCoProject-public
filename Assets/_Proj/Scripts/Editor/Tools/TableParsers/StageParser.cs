using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StageParser
{
    public static void Import(string csvPath)
    {
        string textCsvPath = "Assets/_Proj/Data/CSV/tbl_text_mst.csv";
        var textDict = TextParser.Import(textCsvPath);

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length <= 1) return;

        var db = ScriptableObject.CreateInstance<StageDatabase>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            var v = System.Text.RegularExpressions.Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            if (v.Length < 12)
            {
                Debug.LogWarning($"[StageParser] {i}행 데이터 부족 → 스킵");
                continue;
            }

            string id = v[0].Trim('\uFEFF');
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[ChapterParser] ID 누락 → {i}행");
                continue;
            }


            string rawName = v[1];
            string finalName = TextParser.Resolve(rawName, textDict);

            db.stageDataList.Add(new StageData
            {
                stage_id = id,
                stage_name = finalName,
                stage_img = v[2],
                stage_desc = v[3],
                map_id= v[4],
                start_cutscene = v[5],
                start_talk = v[6],
                end_talk = v[7],
                end_cutscene = v[8],
                treasure_01_id = v[9],
                treasure_02_id = v[10],
                treasure_03_id = v[11],
            });
        }

        string assetPath = "Assets/_Proj/Data/ScriptableObject/Stage/StageDatabase.asset";
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[StageParser] 변환 완료 → {assetPath}");
    }
}
