using System.Collections;
using UnityEngine;

public class Door : Block, ISignalReceiver
{
    public bool IsOn { get; set; }

    public float openSpeed = 1f;

    public void ReceiveSignal()
    {
        StopAllCoroutines();
        IsOn = !IsOn;
        StartCoroutine(OpenCloseCoroutine(IsOn));
        //if (IsOn)
        //{
        //    //TODO: 문이 열리는 로직을 여기에 집어넣기
        //}
        //else
        //{
        //    //TODO: 문이 닫히는 로직을 여기에 집어넣기
        //}
    }

    IEnumerator OpenCloseCoroutine(bool isOn)
    {
        Transform doorTransform = transform.Find("door_metal_left");
        float targetRotation = isOn ? 90 : 0;
        float currentRotation = doorTransform.rotation.eulerAngles.y;
        while (!Mathf.Approximately(currentRotation, targetRotation))
        {
            doorTransform.Rotate(new(0, (targetRotation - currentRotation) * Time.deltaTime * openSpeed, 0));
            yield return null;
        }
    }
    protected override void OnEnable()
    {
        base.OnEnable();
    }
}
