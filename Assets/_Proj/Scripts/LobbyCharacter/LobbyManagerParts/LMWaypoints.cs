using System.Collections.Generic;
using UnityEngine;

public class LMWaypoints
{
    private List<LobbyWaypoint> waypoints = new();
    public List<LobbyWaypoint> GetWaypoints()
    {
        waypoints.Clear();
        LobbyWaypoint[] foundWaypoints = Object.FindObjectsByType<LobbyWaypoint>(FindObjectsSortMode.None);
        HashSet<Vector3> usedPositions = new HashSet<Vector3>();
        List<LobbyWaypoint> startWaypoints = new List<LobbyWaypoint>();
        List<LobbyWaypoint> normalWaypoints = new List<LobbyWaypoint>();

        foreach (LobbyWaypoint lw in foundWaypoints)
        {
            if (usedPositions.Add(lw.transform.position))
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
            
        }

        waypoints.AddRange(startWaypoints);
        waypoints.AddRange(normalWaypoints);
        Debug.Log($"waypoints의 총 갯수 {waypoints.Count}");
        return waypoints;
    }
}
