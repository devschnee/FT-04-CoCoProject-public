using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ShopParser
{
    public static void Import(string csvPath)
    {
        string textCsvPath = "Assets/_Proj/Data/CSV/tbl_text_mst.csv";
        var textDict = TextParser.Import(textCsvPath);

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length <= 1) return;

        var db = ScriptableObject.CreateInstance<ShopDatabase>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            var v = line.Split(',');

            if (v.Length < 7)
            {
                Debug.LogWarning($"[ShopParser] {i}행 데이터 부족 → 스킵");
                continue;
            }

            if (!int.TryParse(v[0].Trim('\uFEFF'), out int id))
            {
                Debug.LogWarning($"[ShopParser] ID 변환 실패 → {v[0]}");
                continue;
            }

            int.TryParse(v[4], out int item);
            int.TryParse(v[6], out int price);

            Enum.TryParse(v[5], true, out ShopType type);

            string rawName = v[1];
            string finalName = TextParser.Resolve(rawName, textDict);

            db.shopDataList.Add(new ShopData
            {
                shop_id = id,
                shop_name = v[1],
                shop_icon = v[2],
                shop_desc = v[3],
                shop_item = item,
                shop_type = type,
                shop_price = price
            });
        }

        string assetPath = "Assets/_Proj/Data/ScriptableObject/Shop/ShopDatabase.asset";
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ShopParser] 변환 완료 → {assetPath}");
    }
}
