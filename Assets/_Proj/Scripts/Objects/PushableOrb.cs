using UnityEngine;

public class PushableOrb : PushableObjects
{
    private float sphereRadius = 0.5f;

    protected override void Awake()
    {
        base.Awake();

        SphereCollider sc = GetComponent<SphereCollider>();
        if(sc != null) sphereRadius = sc.radius;

        // 구체는 경사로 허용
        allowSlope = true;
    }

    // 구체 충돌 검사
    protected override bool CheckBlocking(Vector3 target)
    {
        return Physics.CheckSphere(target, sphereRadius * 0.9f, blockingMask);
    }
}
