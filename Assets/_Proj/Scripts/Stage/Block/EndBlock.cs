using UnityEngine;

public class EndBlock : Block
{
    IStageManager stage;
    protected override void OnEnable()
    {
        base.OnEnable();
    }

    public void Init(IStageManager stage)
    {
        this.stage = stage;
    }


    //???????????
    public void OnTriggerEnter(Collider collision)
    {
        Debug.Log("충돌 감지되긴 함");


        if (collision.CompareTag("Player"))
        {
            if (stage is StageManager stageM)
            {
                stageM.ClearStage();
                
            }
            else if (stage is TutorialStageManager tutorial)
            {
                tutorial.ClearStage();
                
            }
        }
    }

}
