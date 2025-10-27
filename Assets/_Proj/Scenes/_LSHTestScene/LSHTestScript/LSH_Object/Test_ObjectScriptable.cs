using UnityEngine;

[CreateAssetMenu(fileName = "ObjectData", menuName = "CocoDoogy/TestObject")]
public class ObjectScriptable : ScriptableObject
{
    public GameObject objectPrefab;
    public bool isMainObecjt;
    public ObjectType type;
}
