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
        var treasureposList = stage.GetComponentsInChildren<Treasure>();
        for (int i = 0; i < treasureposList.Length; i++)
        {
            wayPoint[i + 1] = treasureposList[i].transform;
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
}
