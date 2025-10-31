using System;
using System.Collections.Generic;
using UnityEngine;

public enum BlockType
{
    // 일반블록, 나무상자, 철구, 흐르는물, 안흐르는물, 스위치, 기계식문, 충격감지탑, 멧돼지, 거북이, 버팔로, (경사로), 감시터렛, 시작점, 골인지점, 보물(보물의 경우 1,2,3으로 id 지정)
    Normal, Slope, Water, FlowWater, Box, Switch, Turret, Tower, Door, Ironball, Hog, Tortoise, Buffalo, Start, End
}

[Serializable]
public class OptionalProperty
{
    public bool isOn = false;
    public Vector3Int linkedPos = new(int.MaxValue, int.MaxValue, int.MaxValue);
}
public interface IOptionalProperty
{
    public OptionalProperty property { get; set; }


}

[Serializable]
public class BlockSaveData
{

    // 이 클래스는 실제 JSON 직렬화에 이용됨. (맵데이터가 가진 List<BlockSaveData>에 저장되어 전달됨.
    // JSON 직렬화를 위해서, BlockIdentity : MonoBehaviour에서 제공되는 기본 속성
    //블록의 종류
    public BlockType blockType;
    //블록의 이름
    public string blockName;

    //배치 정보에서 뽑아오는 해당 블록의 위치, 회전값 정보
    public Vector3Int position;
    public Quaternion rotation;

    //BlockIdentity를 상속한 TriggerIdentity, GimikIdentity는 모두 IPropertyHandler를 구현함.
    //IPropertyHandler가 가진 속성이므로 TriggerIdentity, GimikIdentity도 가지고 있다고 볼 수 있음.
    public OptionalProperty property;
}

[Serializable]
public class MapData
{
    //모든 엔티티를 단순 리스트로 담아서 저장.
    public List<BlockSaveData> blocks = new List<BlockSaveData>();



}