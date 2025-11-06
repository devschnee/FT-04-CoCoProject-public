using System.Collections.Generic;
using UnityEngine;

public enum FourDir

{
    Forward, Left, Backward, Right
}
public interface IEdgeColliderHandler
{

    public virtual Vector3 EnumToDir(FourDir dir)
    {
        var mono = this as MonoBehaviour;
        return dir == FourDir.Forward ? mono.transform.forward :
               dir == FourDir.Left ? -mono.transform.right :
               dir == FourDir.Backward ? -mono.transform.forward :
               mono.transform.right;
    }
    public List<Collider> TransparentColliders { get; }
    public LayerMask GroundLayer { get; }
    public float RayOffsetY { get; }
    void Inject();

    virtual void DetectAndApplyFourEdge()
    {
        var mono = this as MonoBehaviour;
        for (int i = 0; i < 4; i++)
        {
            Vector3 rayOrigin = mono.transform.position - (Vector3.up * RayOffsetY);
            Vector3 dir = (this as IEdgeColliderHandler).EnumToDir((FourDir)i);
            Ray ray = new Ray(rayOrigin, dir);

            var results = Physics.RaycastAll(ray, 1.49f, GroundLayer);

            //일단 막아두고
            SetCollider(i);
            if (results.Length > 0) SetCollider(i, false);


            //if (results.Length > 1)
            ////그라운드레이어로 취급되는 오브젝트가 아무것도 검출되지 않았다는 뜻.
            //{

            //    TransparentColliders[i].gameObject.SetActive(true);
            //}
            //else
            //{

            //    TransparentColliders[i].gameObject.SetActive(false);

            //}

        }
    }

    virtual void SetCollider(int index, bool isOn = true) => TransparentColliders[index].gameObject.SetActive(isOn);

    virtual void SetAllCollider(bool isOn = true) => TransparentColliders.ForEach((x)=>SetCollider(TransparentColliders.IndexOf(x), isOn));
    virtual List<IEdgeColliderHandler> DetectGrounds()
    {
        var mono = this as MonoBehaviour;
        List<IEdgeColliderHandler> result = new();

        for (int i = 0; i < 4; i++)
        {
            Vector3 rayOrigin = mono.transform.position - (Vector3.up * RayOffsetY);
            Vector3 dir = EnumToDir((FourDir)i);
            Ray ray = new Ray(rayOrigin, dir);

            var results = Physics.RaycastAll(ray, 1.49f, GroundLayer);

            foreach (RaycastHit hit in results)
            {
                //if (hit.collider.gameObject == mono.gameObject) continue;
                if (hit.collider.TryGetComponent<IEdgeColliderHandler>(out var handler)) result.Add(handler);
            }
        }

        Debug.Log($"IEdgeColliderHandler의 캐싱메서드: {result.Count}개 검출하여 저장.");
        return result;
    }
}
