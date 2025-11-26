using UnityEngine;
using UnityEngine.AI;

// NavMeshSurface 상 위치 얻는 확장메서드

public static class SpawnPoint
{
    public static Vector3 GetSpawnPoint(this Vector3 pos, float radius = 1f, float samplePositionMaxDistance = 0.1f)
    {
        Vector3 randomDir = pos + Random.insideUnitSphere * radius;
        randomDir.y = pos.y;
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, samplePositionMaxDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return pos;
    }
}
