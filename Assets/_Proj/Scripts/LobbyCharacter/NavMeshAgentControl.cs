using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshAgentControl
{
    // 필요한 기본 이동 로직들을 만들고 각 스크립트에 조립하는 형식이 좋겠지? 코루틴을 사용하는게 더 좋으니
    private readonly NavMeshAgent agent;
    private float moveSpeed; // agent 스피드
    private float angularSpeed; // agent 회전 스피드
    private float acceleration; // agent 가속
    private float moveRadius; // Random.insideUnitSphere 범위 설정

    public NavMeshAgentControl(NavMeshAgent agent, float moveSpeed, float angularSpeed, float acceleration, float moveRadius)
    {
        this.agent = agent;
        this.moveSpeed = moveSpeed;
        this.angularSpeed = angularSpeed;
        this.acceleration = acceleration;
        this.moveRadius = moveRadius;
    }

    // 캐릭터 움직일 때 걷거나 뛰는 애니메이션 속도 값 리턴 // 이젠사용안할듯?
    public float ValueOfMagnitude()
    {
        return agent.velocity.magnitude;
    }

    // 속도 변경 시 적용인데 아직 쓸 일이 없을 듯
    public void MoveValueChanged()
    {
        if (agent.speed != moveSpeed) agent.speed = moveSpeed;
        if (agent.angularSpeed != angularSpeed) agent.angularSpeed = angularSpeed;
        if (agent.acceleration != acceleration) agent.acceleration = acceleration;
    }

    /// <summary>
    /// Trans일 시 위치 이동
    /// </summary>
    /// <param name="point"></param>
    public void MoveToTransPoint(Transform point)
    {
        if (agent.isStopped) agent.isStopped = false;
        Vector3 pos = point.position;
        float speed = Random.Range(2.4f, 6f);
        agent.SetDestination(pos);
        agent.speed = speed;
        agent.acceleration = Random.Range(speed, 10f);
        //agent.stoppingDistance = Random.Range(0f, 0.5f);
    }
    /// <summary>
    /// Vector3일 시 위치 이동
    /// </summary>
    /// <param name="point"></param>
    public void MoveToVectorPoint(Vector3 point)
    {
        if (agent.isStopped) agent.isStopped = false;
        float speed = Random.Range(2.4f, 6f);
        agent.SetDestination(point);
        agent.speed = speed;
        agent.acceleration = Random.Range(speed, 10f);
        //agent.stoppingDistance = Random.Range(0f, 0.5f);
    }
    /// <summary>
    /// 마지막 포인트 지점일 시 느리게 이동(필요 없을 수 있음)
    /// </summary>
    /// <param name="point"></param>
    public void MoveToLastPoint(Transform point)
    {
        if (agent.isStopped) agent.isStopped = false;
        agent.speed = 2f;
        agent.acceleration = 8f;
        agent.autoBraking = true;
        agent.stoppingDistance = 0f;
        Vector3 randomDir = point.position + Random.insideUnitSphere * 0.3f;
        randomDir.y = point.position.y;
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, 0.1f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
    /// <summary>
    /// 해당 포인트를 기준으로 랜덤 범위 이동
    /// </summary>
    /// <param name="point"></param>
    public void MoveToRandomTransPoint(Transform point)
    {
        if (agent.isStopped) agent.isStopped = false;
        Vector3 randomDir = point.position + Random.insideUnitSphere * moveRadius;
        randomDir.y = point.position.y;
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, 0.1f, NavMesh.AllAreas))
        {
            MoveToVectorPoint(hit.position);
        }
    }
    // 코코두기 전용
    public void CocoMove(Vector3 point)
    {
        if (agent.isStopped) agent.isStopped = false;
        float speed = 7f;
        agent.speed = speed;
        agent.acceleration = Random.Range(speed * 1.5f, speed * 3f);
        agent.angularSpeed = Random.Range(190, 300);
        agent.autoBraking = false;
        agent.stoppingDistance = Random.Range(0f, 1f);
        agent.SetDestination(point);
    }
    // 마스터 전용
    public void MasterMove(Vector3 point)
    {
        if (agent.isStopped) agent.isStopped = false;
        float speed = 4f;
        agent.speed = speed;
        agent.acceleration = Random.Range(speed * 1.5f, speed * 3f);
        agent.angularSpeed = Random.Range(190, 300);
        agent.autoBraking = false;
        agent.stoppingDistance = Random.Range(0f, 1f);
        agent.SetDestination(point);
    }

    public void CocoMoveToRandomTransPoint(BaseLobbyCharacterBehaviour owner, Transform point)
    {
        if (agent.isStopped) agent.isStopped = false;
        Vector3 pos = point.position + Random.onUnitSphere * Random.Range(12f, 20f);
        Vector3 ownerPos = owner.transform.position;
        pos.y = ownerPos.y;
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 6f, NavMesh.AllAreas))
        {
            CocoMove(hit.position);
        }
    }
    public void MasterMoveToRandomTransPoint(BaseLobbyCharacterBehaviour owner, Transform point)
    {
        if (agent.isStopped) agent.isStopped = false;
        Vector3 pos = point.position + Random.onUnitSphere * 25f;
        Vector3 ownerPos = owner.transform.position;
        pos.y = ownerPos.y;
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 0.3f, NavMesh.AllAreas))
        {
            MasterMove(hit.position);
        }
    }
    

    public void RestAgent()
    {
        if (agent.enabled == false) agent.enabled = true;
        if (agent.isStopped) agent.isStopped = false;
    }
}
