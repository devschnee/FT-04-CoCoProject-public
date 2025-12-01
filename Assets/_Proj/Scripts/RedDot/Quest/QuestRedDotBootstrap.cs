using System.Collections;
using UnityEngine;

/// <summary>
/// 로비에서 퀘스트 빨간점 초기 계산해주는 부트스트랩
/// </summary>
public class QuestRedDotBootstrap : MonoBehaviour
{
    private IEnumerator Start()
    {
        //  필요한 것들 준비될 때까지 한 프레임씩 기다림
        while (DataManager.Instance == null ||
               DataManager.Instance.Quest == null ||
               DataManager.Instance.Quest.Database == null ||
               UserData.Local == null ||
               UserData.Local.quest == null)
        {
            yield return null;
        }

        // (선택) 퀘스트 리셋 체크
        QuestResetManager.CheckAndReset();

        // ⭐ 여기서 처음 한 번 빨간점 상태 계산
        Debug.Log("[QuestRedDotBootstrap] Initial Recalculate");
        QuestRedDotManager.Recalculate();
    }
}
