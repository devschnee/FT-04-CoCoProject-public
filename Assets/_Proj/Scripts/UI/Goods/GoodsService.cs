using UnityEngine;

public class GoodsService
{
    private readonly IGoodsStore store;

    public GoodsService(IGoodsStore store)
    {
        this.store = store;
    }

    public int Get(int goodsId)
    {
        return store.GetAmount(goodsId);
    }

    public void Add(int goodsId, int amount)
    {
        int cur = store.GetAmount(goodsId);
        store.SetAmount(goodsId, cur + amount);
    }

    public bool TrySpend(int goodsId, int amount)
    {
        int cur = store.GetAmount(goodsId);
        if (cur < amount)
            return false;

        store.SetAmount(goodsId, cur - amount);
        return true;
    }
}