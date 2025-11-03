using UnityEngine;

public class TurretBlock : Block, ISignalSender
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

    
}
