using System;
using UnityEngine;

public struct QuestRedDotState
{
    public bool hasDaily;
    public bool hasWeekly;
    public bool hasAchievement;

    public bool hasAny => hasDaily || hasWeekly || hasAchievement;
}

/// <summary>
/// 퀘스트 빨간점 계산 전담 매니저
/// </summary>
public static class QuestRedDotManager
{
    public static QuestRedDotState Current { get; private set; }
    public static event Action<QuestRedDotState> OnStateChanged;

    public static void Recalculate()
    {
        var newState = CalculateInternal();

        Debug.Log($"[QuestRedDotManager] Recalculate: " +
                  $"daily={newState.hasDaily}, weekly={newState.hasWeekly}, achv={newState.hasAchievement}");

        if (newState.hasDaily == Current.hasDaily &&
            newState.hasWeekly == Current.hasWeekly &&
            newState.hasAchievement == Current.hasAchievement)
        {
            return;
        }

        Current = newState;
        OnStateChanged?.Invoke(Current);
    }



    public static void ForceClear()
    {
        Current = default;
        OnStateChanged?.Invoke(Current);
    }

    private static QuestRedDotState CalculateInternal()
    {
        QuestRedDotState state = default;

        if (DataManager.Instance == null ||
            DataManager.Instance.Quest == null ||
            DataManager.Instance.Quest.Database == null)
        {
            return state;
        }

        if (UserData.Local == null || UserData.Local.quest == null)
        {
            return state;
        }

        var db = DataManager.Instance.Quest.Database;
        var qData = UserData.Local.quest;

        bool hasDaily = false;
        bool hasWeekly = false;
        bool hasAchievement = false;

        foreach (var quest in db.questList)
        {
            // 진행도
            int progress = 0;
            if (!qData.progress.TryGetValue(quest.quest_id, out progress))
                progress = 0;

            bool rewarded = qData.rewarded.Contains(quest.quest_id);

            // 완료 + 미수령만 빨간점 후보
            bool clearAndUnclaimed = (progress >= quest.quest_value) && !rewarded;
            if (!clearAndUnclaimed)
                continue;

            switch (quest.quest_type)
            {
                case QuestType.daily:
                case QuestType.daily_stackrewards:
                    hasDaily = true;
                    break;

                case QuestType.weekly:
                case QuestType.weekly_stackrewards:
                    hasWeekly = true;
                    break;

                case QuestType.achievements:
                    hasAchievement = true;
                    break;
            }
        }

        state.hasDaily = hasDaily;
        state.hasWeekly = hasWeekly;
        state.hasAchievement = hasAchievement;
        return state;
    }
}
