using UnityEngine;

public class PlayerPush : MonoBehaviour, IMoveStrategy
{
    [Header("Push Settings")]
    public float tileSize = 1f;
    public LayerMask pushables;
    public float frontOffset = 0.4f;

    // 현재 밀고 있는 대상 추적을 위한
    private IPushHandler currPushHandler = null;

    public (Vector3, Vector3) Execute(Vector3 moveDir, Rigidbody rb, PlayerMovement player)
    {
        // 입력 없으면 즉시 리셋
        if (moveDir.sqrMagnitude < 1e-6f)
        {
            if (currPushHandler != null)
            {
                currPushHandler.StopPushAttempt();
                currPushHandler = null;
            }
            return (Vector3.zero, Vector3.zero);
        }

        //먼저 4방향으로 고정 -> 조이스틱 미세한 각도 떨림으로 인한 홀드-리셋 방지
        Vector2Int dir4 = player.To4Dir(moveDir); // up/right/left/down 중 하나로 스냅
        Vector3 dirCard = new Vector3(dir4.x, 0f, dir4.y); // 이걸로 캐스트/푸시 둘 다 수행
        Vector3 dirN = dirCard; // 이미 정규화됨 (x/z는 -1,0,1이라서)

        // 앞 1칸 두께 있게 훑기 (레이어 제한 없이 -> IPushHandler로 필터)
        float probeRadius = 0.35f;
        float maxDist = tileSize * 1.1f;
        float front = Mathf.Max(0.1f, frontOffset);

        Vector3 origin = rb.position + Vector3.up * 0.5f + dirN * front;

        var hits = Physics.SphereCastAll(
            origin,
            probeRadius,
            dirN,
            maxDist,
            ~0, // 레이어 전부 허용. 최종은 컴포넌트로 필터
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        IPushHandler next = null;
        foreach (var h in hits)
        {
            if (h.rigidbody && h.rigidbody == rb) continue; // 자기 자신 무시
            if (h.collider.TryGetComponent<IPushHandler>(out var handler))
            {
                next = handler;
                break;
            }
        }

        // 대상 처리 (방향 고정값 dir4로만 시도 -> 흔들려도 누적 유지)
        if (next != null)
        {
            if (!ReferenceEquals(currPushHandler, next))
            {
                currPushHandler?.StopPushAttempt();
                currPushHandler = next;
            }
            currPushHandler.StartPushAttempt(dir4); // 고정 4방향
        }
        else
        {
            if (currPushHandler != null)
            {
                currPushHandler.StopPushAttempt();
                currPushHandler = null;
            }
        }

        // 이동은 원래대로
        return (moveDir, Vector3.zero);
    }
}
