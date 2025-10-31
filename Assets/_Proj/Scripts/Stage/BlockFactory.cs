using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;





public class BlockFactory : MonoBehaviour
{
    //전역으로 접근할 필요 없음. StageManager가 이 클래스의 객체를 하나만 알고 있으면 됨.

    [Header("등록된 블록 데이터")]
    public List<BlockData> allBlocks = new List<BlockData>();


    public GameObject CreateBlock(BlockSaveData data)
    {
        if (data == null) return null;

        Vector3Int position = data.position;
        Quaternion rotation = data.rotation;
        
        //블록 프리팹 찾기 => 블록타입이 노멀이면 이름으로 찾고, 아니면 타입으로 찾기.
        var blockPrefab = data.blockType == BlockType.Normal ? FindBlockPrefab(data.blockType, data.blockName) : FindBlockPrefab(data.blockType);

        var obj = Instantiate(blockPrefab, position, rotation);
        //switch (data.blockType)
        //{
        //    case BlockType.Box:
        //        obj.AddComponent<Box>().Init(data);
        //        break;
        //    case BlockType.Switch:
        //        obj.AddComponent<Switch>();
        //        break;
        //    case BlockType.Door:
        //        obj.AddComponent<Door>();
        //        break;
        //    case BlockType.End:
        //        obj.AddComponent<EndBlock>();
        //        break;
        //    case BlockType.Start:
        //    case BlockType.Normal:
        //    case BlockType.Slope:
        //    case BlockType.Water:
        //    case BlockType.FlowWater:
        //    case BlockType.Turret:
        //    case BlockType.Tower:
        //    case BlockType.Ironball:
        //    case BlockType.Hog:
        //    case BlockType.Tortoise:
        //    case BlockType.Buffalo:
        //        obj.AddComponent<NormalBlock>();
        //        break;
        //}

        return obj;
    }


    

    public GameObject FindBlockPrefab(BlockType blockType, string blockName = null)
    {
        //블록타입이 노멀이면 이름으로 찾고, 아니면 타입으로 찾기.
        BlockData data = blockType == BlockType.Normal ? allBlocks.Find(x => x.blockName == blockName) : allBlocks.Find(x => x.blockType == blockType);
        if (data == null)
        {
            Debug.LogWarning($"BlockFactory: '{blockName}' 데이터를 찾을 수 없습니다.");
            return null;
        }
        return data.prefab;
    }
}