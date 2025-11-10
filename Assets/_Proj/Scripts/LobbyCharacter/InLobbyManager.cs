using NUnit.Framework;
using System;
using Unity.AI.Navigation;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// 저장되어있는 동물 오브젝트, 데코 오브젝트, 집 오브젝트를 어떻게 분리할까


public class InLobbyManager : MonoBehaviour
{
    [SerializeField] GameObject plane;
    [SerializeField] EditModeController editController;
    [SerializeField] float interactDistance = 2f;
    [SerializeField] float routineDelay = 3f;

    private CocoDoogyBehaviour coco;
    private MasterBehaviour master;

    private NavMeshSurface planeSurface;
    public Transform[] cocoWaypoints;

    public bool isEditMode { get; private set; } // 에딧컨트롤러에서 받아오기
    private int originalLayer; // 평상 시 레이어
    private int editableLayer; // 편집모드 시 레이어
    //private bool oneForInit = false;


    public static InLobbyManager Instance { get; private set; }
    private List<ILobbyState> lobbyCharacter = new(); // 맵에 활성화 된 캐릭터들 모음

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (planeSurface == null)
        {
            planeSurface = FindFirstObjectByType<NavMeshSurface>();
        }

        if (editController == null) editController = FindFirstObjectByType<EditModeController>();
        isEditMode = false;

        originalLayer = LayerMask.NameToLayer("InLobbyObject");
        editableLayer = LayerMask.NameToLayer("Editable");

    }

    private void Start() // 깨끗한 프리팹에 붙여주는 방법이 인게임 아웃게임 전환에서 좋지 않을 깝숑
    {
        planeSurface.BuildNavMesh();

        GameObject gObj = Instantiate(DataManager.Instance.mainChar.GetPrefab(99999), cocoWaypoints[0].position, Quaternion.identity);
        gObj.transform.localScale = new Vector3(4, 4, 4);
        gObj.AddComponent<CocoDoogyBehaviour>();

        GameObject gObj2 = Instantiate(DataManager.Instance.Animal.GetPrefab(30001), cocoWaypoints[1].position, Quaternion.identity);
        gObj2.transform.localScale = new Vector3(4, 4, 4);
        gObj2.AddComponent<AnimalBehaviour>();

        foreach (var lC in lobbyCharacter)
        {
            lC.Init();
            lC.PostInit();
        }
        //coco = gObj.GetComponent<CocoDoogyBehaviour>();
        //coco.gameObject.SetActive(false);

        //StartCoroutine(MainCharRoutineLoop());

        // foreach (var lC in lobbyCharacter)
        // {
        //     if (lC == null) Debug.Log($"{lC} null");
        //     if (lC != null)
        //     {
        //         lC.StartScene();
        //         Debug.Log($"{lC} StartScene");
        //     }
        // }
        // if (!oneForInit)
        // {
        //     초기화
        //     oneForInit = true;
        //     return;
        // }
    }

    private void Update()
    {
        bool current = editController.IsEditMode;
        Debug.Log($"current 상태 : {current}");
        if (current != isEditMode)
        {
            isEditMode = current;
            if (isEditMode)
            {
                foreach (var lC in lobbyCharacter)
                {
                    if (lC != null)
                    {
                        lC.InEdit();
                        var mono = lC as BaseLobbyCharacterBehaviour;
                        //gObj.InEdit();
                        mono.gameObject.layer = editableLayer;
                    }
                }
                Debug.Log("편집모드 진입");
            }
            else if (!isEditMode)
            {
                planeSurface.BuildNavMesh();
                foreach (var lC in lobbyCharacter)
                {
                    if (lC != null)
                    {
                        lC.InNormal();
                        var mono = lC as BaseLobbyCharacterBehaviour;
                        mono.gameObject.layer = originalLayer;
                        //lC.InUpdate();
                    }
                }
                Debug.Log("일반모드 진입");
            }
        }

        // 코코두기 안드로이드 거리 감지 및 상호작용 이벤트
        // 1회성만 가능하게 만들어야함
        // if (coco.gameObject.activeSelf && master.gameObject.activeSelf)
        // {
        //     float dist = Vector3.Distance(coco.transform.position, master.transform.position);
        //     if (dist < interactDistance)
        //     {
        //         coco.OnCocoMasterEmotion();
        //         master.OnCocoMasterEmotion();
        //     }
        // }
    }

    private void OnDestroy()
    {
        // foreach (var lC in lobbyCharacter)
        // {
        //     if (lC != null)
        //     {
        //         lC.ExitScene();
        //         Debug.Log($"{lC} ExitScene 호출");
        //     }
        // }
    }

    private IEnumerator MainCharRoutineLoop()
    {
        // while (true)
        // {
        //     // 루틴 시작
        //     float activeDelay = UnityEngine.Random.Range(2, 5);

        //     if (!coco.gameObject.activeSelf) coco.gameObject.SetActive(true);
        //     yield return new WaitForSeconds(activeDelay);
        //     if (!master.gameObject.activeSelf) master.gameObject.SetActive(true);

        //     if (coco.IsRoutineComplete) coco.gameObject.SetActive(false);
        //     if (master.IsRoutineComplete) master.gameObject.SetActive(false);
        //     yield return new WaitUntil(() => coco.IsRoutineComplete);
        // }
        StartCoroutine(CocoRoutine());
        //StartCoroutine(MasterRoutine());

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator CocoRoutine()
    {
        while (true)
        {
            if (!coco.gameObject.activeSelf) coco.gameObject.SetActive(true);
            yield return new WaitUntil(() => coco.IsCMRoutineComplete);

            coco.gameObject.SetActive(false);
            yield return new WaitForSeconds(routineDelay);

            coco.ResetRoutine();
            coco.ResetInteract(0);
            coco.ResetInteract(1);
        }
    }

    private IEnumerator MasterRoutine()
    {
        while (true)
        {
            float activeDelay = UnityEngine.Random.Range(2, 5);
            yield return new WaitForSeconds(activeDelay);
            if (!master.gameObject.activeSelf) master.gameObject.SetActive(true);
            yield return new WaitUntil(() => master.IsCMRoutineComplete);

            master.gameObject.SetActive(false);
            yield return new WaitForSeconds(routineDelay);

            master.ResetRoutine();
            master.ResetInteract(0);
        }
    }


    // 등록 및 삭제
    public void RegisterLobbyChar(ILobbyState gObj)
    {
        lobbyCharacter.Add(gObj);
        Debug.Log($"{gObj} 등록됨");
    }
    public void UnregisterLobbyChar(ILobbyState gObj)
    {
        lobbyCharacter.Remove(gObj);
        Debug.Log($"{gObj} 삭제됨");
    }
}
