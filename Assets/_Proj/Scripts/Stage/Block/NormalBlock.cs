using System.Collections.Generic;
using UnityEngine;

public class NormalBlock : Block, IEdgeColliderHandler
{
    public LayerMask groundLayer;

    public List<Collider> TransparentColliders { get => transparentColliders; set => transparentColliders = value; }
    public Collider Left { get; set; }
    public Collider Backward { get; set; }
    public Collider Right { get; set; }

    public List<Collider> transparentColliders;
    private Vector3 EnumToDir(FourDir dir)
    {

        return dir == FourDir.Forward ? Vector3.forward :
               dir == FourDir.Left ? Vector3.left :
               dir == FourDir.Backward ? Vector3.back :
               Vector3.right;
    }

    void Awake()
    {
        groundLayer = LayerMask.GetMask("Ground", "Slope", "Pushable", "Wall");
    }
    public void Inject()
    {
    }

    public void Inspect()
    {
        for (int i = 0; i < 4; i++)
        {

            Vector3 dir = EnumToDir((FourDir)i);
            Ray ray = new Ray(transform.position, dir);
            
            var results = Physics.RaycastAll(ray, 1, groundLayer);

            foreach (RaycastHit hit in results)
            {
                print(hit.collider.gameObject.layer);
            }
            if (results.Length < 1)
            //그라운드레이어로 취급되는 오브젝트가 아무것도 검출되지 않았다는 뜻.
            {
                transparentColliders[i].gameObject.SetActive(true);
            }
        }
    }
    protected override void OnEnable() 
    {
        base.OnEnable();
        //isGround = true;
        //isStackable = true;
        //isStatic = true;
        //isOverlapping = false;
    }
}
