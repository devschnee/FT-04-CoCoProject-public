using System.Collections;
using UnityEngine;

public interface IPushable
{
    bool RequestPush(Vector3 axis);
    bool IsMoving {  get; }
    float TileSize { get; }
}

[RequireComponent(typeof(Rigidbody))]
public abstract class PushableObjects : MonoBehaviour, IPushable
{
    public float tileSize = 1f;
    public float slideSpeed = 8f;
    public float settleEps = 0.0005f;

    public bool IsMoving {  get; protected set; }
    public float TileSize => tileSize;

    protected Rigidbody rb;
    protected Collider col;
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.MovePosition(Snap(transform.position));
    }

    public virtual Vector3 Snap(Vector3 p)
    {
        float gx = Mathf.Round(p.x / tileSize) * tileSize;
        float gz = Mathf.Round(p.z / tileSize) * tileSize;
        return new Vector3(gx, p.y, gz);
    }

    protected virtual Vector3 GetNextCellCenter(Vector3 currCenter, Vector3 axis)
    {
        return currCenter + axis * tileSize;
    }

    protected abstract bool CanEnterCell(Vector3 nextCenter, Vector3 axis);

    public virtual bool RequestPush(Vector3 axis)
    {
        if (IsMoving) return false;

        axis.y = 0;
        if (Mathf.Abs(axis.x) > Mathf.Abs(axis.z)) axis = new Vector3(Mathf.Sign(axis.x), 0, 0);
        else
           axis = new Vector3(0, 0, Mathf.Abs(axis.z));

        Vector3 curr = Snap(transform.position);
        Vector3 next = GetNextCellCenter(curr, axis);

        if (!CanEnterCell(next, axis)) return false;

        SlideOneCell(next);
        return true;
    }

    IEnumerator SlideCoroutine(Vector3 target)
    {
        IsMoving = true;
        while((rb.position - target).sqrMagnitude > settleEps)
        {
            Vector3 next = Vector3.MoveTowards(rb.position, target, Time.fixedDeltaTime * slideSpeed);
            rb.MovePosition(next);
        }
        yield return new WaitForFixedUpdate();
        rb.MovePosition(target);
        IsMoving = false;
    }

    public void SlideOneCell(Vector3 targetCenter)
    {
        StopAllCoroutines();
        StartCoroutine(SlideCoroutine(targetCenter));
    }
}
