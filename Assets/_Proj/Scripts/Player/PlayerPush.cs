using UnityEngine;

public class PlayerPushTrigger : MonoBehaviour, IMoveStrategy
{
    [Header("Push Settings")]
    public float tileSize = 1f;
    public LayerMask pushables;
    public float frontOffset = 0.4f;

    // DIP. 현재 밀고 있는 대상 추적 위함.
    private IPushHandler currPushHandler = null;

    public (Vector3, Vector3) Execute(Vector3 moveDir, Rigidbody rb, PlayerMovement player)
    {
        // 입력 없으면 현재 시도 중인 푸시를 중단, 핸들러 해제
        if (moveDir.sqrMagnitude < 0.0001f)
        {
            // 핸들러 중단 후 참조 해제
            currPushHandler?.StopPushAttempt();
            currPushHandler = null;
            return (moveDir, Vector3.zero);
        }

        // 방향 벡터 계산 후, 이동 시도한 위치 앞에 푸시 가능한 오브젝트가 존재하면
        Ray ray = new Ray(rb.position + Vector3.up * 0.5f, moveDir);

        if (Physics.Raycast(ray, out RaycastHit hit, 1.1f, pushables))
        {
            if (hit.collider.TryGetComponent<IPushHandler>(out var handler))
            {
                // 감지된 핸들러를 현재 핸들러로 설정 (밀기 대상 추적 시작)
                currPushHandler = handler;

                Vector2Int dir = player.To4Dir(moveDir);
                currPushHandler.StartPushAttempt(dir);
            }
        }
        else
        {
            // 레이캐스트 실패 시 (밀기 대상에서 벗어남), 핸들러 참조 해제
            if (currPushHandler != null)
            {
                currPushHandler.StopPushAttempt();
                currPushHandler = null;
            }
        }
        return (moveDir, Vector3.zero);
    }
}
