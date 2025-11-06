using System.Collections.Generic;
using UnityEngine;

public class NormalBlock : Block, IEdgeColliderHandler
{
    public LayerMask groundLayer;
    public float rayOffsetY;
    public List<Collider> TransparentColliders { get => transparentColliders; }
    public LayerMask GroundLayer { get => groundLayer; }

    public List<Collider> transparentColliders;
    public float RayOffsetY { get => rayOffsetY; }

    void Awake()
    {
        //groundLayer = LayerMask.GetMask("Ground", "Slope", "Pushables", "Wall");
    }
    public void Inject()
    {
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
