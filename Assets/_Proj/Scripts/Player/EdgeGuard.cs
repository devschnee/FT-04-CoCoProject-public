using UnityEngine;
using System;

public class EdgeGuard : MonoBehaviour
{
    [Header("충돌 감지 설정")]
    [Tooltip("검출 대상으로 허용할 레이어 (예: 고정X 레이어)")]
    public LayerMask targetLayerMask;

    [Tooltip("BoxCast를 쏠 방향 벡터 (인스펙터에서 수동 설정 필요)")]
    public Vector3 castDirection = Vector3.forward; // 각 Guard의 바깥 방향으로 설정

    [Tooltip("콜라이더 크기에 대한 BoxCast 크기 비율 (1.0 미만으로 설정)")]
    [Range(0.01f, 0.99f)] public float castSizeMultiplier = 0.95f;

    [Tooltip("BoxCast를 쏠 최대 거리 (아주 짧게 설정)")]
    [Range(0.01f, 0.5f)] public float castDistance = 0.1f;

    private BoxCollider boundaryCollider;
    private Vector3 halfExtents; // BoxCollider의 절반 크기
    private Type selfType;

    private void Awake()
    {
        boundaryCollider = GetComponent<BoxCollider>();
        if (boundaryCollider == null)
        {
            enabled = false;
            return;
        }

        halfExtents = Vector3.Scale(boundaryCollider.size, transform.lossyScale) * 0.5f;
        selfType = typeof(EdgeGuard);
    }

    private void FixedUpdate()
    {
        UpdateTriggerState();
    }

    private void UpdateTriggerState()
    {
        Vector3 castDirLocal = castDirection.normalized;
        
        Vector3 castHalfExt = new Vector3(
            // castDirection이 X축이면 Y, Z 축을 줄임
            castDirLocal.x != 0 ? halfExtents.x : halfExtents.x * castSizeMultiplier,
            // castDirection이 Y축이면 X, Z 축을 줄임
            castDirLocal.y != 0 ? halfExtents.y : halfExtents.y * castSizeMultiplier,
            // castDirection이 Z축이면 X, Y 축을 줄임
            castDirLocal.z != 0 ? halfExtents.z : halfExtents.z * castSizeMultiplier
        );
        
        Vector3 castDirWorld = transform.TransformDirection(castDirection);
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
            
            bool isTargetLayer = (targetLayerMask.value & (1 << hitCollider.gameObject.layer)) != 0;

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
        BoxCollider currCollider = GetComponent<BoxCollider>();
        if (currCollider == null) return;

        Vector3 currHalfExt = Vector3.Scale(currCollider.size, transform.lossyScale) * 0.5f;

        Vector3 castHalfExtents = currHalfExt * castSizeMultiplier;
        Vector3 castCenter = currCollider.bounds.center;
        Vector3 castEnd = castCenter + transform.TransformDirection(castDirection) * castDistance;

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