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
        SendSignal();

    }

}
