using UnityEngine;
// 이건 테스트용 데이터입니다.

[CreateAssetMenu(fileName = "ObjectData", menuName = "LSH_Test/ObjectData")]
public class TestScriptableObject : ScriptableObject
{
    [Header("기본 정보")]
    public int id;
    public string displayName;
    public ObjectType type;

    [Header("Prefab & Settings")]
    public GameObject prefab;
    public bool isCocoDoogy;
    public bool isMaster;

}
