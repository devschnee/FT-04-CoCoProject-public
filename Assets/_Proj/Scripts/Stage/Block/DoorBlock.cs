using System.Collections;
using UnityEngine;


//251031 - 연결 자체는 잘 되는 것 확인함.
//문이 열리는 로직만 잘 구성하면 될 것같다.
//ISignalSender를 구현한 다른 클래스(터렛, 충격감지탑)의 연결 처리도 같은 방식으로 연결하면 문제없이 작동할 것임.
public class DoorBlock : Block, ISignalReceiver
{
    public bool IsOn { get; set; }

    public float openSpeed = 1f;
    Transform left;
    Transform right;
    public void ReceiveSignal()
    {
        IsOn = !IsOn;
        
        // KHJ - 디버깅으로만 테스트 좀 해볼게요
        Debug.Log($"[Door] 문{(IsOn ? "열림" : "닫힘")}");
        ToggleDoor(IsOn);
        //if (IsOn)
        //{
        //    //TODO: 문이 열리는 로직을 여기에 집어넣기
        //}
        //else
        //{
        //    //TODO: 문이 닫히는 로직을 여기에 집어넣기
        //}
    }

    void Awake()
    {
        
        if (!left)
            left = transform.GetChild(0).GetChild(1);
        if (!right)
            right = transform.GetChild(0).GetChild(2);
    }

    void ToggleDoor(bool isOn)
    {
        

        float xOffset = .75f;
        left.Translate(isOn ? new(xOffset, 0, 0) : new(-xOffset, 0, 0));
        right.Translate(isOn ? new(-xOffset, 0, 0) : new(xOffset, 0, 0));
    }
    protected override void OnEnable()
    {
        base.OnEnable();
    }
}
