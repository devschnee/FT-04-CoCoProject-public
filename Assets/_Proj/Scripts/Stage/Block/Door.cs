using System.Collections;
using UnityEngine;


//251031 - 연결 자체는 잘 되는 것 확인함.
//문이 열리는 로직만 잘 구성하면 될 것같다.
//ISignalSender를 구현한 다른 클래스(터렛, 충격감지탑)의 연결 처리도 같은 방식으로 연결하면 문제없이 작동할 것임.
public class Door : Block, ISignalReceiver
{
    public bool IsOn { get; set; }

    public float openSpeed = 1f;

    public void ReceiveSignal()
    {
        IsOn = !IsOn;
        
        // KHJ - 디버깅으로만 테스트 좀 해볼게요
        Debug.Log($"[Door] 문{(IsOn ? "열림" : "닫힘")}");
        RotateDoor(IsOn);
        //if (IsOn)
        //{
        //    //TODO: 문이 열리는 로직을 여기에 집어넣기
        //}
        //else
        //{
        //    //TODO: 문이 닫히는 로직을 여기에 집어넣기
        //}
    }

    void RotateDoor(bool isOn)
    {
        Transform doorTransform = transform.Find("door_metal_left");
        //float startRotation = isOn ? 0 : 90;
        float targetRotation = isOn ? 90 : -90;
        //doorTransform.Rotate(0, startRotation, 0);
        doorTransform.Rotate(0, targetRotation, 0);
    }
    protected override void OnEnable()
    {
        base.OnEnable();
    }
}
