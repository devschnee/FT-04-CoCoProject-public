using UnityEngine;

public class PushableBox : PushableObjects
{
    private BoxCollider boxCol;

    protected override void Awake()
    {
        base.Awake();
        boxCol = GetComponent<BoxCollider>();
    }

    protected override bool CheckBlocking(Vector3 target)
    {
        var b = boxCol.bounds;
        Vector3 half = b.extents - Vector3.one * 0.005f;
        Vector3 center = new Vector3(target.x, target.y + b.extents.y, target.z);

        // 규칙상 차단 (blocking)
        if (Physics.CheckBox(center, half, transform.rotation, blockingMask, QueryTriggerInteraction.Ignore))
            return true;

        // 점유 차단(허용 레이어 제외)
        var hits = Physics.OverlapBox(center, half, transform.rotation, ~throughLayer, QueryTriggerInteraction.Ignore);
        foreach (var c in hits)
        {
            //if ((groundMask.value & (1 << c.gameObject.layer)) != 0) continue;
            if (rb && c.attachedRigidbody == rb) continue; // 자기 자신
            if (c.transform.IsChildOf(transform)) continue; // 자식
            return true;
        }

        return false;
    }
}
