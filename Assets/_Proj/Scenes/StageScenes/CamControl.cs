using System.Collections;
using UnityEngine;

public class CamControl : MonoBehaviour
{

    public Camera cam;
    public GameObject playerObj;
    public GameObject stage;

    [HideInInspector]public Transform[] wayPoint;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 offset;

    void Start()
    {
        offset = transform.position;
    }

    void Update()
    {
        if (!playerObj) return;

        transform.position = playerObj.transform.position + offset;
    }

    public void FindWayPoint()
    {
        if (wayPoint == null || wayPoint.Length < 2)
            wayPoint = new Transform[2];

        wayPoint[0] = stage.GetComponentInChildren<EndBlock>().transform;
        wayPoint[1] = stage.GetComponentInChildren<StartBlock>().transform;
    }

    public IEnumerator CameraWalking(float duration = 2f)
    {
        if (wayPoint[0] == null || wayPoint[1] == null)
        {
            Debug.LogError("WayPoint null!");
            yield break;
        }

        // 시작 / 끝 위치 설정
        Vector3 startPos = wayPoint[0].position + offset;
        Vector3 endPos = wayPoint[1].position + offset;

        float t = 0f;

        cam.transform.position = startPos;

        // duration 동안 천천히 이동
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerpT = Mathf.Clamp01(t / duration);

            cam.transform.position = Vector3.Lerp(startPos, endPos, lerpT);

            yield return null;
        }
    }
    //카메라워킹 맵 로딩 후 end블록에서 start블록으로 offset 얼마?
    //카메라워킹 끝나면 플레이어한테 가야한다
    //웨이포인트 쓰는데 시작지점은 end블록 끝 지점은 start블록
}
