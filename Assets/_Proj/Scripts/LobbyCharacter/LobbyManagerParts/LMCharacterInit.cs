using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LMCharacterInit
{
    private readonly LobbyCharacterManager LCM;
    private CocoDoogyBehaviour coco;
    private MasterBehaviour master;
    private List<ILobbyState> lobbyCharacter = new();

    public LMCharacterInit(LobbyCharacterManager LCM, List<ILobbyState> lobbyCharacter)
    {
        this.LCM = LCM;
        this.lobbyCharacter = lobbyCharacter;
    }
    public void Init()
    {
        GameObject gObj = Object.Instantiate(DataManager.Instance.mainChar.GetPrefab(99999), LCM.Waypoints[0].transform.position, Quaternion.identity);
        gObj.transform.localScale = new Vector3(3, 3, 3);
        gObj.AddComponent<CocoDoogyBehaviour>();

        GameObject gObj2 = Object.Instantiate(DataManager.Instance.mainChar.GetPrefab(99998), LCM.Waypoints[0].transform.position, Quaternion.identity);
        gObj2.transform.localScale = new Vector3(3, 3, 3);
        gObj2.AddComponent<MasterBehaviour>();

        int priority = 50;
        foreach (var lC in lobbyCharacter)
        {
            var mono = lC as BaseLobbyCharacterBehaviour;
            if (mono.CompareTag("CocoDoogy"))
            {
                var cocoB = lC as CocoDoogyBehaviour;
                coco = cocoB;
            }
            else if (mono.CompareTag("Master"))
            {
                var masterB = lC as MasterBehaviour;
                master = masterB;
            }
            else if (mono.CompareTag("Animal"))
            {
                var agent = mono.GetComponent<NavMeshAgent>();
                agent.avoidancePriority = priority;
            }
            lC.Init();
            lC.PostInit();
            lC.LoadInit();
            lC.FinalInit();
            priority += 4;
        }
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
