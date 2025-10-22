using System.Collections;
using UnityEngine;

public class PushableObjects : MonoBehaviour
{
    public float moveTime = 0.12f;
    public float tileSize = 1f;
    [Tooltip("이동 막는 물체")]
    public LayerMask blockingMask;
    [Tooltip("땅 판정")]
    public LayerMask groundMask;
    [Tooltip("경사로")]
    public LayerMask slopeMask;

    private bool isMoving = false;
    private bool isHoling = false;
    public float requiredHoldtime = 0.5f;
    private float currHold = 0f;
    private Vector2Int holdDir;

    public bool allowFall = true;
    public bool allowSlope = false;

    void Update()
    {
        if (!isHoling || isMoving) return;
        currHold += Time.deltaTime;
        if(currHold >= requiredHoldtime)
        {
            TryPush(holdDir);
            currHold = 0f;
            isHoling = false;
        }
    }

    public bool TryPush(Vector2Int dir)
    {
        if (isMoving) return false;

        Vector3 offset = new Vector3(dir.x, 0f, dir.y) * tileSize;
        Vector3 target = transform.position + offset;

        // 경사로
        if (allowSlope && Physics.Raycast(target + Vector3.up * 0.5f, Vector3.down, 1f, slopeMask))
        {
            Vector3 up = target + Vector3.up * tileSize;
            if (!Physics.CheckBox(up + Vector3.up * 0.5f, Vector3.one * 0.4f, Quaternion.identity, blockingMask) &&
                Physics.Raycast(up + Vector3.up * 0.1f, Vector3.down, 1.5f, groundMask))
            {
                StartCoroutine(MoveTo(up));
                return true;
            }
        }

        // 낭떠러지 처리 (여러 칸까지 반복 낙하)
        if (allowFall)
        {
            while (!Physics.Raycast(target + Vector3.up * 0.1f, Vector3.down, 1.5f, groundMask))
            {
                target += Vector3.down * tileSize;
                if (target.y < -100f) return false; // 무한 추락 방지
            }
        }

        // 목적지에 뭔가 있으면 못 감
        if (Physics.CheckBox(target + Vector3.up * 0.5f, Vector3.one * 0.4f, Quaternion.identity, blockingMask))
            return false;

        StartCoroutine(MoveTo(target));
        return true;
    }

    IEnumerator MoveTo(Vector3 target)
    {
        isMoving = true;
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < moveTime)
        {
            transform.position = Vector3.Lerp(start, target, elapsed / moveTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
        isMoving = false;
    }

    public void StartPushAttempt(Vector2Int dir)
    {
        if(isMoving) return;
        if(isHoling && dir != holdDir)
        {
            currHold = 0f;
        }

        holdDir = dir;
        isHoling = true;
    }

    public void StopPushAttempt()
    {
        isHoling = false;
        currHold = 0f;
    }
}
