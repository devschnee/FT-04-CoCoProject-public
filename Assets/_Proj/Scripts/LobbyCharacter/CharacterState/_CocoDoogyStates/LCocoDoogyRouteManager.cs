using System.Collections.Generic;
using UnityEngine;

public class LCocoDoogyRouteManager
{
    private List<LobbyWaypoint> waypoints = new();
    public List<LobbyWaypoint> moveWaypoints;
    public bool hasComplete = false;

    public LCocoDoogyRouteManager(List<LobbyWaypoint> waypoints)
    {
        this.waypoints = waypoints;
    }

    public void RefreshList()
    {
        moveWaypoints = new List<LobbyWaypoint>(waypoints);

        if (moveWaypoints.Count <= 2) return;

        // moveWaypoints의 [1] ~ [moveWaypoints - 1] 까지 랜덤 순서 돌림
        for (int i = 1; i < moveWaypoints.Count; i++)
        {
            int randomIndex = Random.Range(i, moveWaypoints.Count);
            LobbyWaypoint temp = moveWaypoints[i];
            moveWaypoints[i] = moveWaypoints[randomIndex];
            moveWaypoints[randomIndex] = temp;
        }

        hasComplete = false;
    }

    public Transform GetNextTransform(int currentIndex)
    {
        if (currentIndex >= moveWaypoints.Count)
        {
            hasComplete = true;
            Debug.Log($"코코두기 루틴 끝");
        }
        if (moveWaypoints == null || moveWaypoints.Count == 0) return null;
        if (hasComplete)
        {
            return null;
        }
        Transform next = moveWaypoints[currentIndex].transform;
        return next;
    }

    public void RearragneList(int currentIndex, Vector3 pos)
    {
        if (hasComplete)
        {
            // 뭘 넣을까
        }
        else
        {
            int rearrangeIndex = GetClosestWaypointIndex(pos);

            if (rearrangeIndex < currentIndex)
            {
                moveWaypoints[currentIndex] = moveWaypoints[rearrangeIndex];
                Debug.Log($"덮어씀");
            }
            else if (rearrangeIndex > currentIndex)
            {
                LobbyWaypoint temp = moveWaypoints[currentIndex];
                moveWaypoints[currentIndex] = moveWaypoints[rearrangeIndex];
                moveWaypoints[rearrangeIndex] = temp;
                Debug.Log($"서로 교체");
            }
            else if (rearrangeIndex == currentIndex)
            {
                Debug.Log("잘가고있네.");
            }
        }
    }

    private int GetClosestWaypointIndex(Vector3 pos)
    {
        Vector3 startPos = pos;
        Debug.Log($"startPosCoco : {startPos}");
        float minDistance = float.MaxValue;
        int closestIndex = 0;

        for (int i = 1; i < waypoints.Count; i++)
        {
            float distance = Vector3.Distance(startPos, moveWaypoints[i].transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }
    
}
