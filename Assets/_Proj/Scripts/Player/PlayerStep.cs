using UnityEngine;

public class PlayerStep : MonoBehaviour, IMoveStrategy
{
    [Header("Step Settings")]
    public LayerMask blocking;
    public bool enableStepAssist = true;
    public float stepHeight = 0.3f;
    public float stepCheckDistance = 0.35f;

    public (Vector3, Vector3) Execute(Vector3 moveDir, Rigidbody rb, PlayerMovement player)
    {
        if (!enableStepAssist || stepHeight <= 0f || moveDir.sqrMagnitude < 0.0001f)
            return (moveDir, Vector3.zero);

        Vector3 stepOffset = Vector3.zero;

        // 낮은 턱(벽)에 막혔는지 전방 레이로 체크
        Vector3 originLow = rb.position + Vector3.up * 0.05f;
        Vector3 originHigh = rb.position + Vector3.up * (stepHeight + 0.05f);

        if (Physics.Raycast(originLow, moveDir, out RaycastHit hitLow, stepCheckDistance, blocking))
        {
            // 같은 지점에서 높은 위치로 다시 쏴서 위가 비어있으면 올라설 수 있음
            bool upperBlocked = Physics.Raycast(originHigh, moveDir, stepCheckDistance, blocking);
            if (!upperBlocked)
            {
                // 살짝 위로 올려줌
                stepOffset = Vector3.up * (Mathf.Clamp(stepHeight, 0.01f, 0.5f));
            }
        }
        // 보정된 이동 방향은 없으므로 moveDir를 그대로 반환. stepOffset만 추가.
        return (moveDir, stepOffset);
    }
}
