using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestStackRewardUI : MonoBehaviour
{
    [Header("Progress")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text[] stageCountTexts;

    [Header("Reward Boxes")]
    [SerializeField] private Button[] stageRewardButtons;
    [SerializeField] private Image[] stageRewardIcons;

    [Header("Progress Markers")]
    [SerializeField] private Image[] stageMarkers;
    [SerializeField] private Sprite markEmptySprite;
    [SerializeField] private Sprite markFilledSprite;

    [Header("Chest Sprites")]
    [SerializeField] private Sprite[] closedChestSprites;
    [SerializeField] private Sprite[] openedChestSprites;

    [Header("Popup")]
    [SerializeField] private QuestRewardPopup rewardPopup;

    [Header("Managers")]
    [SerializeField] private GoodsManager goodsManager;

    [Header("Basic Goods Icons")]
    [SerializeField] private Sprite coinSprite;
    [SerializeField] private Sprite capSprite;
    [SerializeField] private Sprite energySprite;

    private QuestDatabase questDatabase;
    private QuestType currentType;

    private QuestData[] stackQuests;
    private int clearedCount;
    private int totalCount;


    private void Awake()
    {
        questDatabase = DataManager.Instance.Quest.Database;

        // 버튼
        for (int i = 0; i < stageRewardButtons.Length; i++)
        {
            int idx = i;
            stageRewardButtons[i].onClick.RemoveAllListeners();
            stageRewardButtons[i].onClick.AddListener(() => OnClickReceiveReward(idx));
        }
    }

    public void SetQuestType(QuestType type)
    {
        currentType = type;

        // questDatabase가 null이면 다시 시도
        if (questDatabase == null)
        {
            questDatabase = DataManager.Instance?.Quest?.Database;

            if (questDatabase == null)
            {
                stackQuests = new QuestData[0];
                return;
            }
        }

        // questList null 체크
        if (questDatabase.questList == null)
        {
            stackQuests = new QuestData[0];
            return;
        }

        if (currentType == QuestType.daily)
        {
            stackQuests = questDatabase.questList
                .Where(q => q.quest_type == QuestType.daily_stackrewards)
                .OrderBy(q => q.quest_value)
                .ToArray();
        }
        else
        {
            stackQuests = questDatabase.questList
                .Where(q => q.quest_type == QuestType.weekly_stackrewards)
                .OrderBy(q => q.quest_value)
                .ToArray();
        }
    }



    public void SetData(int total, int cleared)
    {
        totalCount = Mathf.Max(1, total);
        clearedCount = Mathf.Clamp(cleared, 0, totalCount);

        progressSlider.maxValue = totalCount;
        progressSlider.value = clearedCount;

        for (int i = 0; i < stageCountTexts.Length && i < stackQuests.Length; i++)
        {
            stageCountTexts[i].text = stackQuests[i].quest_value.ToString();
        }

        UpdateMarkers();
        RefreshChestStates();
    }


    private void UpdateMarkers()
    {
        for (int i = 0; i < stageMarkers.Length && i < stackQuests.Length; i++)
        {
            bool reached = clearedCount >= stackQuests[i].quest_value;
            stageMarkers[i].sprite = reached ? markFilledSprite : markEmptySprite;
        }
    }


    private bool IsRewardReceived(int index)
    {
        int questId = stackQuests[index].quest_id;
        return UserData.Local.quest.rewarded.Contains(questId);
    }


    private void RefreshChestStates()
    {
        for (int i = 0; i < stageRewardButtons.Length && i < stackQuests.Length; i++)
        {
            QuestData q = stackQuests[i];

            bool canReceive = clearedCount >= q.quest_value;
            bool received = IsRewardReceived(i);

            stageRewardButtons[i].interactable = canReceive && !received;
            stageRewardIcons[i].sprite = received ? openedChestSprites[i] : closedChestSprites[i];
        }
    }

    // questpanelcontroller에서 긁어옴
    private void OnClickReceiveReward(int index)
    {
        QuestData quest = stackQuests[index];
        var qData = UserData.Local.quest;

        if (!qData.progress.ContainsKey(quest.quest_id) ||
            qData.progress[quest.quest_id] < quest.quest_value)
            return;

        if (qData.rewarded.Contains(quest.quest_id))
            return;

        int rewardId = quest.quest_reward_item;
        int rewardCount = quest.quest_reward_item_count;

        goodsManager.GetGoodsService().Add(rewardId, rewardCount);

        qData.rewarded.Add(quest.quest_id);

        //UserData.Local.flag |= UserDataDirtyFlag.Quest;
        //UserData.Local.flag |= UserDataDirtyFlag.Wallet;
        UserData.Local.Save();

        //12.01mj
        QuestRedDotManager.Recalculate();

        RefreshChestStates();
        UpdateMarkers();

        // ⭐ 팝업 보상: 설명 + 아이콘
        if (rewardPopup)
        {
            Sprite icon = GetRewardIcon(rewardId);

            rewardPopup.Open(
                "보상 획득",
                $"{rewardCount}개 획득!",
                icon
            );
        }
    }


    private Sprite GetRewardIcon(int itemId)
    {
        if (itemId == 110003) return coinSprite;
        if (itemId == 110002) return capSprite;
        if (itemId == 110001) return energySprite;

        if (10000 < itemId && itemId < 20000)
            return DataManager.Instance.Deco.GetIcon(itemId);

        if (30000 < itemId && itemId < 40000)
            return DataManager.Instance.Animal.GetIcon(itemId);

        return null;
    }
}
