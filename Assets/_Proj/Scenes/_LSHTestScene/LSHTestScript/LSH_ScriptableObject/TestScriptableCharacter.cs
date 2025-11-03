using UnityEngine;
// �̰� �׽�Ʈ�� �������Դϴ�.

[CreateAssetMenu(fileName = "ObjectData", menuName = "LSH_Test/ObjectData")]
public class TestScriptableCharacter : ScriptableObject
{
    [Header("�⺻ ����")]
    public int id;
    public string displayName;
    public CharacterType type;

    [Header("Prefab & Settings")]
    public GameObject prefab;
    public bool isCocoDoogy;
    public bool isMaster;

}
