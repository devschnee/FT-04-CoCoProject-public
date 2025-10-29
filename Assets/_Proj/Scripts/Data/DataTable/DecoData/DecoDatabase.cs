using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DecoDatabase", menuName = "GameData/DecoDatabase")]
public class DecoDatabase : ScriptableObject
{
    public List<DecoData> decoList = new List<DecoData>();
}
//스크립터블 오브젝트에 DecoData를 list로 보관