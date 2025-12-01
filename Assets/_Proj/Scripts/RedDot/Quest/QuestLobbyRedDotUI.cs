using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 퀘스트 버튼 위 빨간 점 제어
/// </summary>
public class QuestLobbyRedDotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button questButton;   // 로비 퀘스트 열기 버튼
    [SerializeField] private GameObject redDot;    // 버튼 위 빨간 점 오브젝트

    private void Reset()
    {
        if (!questButton) questButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        QuestRedDotManager.OnStateChanged += Apply;
        Apply(QuestRedDotManager.Current); // 현재 상태 즉시 반영

        // 혹시 인스펙터에서 꺼져있을 수도 있으니까 한 번 켜줌
        if (questButton) questButton.interactable = true;
    }

    private void OnDisable()
    {
        QuestRedDotManager.OnStateChanged -= Apply;
    }

    private void Apply(QuestRedDotState state)
    {
        // 일일/주간/업적 중 하나라도 알림 있으면 빨간 점 ON
        if (redDot) redDot.SetActive(state.hasAny);

        // ★ 더 이상 버튼 interactable은 건드리지 않음
        // questButton.interactable = state.hasAny;  // ← 이 줄 삭제!
    }
}
