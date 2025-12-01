using System;
using UnityEngine;

public static class QuestResetManager
{
    public static QuestDatabase QuestDatabase => DataManager.Instance.Quest.Database;

    public static event Action OnQuestReset;

    public static void CheckAndReset()
    {
        if (QuestDatabase == null || UserData.Local == null)
            return;
        bool resetDaily = false;
        bool resetWeekly = false;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long lastDaily = UserData.Local.quest.lastDailyResetAt;
        long lastWeekly = UserData.Local.quest.lastWeeklyResetAt;

        // 한국시간(KST) 계산
        DateTime nowKorean = DateTime.UtcNow.AddHours(9);

        // Daily Reset = 오늘 오전 6:00
        DateTime dailyResetTime = new DateTime(
            nowKorean.Year,
            nowKorean.Month,
            nowKorean.Day,
            6, 0, 0
        );
        long dailyResetTimestamp = new DateTimeOffset(dailyResetTime).ToUnixTimeSeconds();

        if (now >= dailyResetTimestamp && lastDaily < dailyResetTimestamp)
        {
            ResetDaily();
            resetDaily = true;
        }

        // Weekly Reset = 월요일 오전 6시
        DateTime weeklyResetTime = dailyResetTime;
        long weeklyResetTimestamp = dailyResetTimestamp;

        bool isMonday = nowKorean.DayOfWeek == DayOfWeek.Monday;

        if (isMonday && now >= weeklyResetTimestamp && lastWeekly < weeklyResetTimestamp)
        {
            ResetWeekly();
            resetWeekly = true;
        }

        // 저장 + 이벤트 발생
        if (resetDaily || resetWeekly)
        {
            UserData.Local.Save();

            OnQuestReset?.Invoke();
        }
    }
    private static void ResetDaily()
    {
        foreach (var q in QuestDatabase)
        {
            if (q.quest_type == QuestType.daily)
            {
                UserData.Local.quest.progress[q.quest_id] = 0;
                UserData.Local.quest.rewarded.Remove(q.quest_id);
            }
        }

        UserData.Local.quest.lastDailyResetAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        //UserData.Local.flag |= UserDataDirtyFlag.Quest;

        //12.01mj
        QuestRedDotManager.Recalculate();
    }

    private static void ResetWeekly()
    {
        foreach (var q in QuestDatabase)
        {
            if (q.quest_type == QuestType.weekly)
            {
                UserData.Local.quest.progress[q.quest_id] = 0;
                UserData.Local.quest.rewarded.Remove(q.quest_id);
            }
        }

        UserData.Local.quest.lastWeeklyResetAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        //UserData.Local.flag |= UserDataDirtyFlag.Quest;

        //12.01mj
        QuestRedDotManager.Recalculate();
    }
}
