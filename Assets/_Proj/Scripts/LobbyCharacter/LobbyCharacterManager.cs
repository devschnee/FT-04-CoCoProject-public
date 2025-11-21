using NUnit.Framework;
using System;
using Unity.AI.Navigation;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AI;

// 저장되어있는 동물 오브젝트, 데코 오브젝트, 집 오브젝트를 어떻게 분리할까


public class LobbyCharacterManager : MonoBehaviour
{
    [SerializeField] GameObject plane;
    [SerializeField] EditModeController editController;
    [SerializeField] float interactDistance = 10f;
    //[SerializeField] WaypointsControl waypointsControl; // 보류

    private LMCharacterInit lobbyChracterInit; // 씬 시작시 초기화
    private LMWaypoints getWaypoints; // 웨이포인트 얻기
    private LMCharacterRoutineControl routineControl;
    private NavMeshSurface planeSurface; // Bake 용
    private CocoDoogyBehaviour coco; // 코코두기 제어 용
    private MasterBehaviour master; // 안드로이드 제어 용

    public bool IsEditMode { get; private set; } // 에딧컨트롤러에서 받아오기
    public bool IsInitMode = true;
    private int originalLayer; // 평상 시 레이어
    private int editableLayer; // 편집모드 시 레이어
    private float interactionCooldown = 0;

    public List<LobbyWaypoint> Waypoints { get; private set; } = new();
    private List<ILobbyState> lobbyCharacter = new(); // 맵에 활성화 된 캐릭터들 모음

    private static event Action<BaseLobbyCharacterBehaviour> HeyManager;
    
    public static LobbyCharacterManager Instance { get; private set; }

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
        IsEditMode = false;

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
        bool current = editController.IsEditMode;
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
            if (coco.gameObject.activeSelf && master.gameObject.activeSelf)
            {
                interactionCooldown -= Time.deltaTime;
                if (interactionCooldown > 0)return;
                float dist = Vector3.Distance(coco.transform.position, master.transform.position);
                if (dist < interactDistance)
                {
                    if (!coco.IsCMInteracted)
                    {
                        interactionCooldown = 0.3f;
                        coco.OnCocoMasterEmotion();
                    }
                }
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

    // private void ChangeMasterStateToInteractState()
    // {
    // }
    // private void ChangeAnimalStateToInteractState()
    // {
    //     foreach (var lC in lobbyCharacter)
    //     {
    //         if (lC is not AnimalBehaviour) return;
    //     }
    // }

    public void InitWayPoint()
    {
        Waypoints.Clear();
        Waypoints = getWaypoints.GetWaypoints();

        foreach (var c in lobbyCharacter)
        {
            c.InitWaypoint();
        }
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
        routineControl = new LMCharacterRoutineControl(coco, master);
        IsInitMode = false;
        StartCoroutine(routineControl.MainCharRoutineLoop());
    }

    public CocoDoogyBehaviour GetCoco()
    {
        return coco;
    }
    public MasterBehaviour GetMaster()
    {
        return master;
    }

    // 코코두기 안드로이드 전용 이벤트
    public static void RaiseCharacterEvent(BaseLobbyCharacterBehaviour who)
    {
        HeyManager?.Invoke(who);
    }
    private void DeactivateChar(BaseLobbyCharacterBehaviour who)
    {
        who.gameObject.SetActive(false);
        who.CocoMasterSetIsActive(false); // 루틴 컨트롤 위함
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
