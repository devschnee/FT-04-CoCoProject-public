using UnityEngine;

// 10/30 TODO : Shockwave 사용해서 버팔로와 같이 충격파 발생시킬 수 있도록 수정.
// 버팔로와 다르게 낙하 시점에 충격파를 발생시켜야 함. -> 감지탑에 충격파 발생 여부 전달
// 물체에 의해 막힐 수 있음.
// 낙하하고 있다는 걸 어떻게 알지? 버팔로 뿐 아니라 그냥 절벽에서 떨어지더라도 충격파 발생됨.
// 충격파 발생 shockwave.Fire();

public class PushableOrb : PushableObjects
{
    SphereCollider sph;

    [SerializeField] private Shockwave shockwave;
    [Tooltip("쿨타임")]
    public float coolTime = 5f;

    protected override void Awake()
    {
        base.Awake();
        sph = GetComponent<SphereCollider>(); 
        allowSlope = true;
        shockwave = GetComponent<Shockwave>();
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
