using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public static class DecoParser
{
    //static으로 클래스와 함수를 선언, 인스턴스 만들 필요없고, 기능만 제공
    public static void Import(string csvPath)
    {
        string[] lines = File.ReadAllLines(csvPath);
        //매개변수로 받음 csvPath에서 텍스트 파일을 열고 파일의 모든 줄을 읽은 다음 파일을 닫음
        //한줄씩 lines에 저장
        //ex)
        //lines[0] = "id,name,prefabPath,iconPath,category,tag,acquire,stack,description"
        //lines[1] = "1,Flower,Assets/Prefab/Flower.prefab,Assets/Icon/Flower.png,Decoration,Tag1,Shop,99,예쁜 꽃 장식"

        if (lines.Length <= 1) return;

        var db = ScriptableObject.CreateInstance<DecoDatabase>();
        //DecoDatabase ScriptableObject Instance를 생성 -> DecoData를 List로 가지고 있음

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            //현재 System.String 개체에서 선행 공백과 후행 공백을 모두 제거

            if (string.IsNullOrWhiteSpace(line)) continue;
            //null, 공백 체크

            var v = line.Split(',');
            //","를 기준으로 string 분리

            if (v.Length < 8)
            {
                Debug.LogWarning($"[DecoParser] {i}행 데이터 부족 → 스킵");
                continue;
            }

            // 첫 번째 값이 숫자가 아닐 경우 스킵
            if (!int.TryParse(v[0].Trim('\uFEFF'), out int id))
            {
                Debug.LogWarning($"[DecoParser] ID 변환 실패 → {v[0]}");
                continue;
            }

            int.TryParse(v[7], out int stack);

            // Enum 변환 부분
            Enum.TryParse(v[4], true, out Category category);
            Enum.TryParse(v[6], true, out Acquire acquire);

            db.decoList.Add(new DecoData
            {
                id = id,
                name = v[1],
                prefabPath = v[2],
                iconPath = v[3],
                category = category,
                tag = v[5],
                acquire = acquire,
                stack = stack,
                description = v.Length > 8 ? v[8] : ""
                //위에서 분리된 값을 실제 db의 변수들에 넣어줌
            });
        }

        string assetPath = "Assets/_Proj/Data/ScriptableObject/Deco/DecoDatabase.asset";
        //Deco의 ScriptableObject인 DecoDatabase.asset이 생성될 경로
        //실제 폴더가 있어야함
        //다른 DataTable일시 Assets/_Proj/Data/ScriptableObject/여기/이거.asset 이름을 변경하면 됨
        AssetDatabase.CreateAsset(db, assetPath);
        //assetPath 경로에 실제 어셋을 생성
        AssetDatabase.SaveAssets();

        Debug.Log($"[DecoParser] 변환 완료 → {assetPath}");
    }
}