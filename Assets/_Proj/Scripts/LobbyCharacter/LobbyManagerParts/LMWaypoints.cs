using System.Collections.Generic;
using UnityEngine;

public class LMWaypoints
{
    private List<LobbyWaypoint> waypoints = new();
    public List<LobbyWaypoint> GetWaypoints()
    {
        LobbyWaypoint[] foundWaypoints = Object.FindObjectsByType<LobbyWaypoint>(FindObjectsSortMode.None);
        List<LobbyWaypoint> startWaypoints = new List<LobbyWaypoint>();
        List<LobbyWaypoint> normalWaypoints = new List<LobbyWaypoint>();

        foreach (LobbyWaypoint lw in foundWaypoints)
        {
            if (lw.Type == WaypointType.Start)
            {
                startWaypoints.Add(lw);
            }
            else if (lw.Type == WaypointType.Normal)
            {
                normalWaypoints.Add(lw);
            }
        }

        waypoints.AddRange(startWaypoints);
        waypoints.AddRange(normalWaypoints);
        Debug.Log($"waypoints의 총 갯수 {waypoints.Count}");
        return waypoints;
    }
}
