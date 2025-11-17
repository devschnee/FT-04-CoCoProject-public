using UnityEngine;
using Game.Inventory;
using System.Collections.Generic;
public class ShopPurchaseService
{
    private readonly GoodsService _goods;
    private readonly Shop_itemDatabase _shopItemDB;
    private readonly int _coinId, _capId, _energyId;

    public ShopPurchaseService(GoodsService goods, Shop_itemDatabase shopItemDB, int coinId, int capId, int energyId)
    {
        _goods = goods;
        _shopItemDB = shopItemDB;
        _coinId = coinId;
        _capId = capId;
        _energyId = energyId;
    }

    public bool TryBuy(ShopData data, int qty)
    {
        if (data == null || qty <= 0) return false;

        int unitPrice = ReadIntField(data, "shop_price", 0);
        int totalCost = Mathf.Max(0, unitPrice * qty);

        int costId = ResolveCostId(data);
        if (costId <= 0) return false;

        // 결제 차감
        if (!_goods.TrySpend(costId, totalCost))
            return false;

        // 보상 지급
        if (data.shop_group == ShopGroup.goods)
        {
            int rewardGoodsId = ResolveRewardGoodsId(data);
            if (rewardGoodsId > 0)
            {
                int perPack = ResolvePackCount(data.shop_item);
                int grant = Mathf.Max(0, perPack * qty);
                if (grant > 0)
                    _goods.Add(rewardGoodsId, grant);
            }
        }
        else
        {
            List<(int, int)> result = ResolvePackage(data);
            
            if (data.shop_group == ShopGroup.home)
            {
                foreach (var item in result)
                InventoryService.I.Add(PlaceableCategory.Home, item.Item1, item.Item2 * qty);

            }
            if (data.shop_group == ShopGroup.deco)
            {
                foreach (var item in result)
                InventoryService.I.Add(PlaceableCategory.Deco, item.Item1, item.Item2 * qty);

            }
            if (data.shop_group == ShopGroup.animal)
                {

                foreach (var item in result)
                InventoryService.I.Add(PlaceableCategory.Animal, item.Item1, item.Item2 * qty);
                }
            // TODO: 코스튬/패키지 등 다른 그룹 지급 로직 필요 시 확장
        }

        return true;
    }

    private int ResolveCostId(ShopData data)
    {
        int pid = ReadIntField(data, "pay_item_id", 0);
        if (pid > 0) return pid;

        if (data.shop_type == ShopType.coin) return _coinId;
        if (data.shop_type == ShopType.cap) return _capId;

        string name = (data.shop_name ?? "").ToLower();
        if (name.Contains("energy")) return _energyId;

        return 0;
    }

    private int ResolveRewardGoodsId(ShopData data)
    {
        var row = FindItemRow(data.shop_item);
        if (row != null)
        {
            //이부분 테스트중.
            int rewardId = ReadIntField(row, "shop_item_Package_id", -1);
            //
            if (rewardId <= 0) rewardId = ReadIntField(row, "reward_goods_id", -1);
            if (rewardId <= 0) rewardId = ReadIntField(row, "goods_id", -1);
            if (rewardId <= 0) rewardId = ReadIntField(row, "target_goods_id", -1);
            if (rewardId > 0) return rewardId;
        }

        string nm = (data.shop_name ?? "").ToLower();
        if (nm.Contains("coin")) return _coinId;
        if (nm.Contains("cap")) return _capId;
        if (nm.Contains("energy")) return _energyId;

        return 0;
    }

    private int ResolvePackCount(int shopItemId)
    {
        var row = FindItemRow(shopItemId);
        if (row == null) return 1;
        int cnt = ReadIntField(row, "shop_item_count", 1);
        return Mathf.Max(1, cnt);
    }

    private Shop_itemData FindItemRow(int shopItemId)
    {
        if (_shopItemDB == null || _shopItemDB.shopItemList == null) return null;
        return _shopItemDB.shopItemList.Find(x => x.shop_item_id == shopItemId);
    }

    private int ReadIntField(object obj, string field, int fallback)
    {
        if (obj == null) return fallback;
        try
        {
            var f = obj.GetType().GetField(field);
            if (f != null && f.FieldType == typeof(int))
                return (int)f.GetValue(obj);
        }
        catch { }
        return fallback;
    }

    private List<(int, int)> ResolvePackage(ShopData shopData)
    {
        List<(int, int)> packageContents = new();
        //탐색 과정: 특정한 shopData가 넘어오면...
        //샵 데이터베이스에 접근할 필요 없음. 왜냐면 이미 필요한 데이터는 넘어온 상태.
        //구매서비스는 이미 샵DB와 샵_아이템DB를 알고 있음.

        //넘어온 데이터에서 shop_item만을 가지고 shop_itemDatabase로 이동.
        //shop_itemDatabase에서 해당 shop_item과 일치하는 shop_item_id를 가진 데이터를 모두 찾아 임시 보관.
        List<Shop_itemData> dataList = _shopItemDB.shopItemList.FindAll(x => x.shop_item_id == shopData.shop_item);
        dataList.ForEach(x => Debug.Log($"찾아온 아이템의 패키지id:{x.shop_item_Package_id},아이템id:{x.shop_item_id},단위수량:{x.shop_item_count}"));
        //패키지 안에 포함된 아이템(id, 갯수)
        dataList.ForEach(x => packageContents.Add(new(x.shop_item_Package_id, x.shop_item_count)));
        return packageContents;
    }
}