using UnityEngine;
using UnityEngine.UI;

// TODO : 빙판 로직 같은 수상 택시
// 지정된 방향으로 n초간 이동
// 위에 고정X&통과X 물체 태울 수 있음.
// 통과X에 부딪히면 멈춤.(물/물길만 다닐 수 있음)
// 움직이는 동안은 팝업X
// 물길 영향 X

public class Turtle : MonoBehaviour, IDashDirection
{


    [Header("Detects Player")]
    public GameObject btnGroup;
    public Transform playerTrans;
    public float detectRadius;
    public LayerMask playerLayer;

    void Update()
    {
        DetectPlayer();
    }

    // 플레이어 감지
    void DetectPlayer()
    {
        if (!playerTrans || !btnGroup) return;

        float dist = Vector3.Distance(transform.position + Vector3.up * 0.5f, playerTrans.position);
        bool inRange = dist <= detectRadius;

        if (btnGroup.activeSelf != inRange)
        {
            btnGroup.SetActive(inRange);
        }
    }
    void GetDirection(Vector2Int dir)
    {
        
    }

    void IDashDirection.GetDirection(Vector2Int dashDir)
    {
        GetDirection(dashDir);
    }

}
