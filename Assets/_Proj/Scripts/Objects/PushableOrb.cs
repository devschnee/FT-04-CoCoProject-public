using UnityEngine;

public class PushableOrb : PushableObjects
{
    SphereCollider sph;
    protected override void Awake()
    {
        base.Awake();
        sph = GetComponent<SphereCollider>(); 
        allowSlope = true;
    }

    protected override bool CheckBlocking(Vector3 target)
    {
        float r = sph.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z) - 0.005f;
        Vector3 center = new(target.x, target.y + r, target.z);

        if (Physics.CheckSphere(center, r, blockingMask, QueryTriggerInteraction.Ignore))
            return true;

        var hits = Physics.OverlapSphere(center, r, ~throughLayer, QueryTriggerInteraction.Ignore);
        foreach (var c in hits)
        {
            // if ((groundMask.value & (1 << c.gameObject.layer)) != 0) continue; // 필요 시 바닥 제외
            if (c.transform.IsChildOf(transform)) continue;
            return true;
        }
        return false;
    }
}
