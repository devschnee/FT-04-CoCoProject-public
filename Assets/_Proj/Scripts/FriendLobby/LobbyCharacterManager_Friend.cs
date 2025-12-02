using NUnit.Framework;
using System;
using Unity.AI.Navigation;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AI;

public class LobbyCharacterManager_Friend : MonoBehaviour, ILobbyCharacterManager
{
    [SerializeField] GameObject plane;
    [SerializeField] EditModeController editController;
    [SerializeField] float interactDistance = 10f;
    //[SerializeField] WaypointsControl waypointsControl; // 보류

    private LMCharacterInit lobbyChracterInit; // 씬 시작시 초기화
    private LMWaypoints getWaypoints; // 웨이포인트 얻기
    private LMCharacterRoutineControl routineControl; // 코코두기, 안드로이드 루틴 스케쥴러
    private NavMeshSurface planeSurface; // Bake 용
    private CocoDoogyBehaviour coco; // 코코두기 제어 용
    private MasterBehaviour master; // 안드로이드 제어 용
    private AnimalBehaviour animal; // 코코두기 상호작용 용

    public bool IsEditMode { get; private set; } // 에딧컨트롤러에서 받아오기
    public bool IsInitMode { get; private set; } = true; // 씬 첫 초기화 모드인지 판단
    private int originalLayer; // 평상 시 레이어
    private int editableLayer; // 편집모드 시 레이어
    private float interactionCooldown = 0;
    private Collider[] animalHits; // 코코두기가 상호작용할 동물들 콜라이더 배열(OverlapSphere)

    List<LobbyWaypoint> ILobbyCharacterManager.Waypoints { get => Waypoints; }
    public List<LobbyWaypoint> Waypoints { get; private set; } = new();

    private List<ILobbyState> lobbyCharacter = new(); // 맵에 활성화 된 캐릭터들 모음
    public List<ILobbyState> LobbyCharacter => lobbyCharacter;

    private static event Action<BaseLobbyCharacterBehaviour> HeyManager;
    
    public static LobbyCharacterManager_Friend Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        getWaypoints = new LMWaypoints();
        lobbyChracterInit = new LMCharacterInit(this, lobbyCharacter);

        if (planeSurface == null)
        {
            planeSurface = FindFirstObjectByType<NavMeshSurface>();
        }

        if (editController == null) editController = FindFirstObjectByType<EditModeController>();
        //IsEditMode = false;

        originalLayer = LayerMask.NameToLayer("InLobbyObject");
        editableLayer = LayerMask.NameToLayer("Editable");
    }

    private void OnEnable()
    {
        HeyManager += DeactivateChar;
    }

    private void Start()
    {
        planeSurface.BuildNavMesh();
        //Waypoints = waypointsControl.Waypoints;
        Waypoints = getWaypoints.GetWaypoints();
        StartCoroutine(StartGame());
    }

    private void Update()
    {
        bool current = false;
        //Debug.Log($"current 상태 : {current}");
        if (current != IsEditMode)
        {
            IsEditMode = current;
            if (IsEditMode)
            {
                foreach (var lC in lobbyCharacter)
                {
                    if (lC != null)
                    {
                        var mono = lC as BaseLobbyCharacterBehaviour;
                        if (mono.isActiveAndEnabled)
                        {
                            lC.InEdit();
                            mono.gameObject.layer = editableLayer;
                        }

                    }
                }
                //Debug.Log("편집모드 진입");
            }
            else if (!IsEditMode)
            {
                planeSurface.BuildNavMesh();
                if (SettingManager.Instance != null) SettingManager.Instance.RefreshAnimalPositionEntryList();
                foreach (var lC in lobbyCharacter)
                {
                    if (lC != null)
                    {
                        var mono = lC as BaseLobbyCharacterBehaviour;
                        if (mono.isActiveAndEnabled)
                        {
                            lC.InNormal();
                            mono.gameObject.layer = originalLayer;
                        }
                    }
                }
                //Debug.Log("일반모드 진입");
            }
        }

        // 코코두기 안드로이드 거리 감지 및 상호작용 이벤트
        // 1회성만 가능하게 만들어야함
        if (!IsEditMode && !IsInitMode)
        {
            interactionCooldown -= Time.deltaTime;
            if (interactionCooldown > 0)return;

            // 코코, 깡통 상호작용
            if (coco.gameObject.activeSelf && master.gameObject.activeSelf)
            {

                float distCM = Vector3.Distance(coco.transform.position, master.transform.position);
                if (distCM < interactDistance)
                {
                    var currentState = master.GetCurrentState();
                    if (!coco.IsCMInteracted && (currentState == master.MoveState))
                    {
                        interactionCooldown = 0.3f;
                        coco.OnCocoMasterEmotion();
                        return;
                    }
                }
            }
            // 코코, 고기들 상호작용 OverlapSphere를 업데이트에서 쓰면 안 좋다캄
            if (coco.gameObject.activeSelf)
            {
                int count = Physics.OverlapSphereNonAlloc(coco.transform.position, interactDistance, animalHits, LayerMask.GetMask("InLobbyObject"));

                AnimalBehaviour nearestAnimal = null;

                float nearestDist = float.MaxValue;

                for (int i = 0; i < count; i++)
                {
                    if (animalHits[i].CompareTag("Animal"))
                    {
                        float dist = Vector3.Distance(animalHits[i].transform.position, coco.transform.position);

                        if (dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearestAnimal = animalHits[i].GetComponent<AnimalBehaviour>();
                        }
                    }
                }

                if (nearestAnimal != null && !coco.IsCAInteracted)
                {
                    var currentState = nearestAnimal.GetCurrentState();
                    if (currentState == nearestAnimal.MoveState)
                    {
                        animal = nearestAnimal;
                        interactionCooldown = 0.3f;
                        coco.OnCocoAnimalEmotion();
                    }
                }

                // var hits = Physics.OverlapSphere(coco.transform.position, interactDistance, LayerMask.GetMask("InLobbyObject"));

                // foreach (var hit in hits)
                // {
                //     if (hit.CompareTag("Animal"))
                //     {
                //         var getAnimal = hit.GetComponent<AnimalBehaviour>();
                //         if (!coco.IsCAInteracted)
                //         {
                //             animal = getAnimal;
                //             interactionCooldown = 0.3f;
                //             coco.OnCocoAnimalEmotion();
                //             break;
                //         }
                //     }
                // }
            }
        }
    }

    private void OnDisable()
    {
        HeyManager -= DeactivateChar;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        // foreach (var lC in lobbyCharacter)
        // {
        //     if (lC != null)
        //     {
        //         lC.ExitScene();
        //         Debug.Log($"{lC} ExitScene 호출");
        //     }
        // }
    }

    public void InitWayPoint() // 집 바꿀 시 웨이포인트 재탐색
    {
        Waypoints.Clear();
        Waypoints = getWaypoints.GetWaypoints();

        foreach (var c in lobbyCharacter)
        {
            c.InitWaypoint();
        }

        RefreshAniamlHitsArray();
    }

    private void RefreshAniamlHitsArray()
    {
        int animalCount = 0;
        foreach (var a in lobbyCharacter)
        {
            if (a is AnimalBehaviour) animalCount++;
        }

        animalHits = new Collider[animalCount];
    }

    public List<LobbyWaypoint> GetWaypointsForChar()
    {
        return Waypoints;
    }

    private IEnumerator StartGame()
    {
        yield return StartCoroutine(lobbyChracterInit.Init());
        coco = lobbyChracterInit.CocoInit();
        master = lobbyChracterInit.MasterInit();
        routineControl = new LMCharacterRoutineControl(this, coco, master);
        RefreshAniamlHitsArray(); // 로비에 나온 동물들 찾아서 animalHits에 넣기
        IsInitMode = false;
        StartCoroutine(routineControl.MainCharRoutineLoop());
    }

    

    // 코코두기 안드로이드 전용 이벤트
    public static void RaiseCharacterEvent(BaseLobbyCharacterBehaviour who)
    {
        HeyManager?.Invoke(who);
    }
    
    private void DeactivateChar(BaseLobbyCharacterBehaviour who)
    {
        who.gameObject.SetActive(false);
        who.SetCocoMasterIsActive(false); // 루틴 컨트롤 위함
    }


    public CocoDoogyBehaviour GetCoco()
    {
        return coco;
    }
    public MasterBehaviour GetMaster()
    {
        return master;
    }
    public AnimalBehaviour GetAnimal()
    {
        return animal;
    }


    // 로비 캐릭터들 등록 및 삭제
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
