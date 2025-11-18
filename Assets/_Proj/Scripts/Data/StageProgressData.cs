using System;

[Serializable]
public class StageProgressData
{
    public string stageId;
    public bool[] treasureCollected = new bool[3]; // 각 보물별 개별 획득 여부
    public int bestTreasureCount = -1;              // 지금까지 달성한 최대 별 개수

    public int GetCollectedCount()
    {
        int count = 0;
        foreach (bool b in treasureCollected)
            if (b) count++;
        return count;
    }
}