using System.Collections.Generic;
using UnityEngine;
using System.IO;
public static class TextParser
{
    // CSV를 읽어 Dictionary<string, string> 형태로 반환
    public static Dictionary<string, string> Import(string csvPath)
    {
        var dict = new Dictionary<string, string>();
        string[] lines = File.ReadAllLines(csvPath);
        //csvPath의 경로에 있는 CSV의 모든 줄을 읽고 lines에 저장

        if (lines.Length <= 1)
        {
            Debug.LogWarning("[TextParser] CSV에 데이터가 없습니다.");
            return dict;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // 따옴표 포함 방지
            string[] v = line.Split(',');

            // 최소 두 컬럼: kr_text_id, kr_text
            if (v.Length < 2)
                continue;

            string key = v[0].Trim().Trim('"').Trim('[', ']');
            string value = v[1].Trim().Trim('"');
            //첫 번째 컬럼을 key, 두 번째 컬럼을 value로 사용. 양쪽 공백과 큰따옴표(") 제거.

            if (!dict.ContainsKey(key))
                dict[key] = value;
            else
                Debug.LogWarning($"[TextParser] 중복된 키 발견 → {key}");
        }

        Debug.Log($"[TextParser] 총 {dict.Count}개의 문자열 로드 완료");
        return dict;
    }

    // [key] 형식 문자열을 실제 텍스트로 변환하는 헬퍼 함수
    public static string Resolve(string raw, Dictionary<string, string> dict)
    {
        if (string.IsNullOrEmpty(raw))
            return raw;

        // 전체가 [key] 인 경우 → 기존 방식 유지
        if (raw.StartsWith("[") && raw.EndsWith("]") && raw.IndexOf(']') == raw.LastIndexOf(']'))
        {
            //문자열의 시작과 끝이 "[", "]" 대괄호 인지 체크
            string key = raw.Trim('[', ']');
            if (dict.TryGetValue(key, out string text))
                return text;
            return raw;
        }

        // 문장 안에 [key] 포함된 경우 처리
        foreach (var kvp in dict)
        {
            string pattern = $"[{kvp.Key}]";
            if (raw.Contains(pattern))
            {
                raw = raw.Replace(pattern, kvp.Value);
                //대괄호 유지 하며 단어로 치환
                //raw = raw.Replace(pattern, $"[{kvp.Value}]");
            }
        }
        raw = raw.Replace("\\n", "\n");

        return raw; // 일반 문자열 그대로 반환
    }
}