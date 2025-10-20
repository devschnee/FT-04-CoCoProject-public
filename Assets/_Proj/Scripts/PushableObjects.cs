using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushableObjects : MonoBehaviour
{
    public float tileSize = 1f;

    public float slideSpeed = 5f;
    public float settleEps = 0.0005f;

    public bool IsMoving { get; private set; }

    Rigidbody rb;
    Collider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.MovePosition(Snap(transform.position));
    }

    public void SlideOneCell(Vector3 cellCenter)
    {
        if (IsMoving) return;
        StopAllCoroutines();
        StartCoroutine(SlideCoroutine(cellCenter));
    }

    IEnumerator SlideCoroutine(Vector3 target)
    {
        IsMoving = true;
        while((rb.position - target).sqrMagnitude > settleEps)
        {
            Vector3 next = Vector3.MoveTowards(rb.position, target, slideSpeed * Time.fixedDeltaTime);
            rb.MovePosition(next);
            yield return new WaitForFixedUpdate();
        }
        rb.MovePosition(target);
        IsMoving = false;
    }

    public Vector3 Snap(Vector3 p)
    {
        float gx = Mathf.Round(p.x / tileSize) * tileSize;
        float gz = Mathf.Round(p.z / tileSize) * tileSize;
        return new Vector3(gx, p.y, gz);
    }

    public Vector3 GetHalfExtents()
    {
        if (!col) return new Vector3(0.45f, 0.5f, 0.45f);
        var b = col.bounds;
        return b.extents * 0.95f;
    }
}
