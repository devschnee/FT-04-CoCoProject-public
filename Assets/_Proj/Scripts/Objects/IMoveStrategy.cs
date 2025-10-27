using UnityEngine;

public interface IMoveStrategy
{
    // 이동 벡터 받아서 상호작용 처리(밀기, 경사 보정 등)
    // 보정된 이동 벡터와 추가적인 위치 오프셋 반환
    (Vector3, Vector3) Execute(Vector3 moveDir, Rigidbody rb, PlayerMovement player);
}
