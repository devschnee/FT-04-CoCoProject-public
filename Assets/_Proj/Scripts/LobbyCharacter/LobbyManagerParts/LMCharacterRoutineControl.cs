using UnityEngine;

public class LMCharacterRoutineControl
{
    private CocoDoogyBehaviour coco;
    private MasterBehaviour master;
    private ILobbyCharactersEmotion emotion;

    public LMCharacterRoutineControl(CocoDoogyBehaviour coco, MasterBehaviour master)
    {
        this.coco = coco;
        this.master = master;
        Debug.Log($"coco : {this.coco.gameObject.name}");
        Debug.Log($"master : {this.master.gameObject.name}");
    }


}
