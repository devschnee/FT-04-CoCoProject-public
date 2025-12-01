using System.Collections;
using UnityEngine;

public class CamControl : MonoBehaviour
{

    public Camera cam;
    public GameObject playerObj;
    public GameObject stage;

    public Transform[] wayPoint;

    private Vector3 startPosition;
    private Vector3 endPosition;
    public Vector3 offset;

    private float waitTime = 0.45f; // 카메라 워킹 지점마다 대기 시간

    [Range(0.5f, 50f), Tooltip("카메라 댐핑 강도, 50 = 댐핑 없음")]
    public float dampingStrength = 0.5f;

    // 카메라 상태 관리를 위한 변수
    private bool isFollowingPlayer = true;
    public bool IsFollowingPlayer => isFollowingPlayer;
    private Vector3 lookAroundOffset; // 주변 둘러보기 모드에서 사용할 임시 오프셋


    // 주변 둘러보기 모드에서 카메라가 플레이어에서 벗어날 수 있는 최대 거리 (월드 좌표계)
    [Header("Look Around")]
    public float maxLookAroundDistance = 5f;
    [Tooltip("주변 둘러보기 모드에서 카메라 이동 속도")]
    [Range(0, 25f)] public float lookAroundSpeed = 25f;



    //void Start()
    //{
    //    //offset = transform.position;//(4,9,-5)
    //}

    void FixedUpdate()
    {
        if (!playerObj) return;

        // KHJ - 드래그 기능 추가. 분기.
        if (isFollowingPlayer)
        {
            // 평상시 : 플레이어 추적
            transform.position = Vector3.Lerp(transform.position, playerObj.transform.position + offset, Time.fixedDeltaTime > .02 ? 50 : (dampingStrength * Time.fixedDeltaTime));
        }
        else
        {
            // 주변 둘러보기 드래그 모드 : lookAroundOffset에 따라 이동
            transform.position = Vector3.Lerp(transform.position, playerObj.transform.position + offset + lookAroundOffset, Time.fixedDeltaTime > .02 ? 50 : (dampingStrength * Time.fixedDeltaTime));
        }
    }

    public void FindWayPoint()
    {
        if (wayPoint == null || wayPoint.Length < 5)
            wayPoint = new Transform[5];

        wayPoint[0] = stage.GetComponentInChildren<EndBlock>().transform;
        wayPoint[4] = stage.GetComponentInChildren<StartBlock>().transform;
        var treasurePosList = stage.GetComponentsInChildren<Treasure>();

        foreach (var treasure in treasurePosList)
        {
            if (treasure.treaureBlockName.Contains("1")) 
            {
                wayPoint[3] = treasure.transform;
            }
            else if (treasure.treaureBlockName.Contains("2"))
            {
                wayPoint[2] = treasure.transform;
            }
            else if (treasure.treaureBlockName.Contains("3"))
            {
                wayPoint[1] = treasure.transform;
            }
        }
    }

    //public IEnumerator CameraWalking(float duration = 2f)
    //{
    //    if (wayPoint[0] == null || wayPoint[1] == null)
    //    {
    //        Debug.LogError("WayPoint null!");
    //        yield break;
    //    }

    //    //Transform[] wayPoints = new Transform[wayPoint.Length];


    //    //// 시작 / 끝 위치 설정
    //    //Vector3 startPos = wayPoint[0].position + offset;
    //    //Vector3 endPos = wayPoint[4].position + offset;



    //    // duration 동안 천천히 이동

    //    for (int i = 0; i < wayPoint.Length - 1; i++)
    //    {
    //        cam.transform.position = wayPoint[i].position + offset;
    //        float t = 0f;
    //        while (t < duration)
    //        {

    //            t += Time.deltaTime;
    //            float lerpT = Mathf.Clamp01(t / duration);

    //            cam.transform.position = 
    //                Vector3.Lerp(wayPoint[i].position + offset, wayPoint[i + 1].position + offset, lerpT);

    //            yield return null;
    //        }
    //    }
    //}

    // 속도가 높을수록 카메라 속도 빨라짐. 속도 일정하게 유지하면서 이동하는 카메라워킹
    // KHJ - NOTE : 카메라의 속도는 이 함수를 호출하는 각 스크립트(StageManager.cs || TestOnly_StageManager.cs)에서 조절
    public IEnumerator CameraWalking(float speed = 6f)
    {
        if (wayPoint[0] == null || wayPoint[1] == null)
        {
            Debug.LogError("WayPoint null!");
            yield break;
        }

        // 각 웨이포인트 구간을 순서대로 이동
        for (int i = 0; i < wayPoint.Length - 1; i++)
        {
            Vector3 startPos = wayPoint[i].position + offset;
            Vector3 endPos = wayPoint[i + 1].position + offset;

            float dist = Vector3.Distance(startPos, endPos);
            float duration = dist / speed;  // 속도 일정하게 유지하도록
            float t = 0f;

            // 카메라를 시작점으로 이동
            cam.transform.position = startPos;

            // 정해진 속도로 이동
            while (t < duration)
            {
                t += Time.deltaTime;
                float lerpT = Mathf.Clamp01(t / duration);
                float easedT = Mathf.SmoothStep(0f, 1f, lerpT);
                cam.transform.position = Vector3.Lerp(startPos, endPos, easedT);

                yield return null;
            }

            cam.transform.position = endPos;

            // endPos에는 대기 없도록
            if (i < wayPoint.Length - 2 && waitTime > 0f)
                yield return new WaitForSeconds(waitTime);
        }
        // Joystick.cs에서 플레이어가 생성되면 SetFollowingPlayer를 호출함.
        //SetFollowingPlayer(true);
    }


    //카메라워킹 맵 로딩 후 end블록에서 start블록으로 offset 얼마?
    //카메라워킹 끝나면 플레이어한테 가야한다
    //웨이포인트 쓰는데 시작지점은 end블록 끝 지점은 start블록

    // 11/21 KHJ - TODO : 이동이 끝나고 플레이어를 찾아서 연결해줬으면 이후에 터치가 두 손가락으로 들어왔을(<-Joystick.cs에서 처리) 때 캠의 타게팅을 플레이어에 고정시키던 것을 주변을 둘러볼 수 있도록 바꿔야 함. 터치가 손가락 하나 이하가 되면 다시 플레이어 타겟팅

    // KHJ - 카메라 추적 상태 설정
    public void SetFollowingPlayer(bool follow)
    {
        isFollowingPlayer = follow;
        if (follow)
        {
            // 플레이어 추적으로 돌아갈 때 오프셋 초기화
            lookAroundOffset = Vector3.zero;
        }
    }


    // KHJ - 두 손가락 드래그 입력에 따라 카메라 이동
    // drageDelta : 터치 위치 변화량
    public void LookAroundUpdate(Vector3 dragDelta)
    {
        if (isFollowingPlayer) return; // 추적 모드일 때는 동작 X


        // 드래그 방향의 반대 방향으로 이동
        // 화면 좌표계(x = 우, y = 상) -> 월드 좌표계(x = 우, z = 전방)
        Vector3 moveDir = new Vector3(-dragDelta.x, 0, -dragDelta.y).normalized;


        // 카메라 방향을 기준으로 이동 방향을 월드 좌표계로 변환(수평 이동만 허용)
        Vector3 camRight = cam.transform.right;
        Vector3 camFwd = cam.transform.forward;
        camFwd.y = 0; // 수평이동만
        camFwd.Normalize();


        Vector3 worldMove = camRight * -dragDelta.x + camFwd * -dragDelta.y;
        worldMove.y = 0; // y축 이동 무시
        worldMove.Normalize();


        // 이동 offset 계산. dragDelta 크기를 이동 속도에 곱해서 이동량을 결정..
        // NOTE : sensitivity는 픽셀값(dragDelta)을 월드 좌표계에 맞추기 위한 임의의 보정값. 테스트하면서 보정 필요.
        float sensitivityFactor = 0.1f;
        Vector3 moveAmount = worldMove * dragDelta.magnitude * lookAroundSpeed * Time.deltaTime * sensitivityFactor;


        lookAroundOffset += moveAmount;


        // 플레이어를 기준으로 최대로 벗어날 수 있는 거리 제한
        lookAroundOffset = Vector3.ClampMagnitude(lookAroundOffset, maxLookAroundDistance);
    }
}