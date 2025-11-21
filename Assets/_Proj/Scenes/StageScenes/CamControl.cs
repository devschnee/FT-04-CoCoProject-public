using System.Collections;
using Unity.VisualScripting;
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

    //void Start()
    //{
    //    //offset = transform.position;//(4,9,-5)
    //}

    void Update()
    {
        if (!playerObj) return;

        transform.position = playerObj.transform.position + offset;
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

    public IEnumerator CameraWalking(float duration = 2f)
    {
        if (wayPoint[0] == null || wayPoint[1] == null)
        {
            Debug.LogError("WayPoint null!");
            yield break;
        }

        //Transform[] wayPoints = new Transform[wayPoint.Length];

        
        //// 시작 / 끝 위치 설정
        //Vector3 startPos = wayPoint[0].position + offset;
        //Vector3 endPos = wayPoint[4].position + offset;



        // duration 동안 천천히 이동

        for (int i = 0; i < wayPoint.Length - 1; i++)
        {
            cam.transform.position = wayPoint[i].position + offset;
            float t = 0f;
            while (t < duration)
            {
        
                t += Time.deltaTime;
                float lerpT = Mathf.Clamp01(t / duration);

                cam.transform.position = 
                    Vector3.Lerp(wayPoint[i].position + offset, wayPoint[i + 1].position + offset, lerpT);

                yield return null;
            }
        }
    }
    //카메라워킹 맵 로딩 후 end블록에서 start블록으로 offset 얼마?
    //카메라워킹 끝나면 플레이어한테 가야한다
    //웨이포인트 쓰는데 시작지점은 end블록 끝 지점은 start블록

    // KHJ - TODO : 이동이 끝나고 플레이어를 찾아서 연결해줬으면 이후에 터치가 두 손가락으로 들어왔을 때 캠의 타게팅을 플레이어에 고정시키던 것을 주변을 둘러볼 수 있도록 바꿔야 함. 터치가 손가락 하나이하가 되면 다시 플레이어 타겟팅
}
