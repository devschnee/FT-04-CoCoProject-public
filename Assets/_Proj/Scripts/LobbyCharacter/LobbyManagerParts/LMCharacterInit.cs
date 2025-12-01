using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 초기화 넣으니 코드 점점 더러워지네

public class LMCharacterInit
{
    private readonly ILobbyCharacterManager LCM;
    private CocoDoogyBehaviour coco;
    private MasterBehaviour master;
    private List<ILobbyState> lobbyCharacter = new();

    public LMCharacterInit(ILobbyCharacterManager LCM, List<ILobbyState> lobbyCharacter)
    {
        this.LCM = LCM;
        this.lobbyCharacter = lobbyCharacter;
    }
    public IEnumerator Init()
    {
        GameObject[] animals = GameObject.FindGameObjectsWithTag("Animal");    
        int priority = 30;
        foreach (var a in animals)
        {
            var init = a.GetComponent<AnimalBehaviour>() ?? a.AddComponent<AnimalBehaviour>();
            if (a.CompareTag("Animal"))
            {
                init.Init();
                yield return null;

                init.PostInit();
                yield return null;

                init.LoadInit();
                yield return null;

                var agent = init.GetComponent<NavMeshAgent>();
                agent.avoidancePriority = priority;

                priority += 4;
                
                init.FinalInit();
            }
        }

        GameObject gObj = Object.Instantiate(DataManager.Instance.mainChar.GetPrefab(99999), SpawnPoint.GetSpawnPoint(LCM.Waypoints[0].transform.position), Quaternion.identity);
        gObj.transform.localScale = new Vector3(3, 3, 3);
        gObj.tag = "CocoDoogy";
        gObj.SetActive(false);
        var cocoInit = gObj.AddComponent<CocoDoogyBehaviour>();
        lobbyCharacter.Add(cocoInit);

        GameObject gObj2 = Object.Instantiate(DataManager.Instance.mainChar.GetPrefab(99998), SpawnPoint.GetSpawnPoint(LCM.Waypoints[0].transform.position), Quaternion.identity);
        gObj2.transform.localScale = new Vector3(3, 3, 3);
        gObj2.tag = "Master";
        gObj2.SetActive(false);
        var masterInit = gObj2.AddComponent<MasterBehaviour>();
        lobbyCharacter.Add(masterInit);

        foreach (var lC in lobbyCharacter)
        {
            // if (cocoInit)
            // {
            //     gObj.AddComponent<CocoDoogyBehaviour>();
            // }
            // else if (masterInit)
            // {
            //     gObj2.AddComponent<MasterBehaviour>();
            // }
            var mono = lC as BaseLobbyCharacterBehaviour;
            if (mono.CompareTag("CocoDoogy") || mono.CompareTag("Master"))
            {
                lC.Init();
                yield return null;

                lC.PostInit();
                yield return null;

                lC.LoadInit();
                yield return null;

                lC.FinalInit();
            }
        }

        coco = cocoInit;
        master = masterInit;
    }
    public CocoDoogyBehaviour CocoInit()
    {
        return coco;
    }
    public MasterBehaviour MasterInit()
    {
        return master;
    }
}
