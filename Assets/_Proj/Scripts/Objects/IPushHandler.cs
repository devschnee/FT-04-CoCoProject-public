using UnityEngine;

public interface IPushHandler
{
    void StartPushAttempt(Vector2Int dir);
    void StopPushAttempt();
}
