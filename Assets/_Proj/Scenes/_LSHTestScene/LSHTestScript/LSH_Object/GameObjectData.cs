using UnityEngine;

public class GameObjectData : MonoBehaviour
{
    public TestScriptableCharacter Data { get; private set; }

    public void Initialize(TestScriptableCharacter data)
    {
        Data = data;
    }

    public bool IsCocoDoogy => Data.isCocoDoogy;
    public bool IsMaster => Data.isMaster;
}
