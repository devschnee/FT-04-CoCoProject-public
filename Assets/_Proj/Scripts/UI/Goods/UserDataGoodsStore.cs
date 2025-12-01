using UnityEngine;
using static UserData;

// UserData.Local.wallet을 사용하는 IGoodsStore 구현체
// 에너지/캡/코인을 UserData 기준으로 관리
public class UserDataGoodsStore : IGoodsStore
{
    private readonly int _energyId;
    private readonly int _capId;
    private readonly int _coinId;

    public UserDataGoodsStore(int energyId, int capId, int coinId)
    {
        _energyId = energyId;
        _capId = capId;
        _coinId = coinId;
    }

    private UserData.Goods GetGoods()
    {
        if (UserData.Local == null)
        {
            Debug.LogWarning("[UserDataGoodsStore] UserData.Local 이 null ");
            return null;
        }

        if (UserData.Local.goods == null)
            UserData.Local.goods = new Goods();

        return UserData.Local.goods;
    }

    public int GetAmount(int goodsId)
    {
        var goods = GetGoods();
        if (goods == null) return 0;

        return goods[goodsId];
    }

    public void SetAmount(int goodsId, int amount)
    {
        var goods = GetGoods();
        if (goods == null) return;

        int newValue = Mathf.Max(0, amount);

        goods[goodsId] = newValue;
        //UserData.Local.flag |= UserDataDirtyFlag.Goods;
        goods.Save();
    }
}