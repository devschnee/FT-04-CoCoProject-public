using System.Collections.Generic;
using UnityEngine;

public class TortoiseBlock : Block, IEdgeColliderHandler
{
    public List<Collider> transparentColliders;
    public LayerMask groundLayer;
    public float rayOffsetY;

    public List<Collider> TransparentColliders => transparentColliders;

    public LayerMask GroundLayer => groundLayer;

    public float RayOffsetY => rayOffsetY;

    public void Inject()
    {
        
    }

    //public void DetectAndApplyFourEdge()
    //{
    //    for (int i = 0; i < 4; i++)
    //    {
    //        Vector3 rayOrigin = transform.position - (Vector3.up * 1.49f);
    //        Vector3 dir = EnumToDir((FourDir)i);
    //        Ray ray = new Ray(rayOrigin, dir);

    //        var results = Physics.RaycastAll(ray, 1.49f, groundLayer);

    //        foreach (RaycastHit hit in results)
    //        {
    //            print(hit.collider.gameObject.layer);
    //        }

    //        if (results.Length < 1)
    //        //그라운드레이어로 취급되는 오브젝트가 아무것도 검출되지 않았다는 뜻.
    //        {

    //            transparentColliders[i].gameObject.SetActive(true);
    //        }
    //        else
    //        {

    //            transparentColliders[i].gameObject.SetActive(false);

    //        }



    //    }
    //}
}
