using UnityEngine;

public static class ShopStockHelper
{
    public static int GetOwnedCount(ShopData data)
    {
        if (data == null) return 0;
        if (UserData.Local == null || UserData.Local.inventory == null) return 0;

        var items = UserData.Local.inventory.items;
        if (items == null) return 0;

        string key = data.shop_item.ToString();
        if (!items.TryGetValue(key, out int count)) return 0;

        return Mathf.Max(0, count);
    }

    // shop_stack → shop_stock으로 변경 필요
    public static int GetMaxStock(ShopData data)
    {
        if (data == null) return int.MaxValue;
        if (data.shop_stock <= 0) return int.MaxValue;   // 무제한

        return data.shop_stock;
    }

    public static int GetCurrentStock(ShopData data)
    {
        int max = GetMaxStock(data);
        if (max == int.MaxValue) return int.MaxValue;

        int have = GetOwnedCount(data);
        int remain = max - have; // remain = 남은 재고

        return Mathf.Max(0, remain);
    }

    public static bool CanBuy(ShopData data, int qty)
    {
        if (qty <= 0) return false;

        int remain = GetCurrentStock(data);
        if (remain == int.MaxValue) return true;

        return qty <= remain;
    }
}