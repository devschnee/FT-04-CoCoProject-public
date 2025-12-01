using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestPanelController : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private QuestDatabase questDatabase;
    [SerializeField] private TreasureDatabase treasureDatabase;

    [Header("Reward Icons")]
    [SerializeField] private Sprite capSprite;
    [SerializeField] private Sprite coinSprite;

    [Header("Tab Buttons")]
    [SerializeField] private Button dailyTabButton;
    [SerializeField] private Button weeklyTabButton;
    [SerializeField] private Button achievementTabButton;

    [Header("Tab Red Dots")]            // 12.01 MJ
    [SerializeField] private GameObject dailyRedDot;        // 일일 탭 빨간 점
    [SerializeField] private GameObject weeklyRedDot;       // 주간 탭 빨간 점
    [SerializeField] private GameObject achievementRedDot;  // 업적 탭 빨간 점

    [Header("Areas")]
    [SerializeField] private GameObject dailyWeeklyArea;
    [SerializeField] private GameObject achievementArea;

    [Header("Slot")]
    [SerializeField] private Transform dailyWeeklyContentParent;
    [SerializeField] private Transform achievementContentParent;
    [SerializeField] private QuestSlotUI slotPrefab;

    [Header("Scrolls")]
    [SerializeField] private ScrollRect dailyWeeklyScrollRect;
    [SerializeField] private ScrollRect achievementScrollRect;

    [Header("Reward UI")]
    [SerializeField] private QuestStackRewardUI stackRewardUI;

    [Header("Popup")]
    [SerializeField] private QuestRewardPopup rewardPopup;

    [Header("Goods Ids")]
    [SerializeField] private int energyItemId = 110001;
    [SerializeField] private int capItemId = 110002;
    [SerializeField] private int coinItemId = 110003;

    [Header("Goods")]
    [SerializeField] private GoodsManager goodsManager;

    [SerializeField] private Button closeButton;

    private QuestType currentType = QuestType.daily;
    private readonly List<QuestSlotUI> spawnedSlots = new List<QuestSlotUI>();


    private void Awake()
    {
        // 1125 승호
        if (dailyTabButton) dailyTabButton.onClick.AddListener(() => { ChangeTab(QuestType.daily); AudioEvents.Raise(UIKey.Normal, 2); });
        if (weeklyTabButton) weeklyTabButton.onClick.AddListener(() => { ChangeTab(QuestType.weekly); AudioEvents.Raise(UIKey.Normal, 2); });
        if (achievementTabButton) achievementTabButton.onClick.AddListener(() => { ChangeTab(QuestType.achievements); AudioEvents.Raise(UIKey.Normal, 2); });
        if (closeButton) closeButton.onClick.AddListener(() => Close());
    }

    private void OnEnable()
    {
        QuestResetManager.OnQuestReset += HandleQuestReset;

        //12.01 MJ
        QuestRedDotManager.OnStateChanged += ApplyRedDots;
        ApplyRedDots(QuestRedDotManager.Current);   // 현재 상태 바로 반영

        currentType = QuestType.daily;
        UpdateTabVisual();

        if (dailyWeeklyArea) dailyWeeklyArea.SetActive(true);
        if (achievementArea) achievementArea.SetActive(false);

        RefreshList();
        RefreshStack();
        ResetScroll();

        //QuestResetManager.CheckAndReset();
        ChangeTab(QuestType.daily);
        UIPanelAnimator.Open(gameObject);
    }

    private void OnDisable()
    {
        QuestResetManager.OnQuestReset -= HandleQuestReset;

        //  12.01 MJ
        QuestRedDotManager.OnStateChanged -= ApplyRedDots;
    }

    // 리셋 발생 시 자동 Refresh
    private void HandleQuestReset()
    {
        RefreshList();
        RefreshStack();
        ResetScroll();
    }

    public void Close()
    {
        AudioEvents.Raise(UIKey.Normal, 1);
        UIPanelAnimator.Close(gameObject);
    }

    private void ChangeTab(QuestType type)
    {
        currentType = type;

        bool isAchievement = (type == QuestType.achievements);

        if (dailyWeeklyArea) dailyWeeklyArea.SetActive(!isAchievement);
        if (achievementArea) achievementArea.SetActive(isAchievement);

        RefreshList();
        RefreshStack();
        UpdateTabVisual();
        ResetScroll();
    }

    private void ResetScroll()
    {
        if (currentType == QuestType.achievements)
        {
            if (achievementScrollRect)
                achievementScrollRect.verticalNormalizedPosition = 1f;
        }
        else
        {
            if (dailyWeeklyScrollRect)
                dailyWeeklyScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private Transform GetCurrentContentParent()
    {
        return currentType == QuestType.achievements ? achievementContentParent : dailyWeeklyContentParent;
    }

    private void RefreshList()
    {
        foreach (var slot in spawnedSlots)
            if (slot) Destroy(slot.gameObject);
        spawnedSlots.Clear();

        if (questDatabase == null || slotPrefab == null)
            return;

        var parent = GetCurrentContentParent();
        var qData = UserData.Local.quest;

        foreach (var quest in questDatabase.questList)
        {
            if (quest.quest_type != currentType)
                continue;

            var slot = Instantiate(slotPrefab, parent);
            spawnedSlots.Add(slot);

            int progress = qData.progress.ContainsKey(quest.quest_id)
                ? qData.progress[quest.quest_id]
                : 0;

            bool rewarded = qData.rewarded.Contains(quest.quest_id);

            slot.Setup(
                quest,
                progress,
                rewarded,
                OnClickReceiveReward
            );

            Sprite icon = GetRewardIcon(quest.quest_reward_item);
            slot.SetRewardIcon(icon);
            slot.SetRewardCount(quest.quest_reward_item_count);
        }
    }

    private void UpdateTabVisual()
    {
        if (dailyTabButton) dailyTabButton.interactable = currentType != QuestType.daily;
        if (weeklyTabButton) weeklyTabButton.interactable = currentType != QuestType.weekly;
        if (achievementTabButton) achievementTabButton.interactable = currentType != QuestType.achievements;
    }
     
    private void OnClickReceiveReward(QuestData quest)
    {
        var qData = UserData.Local.quest;

        // 조건 미충족
        if (!qData.progress.ContainsKey(quest.quest_id) ||
            qData.progress[quest.quest_id] < quest.quest_value)
            return;

        // 이미 수령
        if (qData.rewarded.Contains(quest.quest_id))
            return;

        // 보상 지급
        int rewardId = quest.quest_reward_item;
        int rewardCount = quest.quest_reward_item_count;

        goodsManager.GetGoodsService().Add(rewardId, rewardCount);

        // 보상 체킹
        qData.rewarded.Add(quest.quest_id);

        //UserData.Local.flag |= UserDataDirtyFlag.Quest;
        //UserData.Local.flag |= UserDataDirtyFlag.Wallet;
        UserData.Local.Save();

        //12.01 MJ
        QuestRedDotManager.Recalculate();

        if (rewardPopup)
        {
            Sprite icon = GetRewardIcon(rewardId);
            rewardPopup.Open(
                "보상 획득",
                $"{quest.quest_name}\n{rewardCount}개 획득!",
                icon
            );
        }

        RefreshList();
        RefreshStack();
        //ResetScroll();   
    }
    private void UpdateStackRewardProgress(int cleared)
    {
        var qData = UserData.Local.quest;

        foreach (var stackQuest in questDatabase.questList)
        {
            if (currentType == QuestType.daily &&
                stackQuest.quest_type != QuestType.daily_stackrewards)
                continue;

            if (currentType == QuestType.weekly &&
                stackQuest.quest_type != QuestType.weekly_stackrewards)
                continue;

            if (cleared >= stackQuest.quest_value)
                qData.progress[stackQuest.quest_id] = stackQuest.quest_value;
            else
                qData.progress[stackQuest.quest_id] = 0;
        }
    }
    private void RefreshStack()
    {
        if (!stackRewardUI) return;

        if (currentType == QuestType.achievements)
        {
            stackRewardUI.gameObject.SetActive(false);
            return;
        }

        stackRewardUI.gameObject.SetActive(true);

        stackRewardUI.SetQuestType(currentType);

        int total = 0;
        int cleared = 0;
        var qData = UserData.Local.quest;

        foreach (var quest in questDatabase.questList)
        {
            if (quest.quest_type != currentType)
                continue;

            total++;
            if (qData.progress.ContainsKey(quest.quest_id) &&
                qData.progress[quest.quest_id] >= quest.quest_value)
                cleared++;
        }

        UpdateStackRewardProgress(cleared);
        stackRewardUI.SetData(total, cleared);
    }

    private Sprite GetRewardIcon(int itemId)
    {
        if (itemId == coinItemId) return coinSprite;
        if (itemId == capItemId) return capSprite;

        if (10000 < itemId && itemId < 20000)
        {
            return DataManager.Instance.Deco.GetIcon(itemId);
        }
        if (30000 < itemId && itemId < 40000)
        {
            return DataManager.Instance.Animal.GetIcon(itemId);
        }

        if (treasureDatabase != null)
        {
            var data = treasureDatabase.treasureList.Find(t => t.reward_id == itemId);
            if (data != null && !string.IsNullOrEmpty(data.view_codex_id))
            {
                Sprite sp = Resources.Load<Sprite>(data.view_codex_id);
                if (sp != null) return sp;

                sp = Resources.Load<Sprite>("Icons/Codex/" + data.view_codex_id);
                if (sp != null) return sp;
            }
        }
        return null;
    }
    /// <summary>
    /// QuestRedDotManager에서 계산한 상태를
    /// 탭 빨간 점 3개에 반영
    /// </summary>
    private void ApplyRedDots(QuestRedDotState state)
    {
        if (dailyRedDot) dailyRedDot.SetActive(state.hasDaily);
        if (weeklyRedDot) weeklyRedDot.SetActive(state.hasWeekly);
        if (achievementRedDot) achievementRedDot.SetActive(state.hasAchievement);
    }
}
