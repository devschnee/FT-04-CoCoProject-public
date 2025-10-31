using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class Shop_itemParser
{
    public static void Import(string csvPath)
    {
        string textCsvPath = "Assets/_Proj/Data/CSV/tbl_text_mst.csv";
        var textDict = TextParser.Import(textCsvPath);

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length <= 1) return;

        var db = ScriptableObject.CreateInstance<Shop_itemDatabase>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            var v = line.Split(',');

            if (v.Length < 3)
            {
                Debug.LogWarning($"[Shop_itemParser] {i}행 데이터 부족 → 스킵");
                continue;
            }

            if (!int.TryParse(v[0].Trim('\uFEFF'), out int id))
            {
                Debug.LogWarning($"[Shop_itemParser] ID 변환 실패 → {v[0]}");
                continue;
            }

            int.TryParse(v[1], out int package_id);
            int.TryParse(v[2], out int count);

            db.shopItemList.Add(new Shop_itemData
            {
                shop_item_id = id,
                shop_item_Package_id = package_id,
                shop_item_count = count
            });
        }

        string assetPath = "Assets/_Proj/Data/ScriptableObject/Shop_item/Shop_itemDatabase.asset";
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Shop_itemParser] 변환 완료 → {assetPath}");
    }
}
