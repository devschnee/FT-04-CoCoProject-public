using UnityEngine;
using System;

// 콜라이더 뚫고 지나가는 문제 있음. -> 특정 조건일 때 Joystick의 input자체를 0으로 만들어버리는 방법 고려.
// 이동O인 물체에 다 붙여줘야 할 듯?
// HACK : 하면 할수록 비효율적인 방법 같음;
// y+1에 콜라이더 설치하고 스크립트는 기준이 되는 box에 하나만 붙인 다음에 각 방향 콜라이더를 awake에서 할당한 후에 감지되는 방향의 콜라이더를 isTrigger로 해주는 방식이 더 낫지 않나?
// 감지는 기준 box 위치에서 한 후, 감지하는 레이어가 Pushables, Ground, TransparentWall(필요 없어질 레이어이지 않을까...), Water, MovingWater, Animals(터틀, 보어만 해당(버팔로는 제자리에 있음))  <- 이 정도?

public class EdgeGuard : MonoBehaviour
{
    [Header("충돌 감지 설정")]
    [Tooltip("검출 대상으로 허용할 레이어 (예: 고정X 레이어)")]
    public LayerMask targetLayer;

    [Tooltip("BoxCast를 쏠 방향 벡터 (인스펙터에서 수동 설정 필요)")]
    public Vector3 castDir = Vector3.forward; // 각 Guard의 바깥 방향으로 설정

    [Tooltip("콜라이더 크기에 대한 BoxCast 크기 비율 (1.0 미만으로 설정)")]
    [Range(0.01f, 0.99f)] public float castSizeMultiplier = 0.45f;

    [Tooltip("BoxCast를 쏠 최대 거리 (아주 짧게 설정)")]
    [Range(0.01f, 0.5f)] public float castDistance = 0.1f;

    private BoxCollider boundaryCollider;
    private Vector3 halfExt; // BoxCollider의 절반 크기
    private Type selfType;

    private void Awake()
    {
        boundaryCollider = GetComponent<BoxCollider>();
        if (boundaryCollider == null)
        {
            enabled = false;
            return;
        }

        halfExt = Vector3.Scale(boundaryCollider.size, transform.lossyScale) * 0.5f;
        selfType = typeof(EdgeGuard);
    }

    private void FixedUpdate()
    {
        UpdateTriggerState();
    }

    private void UpdateTriggerState()
    {
        Vector3 castDirLocal = castDir.normalized;
        
        Vector3 castHalfExt = new Vector3(
            // castDirection이 X축이면 Y, Z 축을 줄임
            castDirLocal.x != 0 ? halfExt.x : halfExt.x * castSizeMultiplier,
            // castDirection이 Y축이면 X, Z 축을 줄임
            castDirLocal.y != 0 ? halfExt.y : halfExt.y * castSizeMultiplier,
            // castDirection이 Z축이면 X, Y 축을 줄임
            castDirLocal.z != 0 ? halfExt.z : halfExt.z * castSizeMultiplier
        );
        
        Vector3 castDirWorld = transform.TransformDirection(castDir);
        Vector3 castCenter = boundaryCollider.bounds.center;

        Collider hitCollider = null;
        bool shouldBeTrigger = false;

        Collider[] overlaps = Physics.OverlapBox(
            castCenter,
            castHalfExt,
            transform.rotation,
            ~0,
            QueryTriggerInteraction.Collide
        );

        foreach (var col in overlaps)
        {
            if (col != boundaryCollider)
            {
                hitCollider = col;
                break;
            }
        }

        RaycastHit boxCastHit;
        if (hitCollider == null && Physics.BoxCast(
            castCenter,
            castHalfExt,
            castDirWorld,
            out boxCastHit,
            transform.rotation,
            castDistance,
            ~0,
            QueryTriggerInteraction.Collide
        ))
        {
            hitCollider = boxCastHit.collider;
        }

        if (hitCollider != null)
        {
            
            bool isTargetLayer = (targetLayer.value & (1 << hitCollider.gameObject.layer)) != 0;

            bool hasSameComponent = hitCollider.GetComponent(selfType) != null;

            if (isTargetLayer || hasSameComponent)
            {
                shouldBeTrigger = true;
            }
        }
        else
        {
        }
        boundaryCollider.isTrigger = shouldBeTrigger;
    }

    private void OnDrawGizmos()
    {
        // Yellow : 충돌 감지 위한 BoxCast 시작 위치와 크기, 감지하련느 물체의 경계
        // Red : BoxCast 최대 도달하는 끝 지점 Box(어디까지 충돌감지하는지)
        // Cyan : 방향, 거리
        BoxCollider currCollider = GetComponent<BoxCollider>();
        if (currCollider == null) return;

        Vector3 currHalfExt = Vector3.Scale(currCollider.size, transform.lossyScale) * 0.5f;

        Vector3 castHalfExtents = currHalfExt * castSizeMultiplier;
        Vector3 castCenter = currCollider.bounds.center;
        Vector3 castEnd = castCenter + transform.TransformDirection(castDir) * castDistance;

        Gizmos.color = Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(castCenter, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, castHalfExtents * 2);

        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(castCenter, castEnd);

        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(castEnd, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, castHalfExtents * 2);
    }
}