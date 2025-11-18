using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoodsManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text capText;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private Image energyIcon;
    [SerializeField] private Image capIcon;
    [SerializeField] private Image coinIcon;
    [SerializeField] private Button energyShopButton;
    [SerializeField] private Button capShopButton;
    [SerializeField] private Button coinShopButton;

    [Header("Goods Ids")]
    [SerializeField] private int energyId = 110001;
    [SerializeField] private int capId = 110002;
    [SerializeField] private int coinId = 110003;

    [Header("Shop Panel")]
    [SerializeField] private ShopPanelController shopPanel;

    private GoodsService goodsService;

    private void Awake()
    {
        // UserData(Local).wallet을 이용하는 GoodsStore
        goodsService = new GoodsService(
    new UserDataGoodsStore(energyId, capId, coinId)
);

        if (energyShopButton)
            energyShopButton.onClick.AddListener(() =>
            {
                if (shopPanel) shopPanel.OpenGoodsEnergy();
            });

        if (capShopButton)
            capShopButton.onClick.AddListener(() =>
            {
                if (shopPanel) shopPanel.OpenGoodsCap();
            });

        if (coinShopButton)
            coinShopButton.onClick.AddListener(() =>
            {
                if (shopPanel) shopPanel.OpenGoodsCoin();
            });
    }

    private void OnEnable()
    {
        RefreshGoodsUI();
    }

    public void RefreshGoodsUI()
    {
        int energy = goodsService.Get(energyId);
        int cap = goodsService.Get(capId);
        int coin = goodsService.Get(coinId);

        if (energyText) energyText.text = energy.ToString(); 
        if (capText) capText.text = cap.ToString();
        if (coinText) coinText.text = coin.ToString();
    }

    public GoodsService GetGoodsService() => goodsService;
}