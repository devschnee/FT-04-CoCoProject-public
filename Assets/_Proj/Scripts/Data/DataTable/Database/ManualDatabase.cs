using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ManualDatabase", menuName = "GameData/ManualDatabase")]
public class ManualDatabase : ScriptableObject, IEnumerable<ManualData>
{
    public List<ManualData> manualList = new List<ManualData>();
    public IEnumerator<ManualData> GetEnumerator()
    {
        return manualList.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
