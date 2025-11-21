using DG.Tweening;
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using static SpeakerData;

public class DialogueParser
{
    public static void Import(string csvPath)
    {
        string textCsvPath = "Assets/_Proj/Data/CSV/tbl_text_mst.csv";
        var textDict = TextParser.Import(textCsvPath);

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length <= 1) return;

        var db = ScriptableObject.CreateInstance<DialogueDatabase>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            //CSV 포맷 그대로 두고, 정규식으로 필드 파싱
            var v = System.Text.RegularExpressions.Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            for (int j = 0; j < v.Length; j++)
                v[j] = v[j].Trim().Trim('"').Trim('\uFEFF');

            if (v.Length < 9)
            {
                Debug.LogWarning($"[DialogueParser] {i}행 데이터 부족 → SplitCount={v.Length} → {line}");
                continue;
            }


            // 첫 번째 값이 비어있을 경우 스킵
            string id = v[0].Trim('\uFEFF');
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[DialogueParser] ID 누락 → {i}행");
                continue;
            }

            int.TryParse(v[1], out int seq);
            float.TryParse(v[6], out float delay);

            Enum.TryParse(v[2], true, out SpeakerPosition speakerPosition);
            Enum.TryParse(v[3], true, out SpeakerId speakerId);
            Enum.TryParse(v[4], true, out EmotionType emotion);
            Enum.TryParse(v[7], true, out SoundType soundType);

            db.dialogueList.Add(new DialogueData
            {
                dialogue_id = id,
                seq = seq,
                speaker_position = speakerPosition,
                speaker_id = speakerId,
                emotion = emotion,
                text = v[5],
                char_delay = delay,
                sound_type = soundType,
                sound_key = v[8]
            });
        }

        string assetPath = "Assets/_Proj/Data/ScriptableObject/Dialogue/DialogueDatabase.asset";
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DialogueParser] 변환 완료 → {assetPath}");
    }
}
