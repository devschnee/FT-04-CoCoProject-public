using System.Collections.Generic;
using UnityEngine;

public enum FourDir

{
    Forward, Left, Backward, Right
}
public interface IEdgeColliderHandler
{
    public List<Collider> TransparentColliders { get; set; }

    void Inject();

    void Inspect();
}
