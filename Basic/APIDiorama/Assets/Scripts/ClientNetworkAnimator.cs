using Unity.Netcode.Components;

public class ClientNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; //allows the client to send animations data over the network
    }
}
