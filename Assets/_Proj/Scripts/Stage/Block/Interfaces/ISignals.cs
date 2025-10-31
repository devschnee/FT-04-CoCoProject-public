using System.Collections.Generic;

public interface ISignalReceiver
{
    bool IsOn { get; set; }
    public void ReceiveSignal();
}

public interface ISignalSender
{
    ISignalReceiver Receiver { get; set; }

    void SendSignal();

    void ConnectReceiver(ISignalReceiver receiver);
}
