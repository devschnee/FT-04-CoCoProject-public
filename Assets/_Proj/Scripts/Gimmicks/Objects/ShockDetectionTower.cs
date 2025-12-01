using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ShockDetectionTower : MonoBehaviour, ISignalSender, ISignalReceiver
{
    [Header("Relay Settings")]
    public float relayRadius = 4f;
    public float relayDelay = 1f;
    public float towerCooldown = 10f;

    [Header("Occlusion")]
    public bool useOcclusion = true;
    public LayerMask occluderMask;
    [Tooltip("신호를 전송시킬 레이어. Door레이어도 포함돼야 함")]
    public LayerMask towerLayer;

    public bool IsOn { get; set; } // 그냥 ISignalReceiver 구현용

    void Awake()
    {
        // 범위 안의 문 자동 탐색
        //var cols = Physics.OverlapSphere(transform.position, relayRadius, ~0);
        //foreach (var c in cols)
        //{
        //    var receiver = c.GetComponentInParent<ISignalReceiver>();
        //    if (receiver != null)
        //    {
        //        Receiver = receiver;
        //        Debug.Log($"[Tower] {name}: 자동으로 가까이에 있는 {receiver} 검색");
        //        break;
        //    }
        //}
    }

    // --- ISignalSender ---
    public ISignalReceiver Receiver { get; set; }

    private bool isCooling = false;


    void Start()
    {
        TryAutoConnect();
    }

    void TryAutoConnect()
    {
        if (Receiver != null) return;

        var cols = Physics.OverlapSphere(transform.position, relayRadius, ~0);
        foreach (var c in cols)
        {
            var receiver = c.GetComponentInParent<ISignalReceiver>();
            if (receiver != null)
            {
                Receiver = receiver;
                Debug.Log($"[Tower] {name}: {receiver} 자동 연결 완료");
                break;
            }
        }
    }

    // 충격파 수신
    public void ReceiveShock(Vector3 origin)
    {
        if (isCooling)
        {
            Debug.Log($"[Tower] {name}: 쿨타임 중, 무시됨");
            return;
        }

        GetComponent<ISignalSender>().SendSignal();
        //if (Receiver == null) TryAutoConnect();
        //Debug.Log($"[Tower] {name}: 충격파 감지!");

        //if (Receiver is DoorBlock door)
        //{
        //    door.OpenPermanently();
        //}
        //else
        //{
        //    // 다른 타워로 신호 릴레이(Door 아닌 경우)
        //    SendSignal();
        //}
        // 쿨타임 진입
        StartCoroutine(CooldownTimer());

        // 주변 탑에 릴레이(전이)
        StartCoroutine(RelayToNearbyTowers(origin));
    }

    // 쿨타임 코루틴
    private IEnumerator CooldownTimer()
    {
        isCooling = true;
        yield return new WaitForSeconds(towerCooldown);
        isCooling = false;
        Debug.Log($"[Tower] {name}: 쿨타임 종료");
    }

    // 충격파 릴레이
    private IEnumerator RelayToNearbyTowers(Vector3 origin)
    {
        yield return new WaitForSeconds(relayDelay);

        var cols = Physics.OverlapSphere(transform.position, relayRadius, towerLayer, QueryTriggerInteraction.Ignore);
        foreach (var c in cols)
        {
            if (c.transform == transform) continue;
            var tower = c.GetComponent<ShockDetectionTower>();
            if (!tower || tower.isCooling) continue;

            // 차폐 검사
            if (useOcclusion)
            {
                Vector3 p0 = transform.position + Vector3.up * 0.5f;
                Vector3 p1 = tower.transform.position + Vector3.up * 0.5f;
                Vector3 dir = p1 - p0; float dist = dir.magnitude;
                if (dist > 0.1f && Physics.Raycast(p0, dir.normalized, dist - 0.05f, occluderMask))
                    continue;
            }

            tower.ReceiveShock(transform.position);
        }
    }

    // --- ISignalSender ---
    public void SendSignal()
    {
        if (Receiver != null)
        {
            if (Receiver is DoorBlock door)
            {
                //door.OpenPermanently();
                // NOTE : ▼ 12/01 기획팀 요청(기획 변경)으로 Tower도 Switch와 같이 일반적인 신호 받도록 변경. 만약 기획이 원래대로 변경된다면 이 라인을 주석처리 하고 윗 라인을 주석 해제해주면 됨.
                Receiver.ReceiveSignal(); 
                Debug.Log($"[Tower] {name}: 문에 신호 전송(영구 열림) 완료");
            }
            else
            {
                // 일반적인 신호 전송
                Receiver.ReceiveSignal();
                Debug.Log($"[Tower] {name}: 수신기({Receiver.GetType().Name})에 신호 전송 완료");
            }
        }
        else
        {
            Debug.Log($"[Tower] {name}: 연결된 수신기가 없음");
        }
    }

    public void ReceiveSignal()
    {
        // 다른 Tower가 나한테 신호 보낼 때
        ReceiveShock(Vector3.zero);
    }

    public void ConnectReceiver(ISignalReceiver receiver)
    {
        Receiver = receiver;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, relayRadius);
    }
#endif
}
