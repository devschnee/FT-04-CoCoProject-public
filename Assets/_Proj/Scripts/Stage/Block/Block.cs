using UnityEngine;

//게임 스테이지에 등장하는 모든 블록의 부모가 되는 스크립트.
//스테이지 로드 시에, JSON 정보로 블록을 생성하고 나면,
//블록의 enum이나 이름으로 분기하여 적절한 타입의 컴포넌트를 붙여주도록 한다. (예: 기본블록-AddComponent<NormalBlock>(); // 나무상자 - AddComponent<WoodBox>();
public abstract class Block : MonoBehaviour
{
    [Header("인게임 로직 관련 필드")]
    //[Tooltip("'지면'으로 분류되는 지 여부")]
    //public bool isGround;
    //[Tooltip("윗면에 오브젝트를 쌓을 수 있는지 여부")]
    //public bool isStackable;
    //[Tooltip("고정된 물체인지 여부")]
    //public bool isStatic;
    //[Tooltip("다른 블록이 겹쳐질 수 있는지 여부")]
    //public bool isOverlapping;
    [Tooltip("그리드 상에서의 포지션(정수)")]
    public Vector3Int gridPosition;
    [Tooltip("가져온 블록세이브데이터의 원본")]
    public BlockSaveData origin;



    

    public void Init(BlockSaveData saveData)
    {
        this.origin = saveData;
    }


    protected virtual void OnEnable()
    {
        gridPosition = Vector3Int.RoundToInt(transform.position);
    }
}
