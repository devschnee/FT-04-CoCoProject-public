using UnityEngine;
using System.Linq;

public class DashExecutor
{
    private readonly float tileSize;
    private readonly LayerMask blockingLayer;
    private const int MAX_CHAIN_DEPTH = 30; // 재귀 깊이 제한

    // 생성자: 필요한 설정 값을 받아 초기화
    public DashExecutor(float tileSize, LayerMask blockingMask)
    {
        this.tileSize = tileSize;
        blockingLayer = blockingMask;
    }

    // Recursion : currTargetPos 위치에 있는 물체를 dir 방향으로 밀어내는 것이 연쇄적으로 가능한지 검증
    public bool CanChainPush(Vector3 currTargetPos, Vector2Int dir, int depth = 0)
    {
        if (depth > MAX_CHAIN_DEPTH) return false;

        Vector3 moveOffset = new Vector3(dir.x, 0, dir.y) * tileSize;
        Vector3 nextPos = currTargetPos + moveOffset;

        // 다음 칸에 '통과X, 고정O' 객체(벽, 지형)가 있는지 체크
        // IPushHandler를 구현하지 않은 모든 충돌체는 고정 장애물로 간주
        Collider[] staticBlock = Physics.OverlapBox(nextPos + Vector3.up * 0.5f, Vector3.one * 0.4f, Quaternion.identity, blockingLayer)
            .Where(c => !c.TryGetComponent<IPushHandler>(out _)).ToArray();

        if (staticBlock.Length > 0)
        {
            // 밀어내야 할 곳이 고정된 장애물로 막혀있다면 연쇄 밀기 실패
            return false;
        }

        // 다음 칸에 '통과X, 고정X' 물체(IPushHandler)가 있는지 체크
        // Raycast를 사용하여 현재 물체와 다음 물체 사이에 충돌체가 있는지 확인
        if (Physics.Raycast(currTargetPos + Vector3.up * 0.5f, moveOffset, out RaycastHit hit, tileSize + 0.05f, blockingLayer))
        {
            if (hit.distance <= tileSize && hit.collider.TryGetComponent<IPushHandler>(out var handler))
            {
                // 밀어야 할 물체가 있다면, 그 물체의 다음 칸을 재귀적으로 확인
                if (!CanChainPush(hit.collider.transform.position, dir, depth + 1))
                {
                    return false;
                }
            }
            // Raycast에 맞았는데 IPushHandler가 없다면, 이미 1번에서 처리되었거나 
            // Raycast가 고정 장애물을 찾은 경우 밀기 실패
            else
            {
                return false;
            }
        }
        return true;
    }


    // Recursion : 연쇄 검증이 끝난 후, 물체들을 뒤에서부터 1칸씩 이동
    public void ExecuteChainPush(Vector3 currTargetPos, Vector2Int dir)
    {
        Vector3 moveOffset = new Vector3(dir.x, 0, dir.y) * tileSize;
        Vector3 nextTargetPos = currTargetPos + moveOffset;

        // 다음 칸에 밀어야 할 물체가 있는지 확인
        if (Physics.Raycast(currTargetPos + Vector3.up * 0.5f, moveOffset, out RaycastHit hit, tileSize + 0.05f, blockingLayer))
        {
            if (hit.distance <= tileSize && hit.collider.TryGetComponent<IPushHandler>(out var handler))
            {
                // 재귀적으로 다음 물체에게 이동 명령을 먼저 내림
                ExecuteChainPush(hit.collider.transform.position, dir);
            }
        }

        // 현재 물체를 1칸 밀어냄 명령
        // Note: 현재 위치의 물체를 찾기 위해 OverlapBox 사용
        Collider[] currHits = Physics.OverlapBox(currTargetPos + Vector3.up * 0.5f, Vector3.one * 0.4f, Quaternion.identity, blockingLayer);

        foreach (Collider collider in currHits)
        {
            // 현재 위치에서 PushableObjects 컴포넌트를 가진 오브젝트를 찾기
            if (collider.TryGetComponent<PushableObjects>(out var pushable))
            {
                // 홀드 시간 없이 즉시 밀어내기
                pushable.ImmediatePush(dir);

                break; // 타일에 하나에 하나의 PushableObject만 있다고 가정
            }
        }
    }
}