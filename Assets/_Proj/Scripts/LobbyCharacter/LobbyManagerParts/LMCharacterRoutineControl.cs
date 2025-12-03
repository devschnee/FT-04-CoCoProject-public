using System.Collections;
using UnityEngine;

/// <summary>
/// 코코두기와 안드로이드 루틴 담당
/// 상호작용도 파츠 클래스로 만들까?
/// </summary>
public class LMCharacterRoutineControl
{
    private readonly ILobbyCharacterManager lobbyManager;
    private CocoDoogyBehaviour coco;
    private MasterBehaviour master;
    private WaitForSeconds delay;
    private WaitUntil wait;

    public LMCharacterRoutineControl(ILobbyCharacterManager lobbyManager, CocoDoogyBehaviour coco, MasterBehaviour master)
    {
        this.lobbyManager = lobbyManager;
        this.coco = coco;
        this.master = master;
        Debug.Log($"coco : {this.coco.gameObject.name}");
        Debug.Log($"master : {this.master.gameObject.name}");
        wait = new WaitUntil(() => LobbyCharacterManager.Instance.IsEditMode == false);
    }

    public IEnumerator MainCharRoutineLoop()
    {
        while (true)
        {
            if (!coco.gameObject.activeSelf) yield return CocoRoutine();
            if (!master.gameObject.activeSelf) yield return MasterRoutine();
            yield return null;
        }
    }

    private IEnumerator CocoRoutine()
    {

        //if (LobbyCharacterManager.Instance.IsEditMode) yield return wait;
        if (LobbyCharacterManager.Instance.IsEditMode == false)
        {
            Vector3 spawnPos = SpawnPoint.GetSpawnPoint(lobbyManager.Waypoints[0].transform.position);
            coco.transform.position = spawnPos;
            yield return delay = new (5f); // 로비 시작 후 좀 있다가 생성하는게 이쁘지 않을까?
            coco.gameObject.SetActive(true);
            coco.SetCharInteracted(2);
            coco.SetTimeToGoHome(false);
            yield break;
        }
        else
        {
            yield return wait;
        }
        
    }
    private IEnumerator MasterRoutine()
    {

        //if (LobbyCharacterManager.Instance.IsEditMode) yield return wait;
        if (LobbyCharacterManager.Instance.IsEditMode == false)
        {
            Vector3 spawnPos = SpawnPoint.GetSpawnPoint(lobbyManager.Waypoints[0].transform.position);
            master.transform.position = spawnPos;
            yield return delay = new (10f);
            master.gameObject.SetActive(true);
            master.SetTimeToGoHome(false);
            yield break;
        }
        else
        {
            yield return wait;
        }
        
    }
}

// private IEnumerator MainCharRoutineLoop()
//     {
//         // while (true)
//         // {
//         //     // 루틴 시작
//         //     float activeDelay = UnityEngine.Random.Range(2, 5);

//         //     if (!coco.gameObject.activeSelf) coco.gameObject.SetActive(true);
//         //     yield return new WaitForSeconds(activeDelay);
//         //     if (!master.gameObject.activeSelf) master.gameObject.SetActive(true);

//         //     if (coco.IsRoutineComplete) coco.gameObject.SetActive(false);
//         //     if (master.IsRoutineComplete) master.gameObject.SetActive(false);
//         //     yield return new WaitUntil(() => coco.IsRoutineComplete);
//         // }
//         StartCoroutine(CocoRoutine());
//         //StartCoroutine(MasterRoutine());

//         yield return new WaitForSeconds(1f);
//     }

//     private IEnumerator CocoRoutine()
//     {
//         while (true)
//         {
//             if (!coco.gameObject.activeSelf) coco.gameObject.SetActive(true);
//             yield return new WaitUntil(() => coco.IsCMRoutineComplete);

//             coco.gameObject.SetActive(false);
//             yield return new WaitForSeconds(routineDelay);

//             coco.ResetRoutine();
//             coco.ResetInteract(0);
//             coco.ResetInteract(1);
//         }
//     }

//     private IEnumerator MasterRoutine()
//     {
//         while (true)
//         {
//             float activeDelay = UnityEngine.Random.Range(2, 5);
//             yield return new WaitForSeconds(activeDelay);
//             if (!master.gameObject.activeSelf) master.gameObject.SetActive(true);
//             yield return new WaitUntil(() => master.IsCMRoutineComplete);

//             master.gameObject.SetActive(false);
//             yield return new WaitForSeconds(routineDelay);

//             master.ResetRoutine();
//             master.ResetInteract(0);
//         }
//     }
