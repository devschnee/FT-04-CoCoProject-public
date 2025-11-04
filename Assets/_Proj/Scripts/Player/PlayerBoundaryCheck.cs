using System;
using UnityEngine;

public class PlayerBoundaryCheck : MonoBehaviour
{
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public LayerMask movableObjLayer;
    public float checkDist = 0.3f;
    public float groundProbeRadius = 0.2f;

    public (Vector3, Vector3) Execute(Vector3 moveDir, Rigidbody rb, PlayerMovement p)
    {
        if (moveDir.sqrMagnitude < 0.0001f)
        {
            return (moveDir, Vector3.zero);
        }

        Vector3 targetDir = moveDir;
        Vector3 stepOffset = Vector3.zero;

        if (IsFallingOff(rb, targetDir))
        {
            targetDir = Vector3.zero;
        }

        if(CheckFwd(rb, targetDir, out RaycastHit hit))
        {
            if((wallLayer.value & (1 << hit.collider.gameObject.layer)) != 0)
            {
                targetDir = Vector3.zero;
            }
        }
        return (targetDir, Vector3.zero);
    }

    private bool IsFallingOff(Rigidbody rb, Vector3 dir)
    {
        Vector3 checkOrigin = rb.position + dir.normalized * checkDist + Vector3.up * 0.01f;

        if (Physics.SphereCast(
            checkOrigin,
            groundProbeRadius,
            Vector3.down,
            out _,
            1f,
            groundLayer))
        {
            return false;
        }

        return true;
    }

    private bool CheckFwd(Rigidbody rb, Vector3 dir, out RaycastHit hit)
    {
        Vector3 center = rb.position + Vector3.up * 0.5f;
        Vector3 halfExtents = new Vector3(0.4f, 0.45f, 0.4f);

        return Physics.BoxCast(
            center,
            halfExtents,
            dir.normalized,
            out hit,
            rb.rotation,
            checkDist * 1.5f,
            wallLayer | movableObjLayer);
    }
}
