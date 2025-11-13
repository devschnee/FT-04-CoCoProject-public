using System.Collections.Generic;
using UnityEngine;

public class LCocoDoogyRouteManager
{
    private int currentIndex = 0;
    public bool hasComplete = false;
    private List<LobbyWaypoint> waypoints = new();
    public List<LobbyWaypoint> moveWaypoints = new();

    public LCocoDoogyRouteManager(List<LobbyWaypoint> waypoints)
    {
        this.waypoints = waypoints;
    }

    public void RefreshList()
    {
        moveWaypoints = waypoints;
        if (moveWaypoints.Count <= 2) return;

        // moveWaypoints의 [1] ~ [moveWaypoints - 1] 까지 랜덤 순서 돌림
        for (int i = 1; i < moveWaypoints.Count; i++)
        {
            int randomIndex = Random.Range(i, moveWaypoints.Count);
            LobbyWaypoint temp = moveWaypoints[i];
            moveWaypoints[i] = moveWaypoints[randomIndex];
            moveWaypoints[randomIndex] = temp;
        }

        currentIndex = 0;
        hasComplete = false;
    }

    public Transform GetNextTransform()
    {
        if (currentIndex >= moveWaypoints.Count)
        {
            hasComplete = true;
        }
        if (moveWaypoints == null || moveWaypoints.Count == 0) return null;
        if (hasComplete)
        {
            currentIndex = 0;
            return null;
        }
        Transform next = moveWaypoints[currentIndex].transform;
        currentIndex++;
        return next;
    }
    
}
