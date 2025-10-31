using UnityEngine;

public class EndBlock : Block
{
    StageManager stage;
    protected override void OnEnable()
    {
        base.OnEnable();
    }

    public void Init(StageManager stage)
    {
        this.stage = stage;
    }


    //???????????
    public void OnCollisionEnter(Collision collision)
    {
        Debug.Log("충돌 감지되긴 함");

        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            stage.ClearStage();
        }
    }

}
