using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 도감 버튼 위 빨간 점 제어
/// </summary>
public class CodexLobbyRedDotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button codexButton;  // 로비 도감 열기 버튼
    [SerializeField] private GameObject redDot;   // 버튼 위 빨간 점 오브젝트

    private void Reset()
    {
        if (!codexButton) codexButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        CodexRedDotManager.OnStateChanged += Apply;
        Apply(CodexRedDotManager.Current); // 현재 상태 즉시 반영
    }

    private void OnDisable()
    {
        CodexRedDotManager.OnStateChanged -= Apply;
    }

    private void Apply(CodexRedDotState state)
    {
        // 동물/조경/치장/유물/집 중 하나라도 새로 해금된 게 있으면 ON
        if (redDot)
            redDot.SetActive(state.hasAny);
    }
}
