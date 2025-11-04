using System.Collections;
using UnityEngine;

// 코코두기가 안드로이드와 특정 범위 내에 있거나 랜덤 값으로
// 뽑힌 동물들에게 다가갔을때 로비 매니저에게 이벤트 호출, 로비매니저는
// 상호작용 실행.

public class CocoDoogyBehaviour : BaseLobbyCharacterBehaviour
{
    private int currentWaypointIndex; // 웨이포인트 이동 인덱스
    private int animalInteractCount = 0; // 동물 친구들 상호작용 판단
    private int masterInteractCount = 0; // 깡통 상호작용 판단

    protected override void Awake()
    {
        base.Awake();
        agent.avoidancePriority = 99;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        currentWaypointIndex = 0;
        animalInteractCount = 0;
        masterInteractCount = 0;

    }

    // 가자 멍뭉아
    private IEnumerator LetsGoCoco(Transform[] waypoints)
    {
        Debug.Log($"시작 인덱스 값 : {currentWaypointIndex}");
        isMoving = true;
        while (isMoving)
        {
            if (currentWaypointIndex == waypoints.Length - 1)
            {
                //charAgent.MoveToTransPoint(waypoints[currentWaypointIndex]);
                charAgent.CocoMoveToRandomTransPoint(waypoints[currentWaypointIndex]);
                currentWaypointIndex = 0;
            }
            if (currentWaypointIndex == 0)
            {
                animalInteractCount = 0;
                charAgent.MoveToLastPoint(waypoints[currentWaypointIndex]);
                yield return waitFS;
            }
            currentWaypointIndex++;
            Debug.Log($"현재 인덱스 값 : {currentWaypointIndex}");
            //charAgent.MoveToTransPoint(waypoints[currentWaypointIndex]);
            charAgent.CocoMoveToRandomTransPoint(waypoints[currentWaypointIndex]);
            yield return waitU;

        }
    }

    // 드래그나 편집모드 성공적으로 끝날 시 본인 위치 기준 가장 가까운 포인트 찾기 
    private int GetClosestWaypointIndex()
    {
        float minDistance = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < InLobbyManager.Instance.cocoWaypoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, InLobbyManager.Instance.cocoWaypoints[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    // 인터페이스 영역
    public override void OnCocoAnimalEmotion()
    {
        if (animalInteractCount == 0)
        {
            // 상호작용 추가
            animalInteractCount = 1;
        }
    }

    public override void OnCocoMasterEmotion()
    {
        if (masterInteractCount == 0)
        {
            // 상호작용 추가
            masterInteractCount = 1;
        }
    }

    public override void OnLobbyEndDrag(Vector3 position)
    {
        base.OnLobbyEndDrag(position);
        currentWaypointIndex = GetClosestWaypointIndex();
        StartCoroutine(LetsGoCoco(InLobbyManager.Instance.cocoWaypoints));
    }
    public override void OnLobbyInteract()
    {
        base.OnLobbyInteract();
        
        AudioEvents.Raise(SFXKey.CocodoogyFootstep, pooled: true, pos: transform.position);
    }
    public override void InNormal()
    {
        base.InNormal();
        currentWaypointIndex = GetClosestWaypointIndex();
        StartCoroutine(LetsGoCoco(InLobbyManager.Instance.cocoWaypoints));
    }
    public override void InUpdate()
    {

    }
    public override void StartScene()
    {
        StartCoroutine(LetsGoCoco(InLobbyManager.Instance.cocoWaypoints));
    }
    public override void ExitScene()
    {

    }
}
