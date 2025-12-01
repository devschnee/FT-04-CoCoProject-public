using UnityEngine;

public class TowerBlock : Block, ISignalSender
{
    public ISignalReceiver Receiver { get; set; }

    public void ConnectReceiver(ISignalReceiver receiver)
    {
        Receiver = receiver;
    }

    public void SendSignal()
    {
        // LSH 추가 1201
        AudioEvents.Raise(SFXKey.InGameObject, 6, pooled: true, pos: transform.position);
        Receiver.ReceiveSignal();
    }
}
