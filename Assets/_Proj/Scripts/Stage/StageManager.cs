using Game.Inventory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class StageClearInfo
{
    public string stageName;
    public int score; //0, 1, 2, 3
}
public interface IStageManager { }
public class StageManager : MonoBehaviour, IStageManager
{
    //이 클래스가 해야 할 일: 스테이지 구성 요청(블록팩토리), 스테이지 내 각종 상호작용 상태 기억, 시작점에 주인공 생성, 주인공이 도착점에 도달 시 스테이지 클리어 처리.

    [Header("IS TEST MODE??????????????????")]
    public bool isTest;


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

    public string currentStageId;

    public string cutsceneUrl; // 컷씬이 있으면 세팅
    public bool hasCutscene = false;

    //맵의 이름으로 찾아온 현재 맵 데이터 객체 (초기상태로의 복귀를 위해 필요)
    private MapData currentMapData; //맵데이터는 늘 기억되고 있을 것임.

    private List<IPlayerFinder> finders = new();

    private bool[] collectedTreasures = new bool[3];


    [SerializeField] BlockFactory factory;


    void Awake()
    {
        if (!isTest)
        {
            currentMapData = FirebaseManager.Instance.currentMapData;
            currentStageId = FirebaseManager.Instance.selectStageID;
        }
    }
    async void Start()
    {
        //1. 파이어베이스가 맵 정보를 가져오길 기다림.
        //TODO: 나중에, 스테이지 들어오기 전에 이미 파이어베이스매니저는 로드할 스테이지 정보를 갖고 들어올 것이기 때문에 Start는 async일 필요 없음.
        if (isTest)
        {
            await Task.Delay(200); //이거 왜 하냐면 파이어베이스매니저가 아직 초기화가 안된 상황일 가능성이 높기 때문임
            currentMapData = await FirebaseManager.Instance.LoadMapFromFirebaseByMapID(mapNameToLoad);
            currentStageId = DataManager.Instance.Stage.GetMapNameData(mapNameToLoad) != null ? DataManager.Instance.Stage.GetMapNameData(mapNameToLoad).stage_id : "Testonly Stage";
        }


        StartCoroutine(StageStart());
    }
    IEnumerator StageStart()
    {
        //가림막쳐주기
        StageUIManager.Instance.stageIdInformation.stageIdInfo = currentStageId;
        var fp = StageUIManager.Instance.FadePanel;
        fp.SetActive(true);
        //stageRoot.name = mapNameToLoad;
        //2. 가져온 맵 정보로 이 씬의 블록팩토리가 맵을 생성하도록 함.
        //2-1. 블록팩토리가 맵을 생성
        LoadStage(currentMapData);
        yield return null;
        InspectBlocks();
        //TODO: 2-2. 블록팩토리가 맵의 오브젝트들 중 서로 연결된 객체를 연결해 줌.
        LinkSignals();

        //가림막치워주기
        fp.GetComponent<FadeController>().FadeOut();
        //if (isTest)
        //{
        //    var dataTest = DataManager.Instance.Stage.GetMapNameData(mapNameToLoad);

        //    if (dataTest.start_talk != "-1")
        //        DialogueManager.Instance.NewDialogueMethod(dataTest.start_talk);

        //    SpawnPlayer();

        //    yield return null;
        //}

        var data = DataManager.Instance.Stage.GetData(currentStageId);
        var data_start_cutscene = DataManager.Instance.Stage.GetStartCutsceneUrl(currentStageId);
        if (!string.IsNullOrEmpty(data_start_cutscene) && data.start_cutscene != "-1")
        {
            yield return PlayCutscene(data_start_cutscene);
        }
        //TODO: 3. 가져온 맵 정보로 모든 블록이 생성되고 연결까지 끝나면 가리고 있던 부분을 치워줌.
        AudioClip bgmClip = DataManager.Instance.Stage.GetAudioClip(currentStageId);
        AudioEvents.RaiseStageBGM(bgmClip);

        camControl.FindWayPoint();
        yield return camControl.StartCoroutine(camControl.CameraWalking(1.25f));


        //Todo : 컷씬 지난후 대화가 있다면 여기서 실행
        if (data.start_talk != "-1")
            DialogueManager.Instance.NewDialogueMethod(data.start_talk);
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
    public void ClearStage()
    {
        StartCoroutine(ClearStageRoutine());
    }

    IEnumerator ClearStageRoutine()
    {
        Debug.Log("[Stage] Clear → 결과 UI 오픈");

        var data = DataManager.Instance.Stage.GetData(currentStageId);

        if(data.end_talk != "-1")
    {
            DialogueManager.Instance.NewDialogueMethod(data.end_talk);
            while (DialogueManager.Instance.isDialogueActive)
                yield return null;
        }

        // 결과 UI 세팅 (기존 로직)
        ShowResultUI();

        // 확인 버튼 누를 때까지 대기
        bool waitConfirm = true;
        StageUIManager.Instance.ExitButton.onClick.AddListener(() =>
        {
            waitConfirm = false;
        });
        while (waitConfirm)
            yield return null;

        var data_end_cutscene = DataManager.Instance.Stage.GetEndCutsceneUrl(currentStageId);
        if (!string.IsNullOrEmpty(data_end_cutscene) && data.end_cutscene != "-1")
        {
            yield return PlayCutscene(data_end_cutscene);
        }

        // 메인씬으로 이동
        //Todo : 챕터에 따라 스테이지 선택화면 분기
        SceneManager.LoadScene("Main");
    }

    private void ShowResultUI()
    {
        //LSH 추가 1120
        AudioEvents.Raise(UIKey.Stage, 0);
        // 기존 UI 열기
        StageUIManager.Instance.Overlay.SetActive(true);
        StageUIManager.Instance.ResultPanel.SetActive(true);
        StageUIManager.Instance.OptionOpenButton.gameObject.SetActive(false);

        var data = DataManager.Instance.Stage.GetData(currentStageId);

        StageUIManager.Instance.stageName.text = data.stage_name;

        int collectedCount = collectedTreasures.Count(x => x);

        var prev = PlayerProgressManager.Instance.GetStageProgress(data.stage_id);
        int prevCount = prev.treasureCollected.Count(x => x);

        // 이전보다 더 많이 모았을 때만 갱신
        if (collectedCount > prevCount)
        {
            PlayerProgressManager.Instance.UpdateStageTreasure(data.stage_id, collectedTreasures);
            Debug.Log($"[StageManager] 별 갱신됨 ({prevCount} → {collectedCount})");
        }
        else
        {
            Debug.Log($"[StageManager] 기존보다 별 개수가 적거나 같음 ({prevCount}), 갱신 안 함");
        }

        UpdateTreasureIcons(
            collectedCount >= 1,
            collectedCount >= 2,
            collectedCount >= 3
        );

        // bestTreasureCount 업데이트
        if (prev.bestTreasureCount < 0)
        {
            prev.bestTreasureCount = collectedCount;
        }
        else
        {
            prev.bestTreasureCount = Mathf.Max(prev.bestTreasureCount, collectedCount);
        }
        ClaimRewards();
        PlayerProgressManager.Instance.SaveProgress();
    }



    //보물 획득으로 해금되는 도감의 해금 처리. 이전보다 더 많은 별을 획득했는지의 여부는 관심 없이 스테이지 클리어하면 곧바로 해금하도록 처리.
    void ClaimRewards()
    {
        //리플레이시
        //추가 획득이나 코인같은 보상대체는 없음                                                 
        var stageData = DataManager.Instance.Stage.GetData(currentStageId);
        string[] treasureIds = { stageData.treasure_01_id, stageData.treasure_02_id, stageData.treasure_03_id };

        Func<int, int, int, bool> rangeFunc = new((min, max, value) => min < value && value < max );

        for (int i = 0; i < treasureIds.Length; i++)
        {
            if (collectedTreasures[i])
            {
                var itemIdFromTreasure = DataManager.Instance.Treasure.GetData(treasureIds[i]).reward_id;
                int qty = DataManager.Instance.Treasure.GetData(treasureIds[i]).count;
                if (rangeFunc(50000, 60000, itemIdFromTreasure))
                    //아티팩트라는 뜻: 그럼 도감만 단순 해금.
                {
                    UserData.Local.codex[CodexType.artifact, itemIdFromTreasure] = true;
                }
                if (rangeFunc(10000, 20000, itemIdFromTreasure))
                    //조경물(배치물)에 해당됨.
                {
                    InventoryService.I.Add(itemIdFromTreasure, qty);
                }
                if (rangeFunc(20000, 30000, itemIdFromTreasure))
                //코스튬에 해당됨.
                {
                    InventoryService.I.Add(itemIdFromTreasure, qty);
                }
                if (rangeFunc(110000, 120000, itemIdFromTreasure))
                //재화에 해당됨.
                {
                    //코드 구조가 좀 이상한데... 모르겠다 작동은 잘 될 거임.
                    GoodsService service = new GoodsService(new UserDataGoodsStore(110001, 110002, 110003));
                    service.Add(itemIdFromTreasure, qty);
                }
            }
        }

        foreach (Block block in blocks)
        {
            if (block is HogBlock) { UserData.Local.codex[CodexType.animal, 30001] = true; break; }
            if (block is TortoiseBlock) { UserData.Local.codex[CodexType.animal, 30002] = true; break; }
            if (block is BuffaloBlock) { UserData.Local.codex[CodexType.animal, 30003] = true; break; }
        }

        //모르겠다... 레이스컨디션 문제에서 과연 안전할까?
        UserData.Local.codex.Save();

    }


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
        currentStageId = FirebaseManager.Instance.selectStageID;

        var data = DataManager.Instance.Stage.GetData(currentStageId);

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

            if (!isTest)
            {
                //보물 블록 처리
                if (block.blockType == BlockType.Treasure)
                {
                    var treasure = go.GetComponent<Treasure>();

                    // blockName으로 구분
                    if (block.blockName.Contains("1"))
                        treasure.Init(data.treasure_01_id);
                    else if (block.blockName.Contains("2"))
                        treasure.Init(data.treasure_02_id);
                    else if (block.blockName.Contains("3"))
                        treasure.Init(data.treasure_03_id);
                    else
                        Debug.LogWarning($"Treasure 블록 이름 인식 실패: {block.blockName}");
                }
                if (block.blockType == BlockType.Dialogue)
                {
                    var dialogue = go.GetComponent<Dialogue>();

                    if (block.blockName.Contains("1"))
                        dialogue.Init(data.dialogue_box_1);
                    else if (block.blockName.Contains("2"))
                        dialogue.Init(data.dialogue_box_2);
                    else if (block.blockName.Contains("3"))
                        dialogue.Init(data.dialogue_box_3);
                    else if (block.blockName.Contains("4"))
                        dialogue.Init(data.dialogue_box_4);
                    else if (block.blockName.Contains("5"))
                        dialogue.Init(data.dialogue_box_5);
                    else if (block.blockName.Contains("6"))
                        dialogue.Init(data.dialogue_box_6);
                    else if (block.blockName.Contains("7"))
                        dialogue.Init(data.dialogue_box_7);
                    else if (block.blockName.Contains("8"))
                        dialogue.Init(data.dialogue_box_8);
                    else if (block.blockName.Contains("9"))
                        dialogue.Init(data.dialogue_box_9);
                }
            }
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

    public void OnTreasureCollected(int index)
    {
        if (index >= 0 && index < 3)
        {
            // 이미 먹은 보물이라면 무시
            if (collectedTreasures[index]) return;

            collectedTreasures[index] = true;

            // 보물 개수에 따라 별 단계 계산
            int starCount = collectedTreasures.Count(x => x);
            UpdateTreasureIcons(
                starCount >= 1,
                starCount >= 2,
                starCount >= 3
            );
            switch(starCount)
            {
                case 0:
                case 1:
                    StageUIManager.Instance.stageImage.sprite = StageUIManager.Instance.ResultCoCoDoogySprite[0];
                    break;
                case 2:
                    StageUIManager.Instance.stageImage.sprite = StageUIManager.Instance.ResultCoCoDoogySprite[1];
                    break;
                case 3:
                    StageUIManager.Instance.stageImage.sprite = StageUIManager.Instance.ResultCoCoDoogySprite[2];
                    break;

            }
        }
    }

    public void UpdateTreasureIcons(bool t1, bool t2, bool t3)
    {

        if (StageUIManager.Instance.star != null && StageUIManager.Instance.star.Length >= 3)
        {
            StageUIManager.Instance.star[0].sprite = t1 ? StageUIManager.Instance.collectedSprite : StageUIManager.Instance.notCollectedSprite;
            StageUIManager.Instance.star[1].sprite = t2 ? StageUIManager.Instance.collectedSprite : StageUIManager.Instance.notCollectedSprite;
            StageUIManager.Instance.star[2].sprite = t3 ? StageUIManager.Instance.collectedSprite : StageUIManager.Instance.notCollectedSprite;
        }

        // 보물 아이콘 슬롯 확인
        if (StageUIManager.Instance.reward == null || StageUIManager.Instance.reward.Length < 3)
            return;

        // 스테이지 데이터 불러오기
        var data = DataManager.Instance.Stage.GetData(currentStageId);
        var treasure1 = DataManager.Instance.Treasure.GetData(data.treasure_01_id);
        var treasure2 = DataManager.Instance.Treasure.GetData(data.treasure_02_id);
        var treasure3 = DataManager.Instance.Treasure.GetData(data.treasure_03_id);
        TreasureData[] treasures = { treasure1, treasure2, treasure3 };

        bool[] collected = collectedTreasures;

        for (int i = 0; i < 3; i++)
        {
            if (treasures[i] == null) continue;

            Image rewardIcon = StageUIManager.Instance.reward[i];
            string codexId = treasures[i].view_codex_id;

            // CodexProvider 통해 아이콘 불러오기
            Sprite iconSprite = DataManager.Instance.Codex.GetCodexIcon(codexId);

            if (iconSprite != null)
            {
                rewardIcon.sprite = iconSprite;
            }
            else
            {
                Debug.LogWarning($"[StageManager] Codex 아이콘 로드 실패: {codexId}");
                rewardIcon.sprite = StageUIManager.Instance.notCollectedSprite;
            }

            // 회색 처리 (획득 안한 건)
            rewardIcon.color = collected[i] ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
        }
    }

    IEnumerator PlayCutscene(string cutscendId)
    {
        if (UserData.Local.preferences.skipDialogues == true) yield break;
        if (string.IsNullOrEmpty(cutscendId) || cutscendId == "-1")
        {
            Debug.Log("[StageManager] 컷신 없음 또는 StageData 없음, 재생 스킵");
            yield break;
        }
       StageUIManager.Instance.videoImage.SetActive(true);

        if (VideoPlayerController.Instance != null)
            yield return VideoPlayerController.Instance.PlayCutscene(cutscendId);
        else
            Debug.LogError("[StageManager] VideoPlayerController.Instance가 씬에 없습니다.");
    }
}
