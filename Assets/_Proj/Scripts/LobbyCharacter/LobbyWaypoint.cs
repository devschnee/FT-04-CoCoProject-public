using UnityEngine;

public enum WaypointType
{
    Start = 0,
    Normal
}
public class LobbyWaypoint : MonoBehaviour
{
    [SerializeField] WaypointType type;
    public WaypointType Type => type;
}
