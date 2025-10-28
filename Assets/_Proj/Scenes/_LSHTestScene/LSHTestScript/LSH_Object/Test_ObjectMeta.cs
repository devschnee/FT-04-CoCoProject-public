using UnityEngine;

public class TestObjectMeta : MonoBehaviour
{
    public TestScriptableObject Data { get; private set; }

    public void Initialize(TestScriptableObject data)
    {
        Data = data;
    }

    public bool IsCocoDoogy => Data.isCocoDoogy;
    public bool IsMaster => Data.isMaster;
}
