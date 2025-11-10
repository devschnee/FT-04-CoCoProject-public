using System.Collections;
using UnityEngine;

public class MasterBehaviour : BaseLobbyCharacterBehaviour
{
    //private int currentDecoIndex;
    //private Transform targetDeco;

    protected override void InitStates()
    {
        throw new System.NotImplementedException();
    }

    protected override void Awake()
    {
        base.Awake();
        agent.avoidancePriority = 98;
        //currentDecoIndex = 0;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    // 인터페이스 영역
    public override void OnCocoAnimalEmotion()
    {
        
    }
    public override void OnCocoMasterEmotion()
    {

    }
    public override void OnLobbyBeginDrag(Vector3 position)
    {
        base.OnLobbyBeginDrag(position);
    }
    public override void OnLobbyDrag(Vector3 position)
    {
        base.OnLobbyDrag(position);
    }
    public override void OnLobbyEndDrag(Vector3 position)
    {
        base.OnLobbyEndDrag(position);
    }
    public override void OnLobbyClick()
    {
        base.OnLobbyClick();
        charAnim.ClickMaster();
    }
    public override void OnLobbyPress()
    {
        base.OnLobbyPress();
    }
    public override void InNormal()
    {
        base.InNormal();
    }
    public override void InEdit()
    {
        base.InEdit();
    }
    public override void Register()
    {
        base.Register();
    }
    public override void Unregister()
    {
        base.Unregister();
    }
    public override void Init()
    {
        base.Init();
        agent.avoidancePriority = 10;
    }
    public override void PostInit()
    {
        base.PostInit();
    }
}
