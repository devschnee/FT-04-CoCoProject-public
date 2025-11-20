using UnityEngine;

public interface IPushHandler
{
    public GameObject gameObject { get; }
    void StartPushAttempt(Vector2Int dir);
    void StopPushAttempt();
}
