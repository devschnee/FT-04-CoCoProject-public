using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



public class TestOnly_StageManager : MonoBehaviour, IStageManager
{
    //이 클래스가 해야 할 일: 스테이지 구성 요청(블록팩토리), 스테이지 내 각종 상호작용 상태 기억, 시작점에 주인공 생성, 주인공이 도착점에 도달 시 스테이지 클리어 처리.

    [Header("IS TEST MODE??????????????????")]
    


    public Transform stageRoot;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] Joystick joystickPrefab;
    [SerializeField] Transform joystickRoot;
    public GameObject playerObject;
    public CamControl camControl;
    //불러올 맵의 이름
    [Header("PLEASE USE ONLY WHEN TEST MODE")]
    public string mapNameToLoad;

    Vector3Int startPoint;
    EndBlock endBlock;

    public Dictionary<Vector3Int, List<Block>> blockDictionary = new();

    public List<Vector3Int> blockPositions = new();
    public List<Block> blocks = new();

   


    //맵의 이름으로 찾아온 현재 맵 데이터 객체 (초기상태로의 복귀를 위해 필요)
    private MapData currentMapData; //맵데이터는 늘 기억되고 있을 것임.

    private List<IPlayerFinder> finders = new();



    [SerializeField] BlockFactory factory;


    void Awake()
    {

          
 
    }
    async void Start()
    {
        //1. 파이어베이스가 맵 정보를 가져오길 기다림.
        //TODO: 나중에, 스테이지 들어오기 전에 이미 파이어베이스매니저는 로드할 스테이지 정보를 갖고 들어올 것이기 때문에 Start는 async일 필요 없음.

            await Task.Delay(200); //이거 왜 하냐면 파이어베이스매니저가 아직 초기화가 안된 상황일 가능성이 높기 때문임
            currentMapData = await FirebaseManager.Instance.LoadMapFromFirebaseByMapID(mapNameToLoad);
           



        StartCoroutine(StageStart());
    }
    IEnumerator StageStart()
    {
        //stageRoot.name = mapNameToLoad;
        //2. 가져온 맵 정보로 이 씬의 블록팩토리가 맵을 생성하도록 함.
        //2-1. 블록팩토리가 맵을 생성
        LoadStage(currentMapData);
        yield return null;
        InspectBlocks();
        //TODO: 2-2. 블록팩토리가 맵의 오브젝트들 중 서로 연결된 객체를 연결해 줌.
        LinkSignals();

        //가림막치워주기

        //if (isTest)
        //{
        //    var dataTest = DataManager.Instance.Stage.GetMapNameData(mapNameToLoad);

        //    if (dataTest.start_talk != "-1")
        //        DialogueManager.Instance.NewDialogueMethod(dataTest.start_talk);

        //    SpawnPlayer();

        //    yield return null;
        //}

        
        //TODO: 3. 가져온 맵 정보로 모든 블록이 생성되고 연결까지 끝나면 가리고 있던 부분을 치워줌.

        camControl.FindWayPoint();
        yield return camControl.StartCoroutine(camControl.CameraWalking(12f));

        //Todo : 컷씬 지난후 대화가 있다면 여기서 실행
        
        //TODO: 4. 시작점에 코코두기를 생성해줌.
        SpawnPlayer();
        //yield return null;

        yield return null;
        //TODO: 5. 카메라 연출 시작

        //6. 연출 종료 시부터 게임 시작.
    }

    //TODO: 상호작용 상태 기억시키기

    //TODO: 도착점 도달 시 스테이지 클리어 처리시키기.
    //스테이지 클리어를 감지할 객체가 필요함.
    //초기에 그 객체의 StageManager 필드에 이 객체를 기억시킴.
    //감지되면, 이 객체가 가진 ClearStage()를 호출함.




   
    void SpawnPlayer()
    {
        playerObject = Instantiate(playerPrefab, startPoint, Quaternion.identity);
        var joystick = Instantiate(joystickPrefab, joystickRoot);
        joystick.GetComponent<RectTransform>().anchoredPosition = new(300, 200);
        playerObject.GetComponent<PlayerMovement>().joystick = joystick;


        //TODO: 나중에 꼭 지울 것.
        camControl.playerObj = playerObject;
        foreach (var finder in finders)
        {
            finder.SetPlayerTransform(playerObject);
        }
    }


    void LoadStage(MapData loaded)
    {
        //currentStageId = FirebaseManager.Instance.selectStageID;

        //var data = DataManager.Instance.Stage.GetData(currentStageId);

        foreach (var block in loaded.blocks)
        {


            //print($"[StageManager] {block.blockName}: {block.blockType} [{block.position.x}],[{block.position.y}],[{block.position.z}]");
            //여기서 팩토리가 들고 있는 프리팹으로 인스턴시에이트.

            //생성 후 블록의 타입으로 컴포넌트 붙여주는 처리는 BlockFactory에서 담당.
            GameObject go = factory.CreateBlock(block);
            go.transform.SetParent(stageRoot, true);
            go.name = block.blockName;


            if (block.blockType == BlockType.Start)
                startPoint = block.position;
            if (block.blockType == BlockType.End)
                go.GetComponent<EndBlock>().Init(this);
            if (BlockType.Hog <= block.blockType && block.blockType <= BlockType.Buffalo)
                finders.Add(go.GetComponent<IPlayerFinder>());

            //GetComponent<Block>().Init(block);
            EnlistBlock(go.GetComponent<Block>());
            if (loaded.blocks.Find(x => x.blockType == BlockType.Start) == null) //스타트 없는 스테이지다?
            {
                Debug.Log("스테이지의 시작점이 없음. 원점 + 5y지점에 주인공 생성.");
                startPoint = Vector3Int.up * 5;
            }
        }
        
        blockPositions = blockDictionary.Keys.ToList();

        foreach (var kv in blockDictionary)
            blocks.AddRange(kv.Value);
    }

    void InspectBlocks()
    {
        foreach (var block in blocks)
        {
            if (block is IEdgeColliderHandler handlerBlock)
            {
                handlerBlock.Inject();
                handlerBlock.DetectAndApplyFourEdge();
            }
        }
    }

    void LinkSignals()
    {
        foreach (var block in blocks)
        {
            if (block is ISignalSender sender)
            {
                if (block.origin.property.linkedPos != Vector3Int.one * int.MaxValue)
                {
                    ISignalReceiver receiver = blockDictionary[block.origin.property.linkedPos].Find(x => x is ISignalReceiver) as ISignalReceiver;
                    sender.ConnectReceiver(receiver);
                    print($"{block.name}:연결함 - {receiver}");
                }
                else
                {
                    Debug.Log($"{block.name}:{block.origin.position} - 연결된 대상이 없습니다. 확인 바랍니다.");
                }
            }
        }
    }

    void EnlistBlock(Block target)
    {
        if (!blockDictionary.ContainsKey(target.gridPosition))
            blockDictionary.Add(target.gridPosition, new() { target });
        else
            blockDictionary[target.gridPosition].Add(target);
    }

    

    

    
}
