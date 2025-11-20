using UnityEngine;

public class PlayerPush : MonoBehaviour, IMoveStrategy
{
    [Header("Push Settings")]
    public float tileSize = 1f;
    public LayerMask pushables;
    public float frontOffset = 0.4f;

    // 현재 밀고 있는 대상 추적을 위한
    private IPushHandler currPushHandler = null;

    //LSH추가
    public bool isPushing => currPushHandler != null;

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

        //NOTE: 강욱 - 1107 : 플레이어의 입장에서 보면, 스피어캐스트(레이캐스트)를 뿌려야 하는 위치는 다음과 같습니다.
        //(플레이어의 로직상 위치(transform.position))보다 위로 0.5, 앞으로(dirN) 1.0지점을 원점으로 하여, 위쪽의 블록과 뒤쪽의 블록까지 감지되도록 해야 합니다.
        //스피어캐스트 말고 레이캐스트 2번으로 끝내면 좋을 것같은데...
        //그 후에 RaycastHit[] hits를 검사하는 과정에서 검출된 IPushHandler가 2개 이상인 경우 플레이어는 이를 밀 수 없도록 처리가 되어야 합니다.
        //그리고 추가 조건으로, 맞은 hit와의 거리가 충분히 가까워야 밀 수 있도록 처리하면 멀리서 미는 일은 없어질 것입니다.


        // 앞 1칸 두께 있게 훑기 (레이어 제한 없이 -> IPushHandler로 필터)
        Vector3 halfExtents = new(.2f, .4f, .2f);
        float maxDist = tileSize * 1.5f;
        float front = Mathf.Max(0.1f, frontOffset);

        Vector3 origin = rb.position + Vector3.up * .8f + dirN * front;

        //SphereCastAll에서 BoxCastAll로 변경합니다.
        var hits = Physics.BoxCastAll(
            origin,
            halfExtents,
            dirN,
            Quaternion.identity,
            maxDist,
            pushables, // 레이어 전부 허용. 최종은 컴포넌트로 필터
            //NOTE: 강욱 - 1107 : 레이어는 pushables만 허용하도록 하고, 대신 아래쪽에서 검출된 거리의 크기에 따라 원격으로 미는 걸 막았습니다.
            QueryTriggerInteraction.Ignore
        );

        //레이어를 전부 허용했기때문에, 뭐든지 다 검출되게 됩니다. 여기서 거리순으로 정렬을 하는데, 원격 푸시 막는 처리는 아래에서 가능할 듯 합니다.
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        IPushHandler next = null;
        foreach (var h in hits)
        {
            if (h.rigidbody && h.rigidbody == rb) continue; // 자기 자신 무시
            if (h.collider.TryGetComponent<IPushHandler>(out var handler))
            {

                float dist = Vector3.Distance(h.collider.transform.position, transform.position);
                Debug.Log($"플레이어푸시: {h.collider.name}은 IPushHandler이므로 검출함. 나와의 거리: {dist}");
                //foreach이기 때문에 일단 검출이 되면 모두 검사함. 만약 새롭게 검출된 핸들러가 기존의 것과 다르다면 할당 해제해줌.
                //아래 코드 설명: next는 무조건 null로 시작함.
                //처음 handler를 만나면 handler로 대입해 줌.
                //하지만 다음 반복에서 또다른 handler가 검출되면 null;이 대입되고 break;함
                //중복으로 검출된 경우에 대한 처리입니다.
                if (next != null) { next = null; break; }
                next = (next == null && Mathf.Approximately(h.distance,0f)) ? handler : null;

                //next = h.distance < .05f ? handler : null;

                //if (handler == null) return (moveDir, Vector3.zero);

                //테스트 2: break;하지 말고 계속 진행해보기. (여러 IPushHandler 객체의 검출 상황)
                //break;
            }
            //내 입력 기준 가까운 오브젝트 순으로 검사하다가 밀 수 있는 오브젝트가 아니면 즉시 무시(원격 푸시 막는 처리) - 테스트 결과 정상.
            else // 이제 거리로 검사하는 대신 pushables 레이어만 열어줬기 때문에... 이 문은 영원히 실행되지 않을 것임.
            {
                Debug.LogWarning($"플레이어푸시: {h.collider.name}은 밀 수 있는 오브젝트가 아님.");
                return (moveDir, Vector3.zero);
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
            if (Vector3.Distance(transform.position, next.gameObject.transform.position) < 1.1f)
            {
                currPushHandler.StartPushAttempt(dir4); // 고정 4방향

                //NOTE: 테스트용. 밀고 있는 경우에는 클램프매그니튜드

                return (Vector3.ClampMagnitude(moveDir, .05f), Vector3.zero);
            }
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
