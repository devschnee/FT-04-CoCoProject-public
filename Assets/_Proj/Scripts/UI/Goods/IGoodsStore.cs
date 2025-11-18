public interface IGoodsStore
{
    int GetAmount(int goodsId);
    void SetAmount(int goodsId, int amount);
}