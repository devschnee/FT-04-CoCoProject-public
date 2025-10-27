using UnityEngine;

public class PushableBox : PushableObjects
{
    // 박스 충돌 검사
    protected override bool CheckBlocking(Vector3 target)
    {
        return Physics.CheckBox(target + Vector3.up * 0.5f, Vector3.one * 0.4f, Quaternion.identity, blockingMask);
    }
}
