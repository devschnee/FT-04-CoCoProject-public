using UnityEngine;

public class Switch : Block, ISignalSender
{
    public ISignalReceiver Receiver { get; set; }
    


    public void ConnectReceiver(ISignalReceiver receiver)
    {
        Receiver = receiver;
    }

    public void SendSignal()
    {
        Receiver.ReceiveSignal();
    }
    protected override void OnEnable()
    {
        base.OnEnable();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"{name}:트리거 입장 감지");
        if (other.gameObject.TryGetComponent<Block>(out Block block))
        {
            print($"{name}: 들어온 오브젝트({other.name})에 Block 컴포넌트({(block.GetType())}) 확인.");
            SendSignal();
         }

    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"{name}:트리거 퇴장 감지");
        if (other.gameObject.TryGetComponent<Block>(out Block block))
        {
            print($"{name}: 나간 오브젝트({other.name})에 Block 컴포넌트({(block.GetType())}) 확인.");
            SendSignal();
        }

    }
}
