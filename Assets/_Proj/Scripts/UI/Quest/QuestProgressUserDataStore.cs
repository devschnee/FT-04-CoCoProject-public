using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UserData를 기반으로 퀘스트 진행도와 보상 수령 상태를 관리하는 스토어 설계용 클래스
///
/// 현재 UserData.cs 안에 퀘스트 관련 필드가 없기 때문에
/// UserData에 접근하는 코드는 모두 주석 처리함
/// 지금은 항상 기본값(진행도 0, 보상 미수령)만 반환하는 더미 구현으로 동작
///
/// 나중에 UserData에 퀘스트용 필드를 추가하면
/// 아래 주석 부분을 풀어서 실제 연동하면 됨

/// public class QuestProgressData
/// {
///     public Dictionary<int, int> progress = new Dictionary<int, int>(); // questId -> currentValue
///     public HashSet<int> rewarded = new HashSet<int>();                // 보상 수령 완료한 questId
/// }
///
/// public partial class UserData
/// {
///     public QuestProgressData quest;
/// }
/// </summary>
public class QuestProgressUserDataStore : IQuestProgressStore
{
    public int GetProgress(int questId)
    {
        // 나중에 UserData에 quest 필드가 생기면 아래 코드를 사용하면 됨

        // if (UserData.Local != null &&
        //     UserData.Local.quest != null &&
        //     UserData.Local.quest.progress != null)
        // {
        //     if (UserData.Local.quest.progress.TryGetValue(questId, out int value))
        //         return value;
        // }
        //
        // return 0;

        // 현재는 UserData에 퀘스트 정보가 없으므로 항상 0을 반환
        return 0;
    }

    public void SetProgress(int questId, int value)
    {
        // 나중에 UserData에 quest 필드가 생기면 아래 코드를 사용하면 됨

        // if (UserData.Local == null)
        //     return;
        //
        // if (UserData.Local.quest == null)
        //     UserData.Local.quest = new QuestProgressData();
        //
        // if (UserData.Local.quest.progress == null)
        //     UserData.Local.quest.progress = new Dictionary<int, int>();
        //
        // UserData.Local.quest.progress[questId] = value;
        //
        // 필요한 경우 여기에서 UserData.Local.Save() 같은 저장 메서드를 호출

        // 현재는 아무 동작도 하지 않는 더미 구현
        Debug.Log("[QuestProgressUserDataStore] SetProgress 호출됨. 현재는 더미 구현입니다. questId=" + questId + ", value=" + value);
    }

    public bool IsRewardReceived(int questId)
    {
        // 나중에 UserData에 quest 필드가 생기면 아래 코드를 사용하면 됨

        // if (UserData.Local != null &&
        //     UserData.Local.quest != null &&
        //     UserData.Local.quest.rewarded != null)
        // {
        //     return UserData.Local.quest.rewarded.Contains(questId);
        // }
        //
        // return false;

        // 현재는 항상 "보상 받지 않음" 상태로 취급
        return false;
    }

    public void SetRewardReceived(int questId)
    {
        // 나중에 UserData에 quest 필드가 생기면 아래 코드를 사용하면 됨

        // if (UserData.Local == null)
        //     return;
        //
        // if (UserData.Local.quest == null)
        //     UserData.Local.quest = new QuestProgressData();
        //
        // if (UserData.Local.quest.rewarded == null)
        //     UserData.Local.quest.rewarded = new HashSet<int>();
        //
        // UserData.Local.quest.rewarded.Add(questId);
        //
        // 필요한 경우 여기에서 UserData.Local.Save() 같은 저장 메서드를 호출

        // 현재는 아무 동작도 하지 않는 더미 구현
        Debug.Log("[QuestProgressUserDataStore] SetRewardReceived 호출됨. 현재는 더미 구현입니다. questId=" + questId);
    }
    public Dictionary<int, int> LoadAllProgress()
    {
        // 나중에 UserData에 quest.progress 딕셔너리가 생기면 아래 코드를 사용하면 됨

        // if (UserData.Local != null &&
        //     UserData.Local.quest != null &&
        //     UserData.Local.quest.progress != null)
        // {
        //     return new Dictionary<int, int>(UserData.Local.quest.progress);
        // }

        // 지금은 더미로 빈 딕셔너리를 반환
        return new Dictionary<int, int>();
    }

    public HashSet<int> LoadAllReceived()
    {
        // 나중에 UserData에 quest.rewarded 집합이 생기면 아래 코드를 사용하면 됨

        // if (UserData.Local != null &&
        //     UserData.Local.quest != null &&
        //     UserData.Local.quest.rewarded != null)
        // {
        //     return new HashSet<int>(UserData.Local.quest.rewarded);
        // }

        // 지금은 더미로 빈 집합을 반환
        return new HashSet<int>();
    }
}