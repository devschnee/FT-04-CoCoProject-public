using System.Collections;
using UnityEngine;

/// <summary>
/// 코코두기와 안드로이드 루틴 담당
/// 상호작용도 파츠 클래스로 만들까?
/// </summary>
public class LMCharacterRoutineControl
{
    private CocoDoogyBehaviour coco;
    private MasterBehaviour master;
    //private ILobbyCharactersEmotion emotion;
    private WaitForSeconds delay;
    private WaitUntil wait;
    //private float interactDistance = 10f;
    //private float loading = 0f;
    //private float RoutineDelay = 5f;

    public LMCharacterRoutineControl(CocoDoogyBehaviour coco, MasterBehaviour master)
    {
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
            // if (coco.gameObject.activeSelf && master.gameObject.activeSelf)
            // {
            //     // 코코두기와 마스터, 동물들 상호작용. 일단 코코두기와 마스터 먼저 만들자.
            //     if (LobbyCharacterManager.Instance.IsEditMode == false)
            //     {
            //         float cmDist = Vector3.Distance(coco.transform.position, master.transform.position);
            //         if (cmDist < interactDistance && !coco.IsInteracting && !coco.IsCMInteracted)
            //         {
            //             coco.OnCocoMasterEmotion();
            //             master.OnCocoMasterEmotion();
            //         }
            //     }
            //     else
            //     {
            //         yield return wait;
            //     }
            // }
            yield return null;
        }
    }

    private IEnumerator CocoRoutine()
    {
        //if (LobbyCharacterManager.Instance.IsEditMode) yield return wait;
        if (LobbyCharacterManager.Instance.IsEditMode == false)
        {
            yield return delay = new (5f); // 로비 시작 후 좀 있다가 생성하는게 이쁘지 않을까?
            coco.gameObject.SetActive(true);
            coco.ResetInteractCount();
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
            yield return delay = new (10f);
            master.gameObject.SetActive(true);
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

// 코코두기 안드로이드 거리 감지 및 상호작용 이벤트
//      1회성만 가능하게 만들어야함
//      if (coco.gameObject.activeSelf && master.gameObject.activeSelf)
//      {
//          float dist = Vector3.Distance(coco.transform.position, master.transform.position);
//          if (dist < interactDistance)
//          {
//               coco.OnCocoMasterEmotion();
//              master.OnCocoMasterEmotion();
//          }
//      }
