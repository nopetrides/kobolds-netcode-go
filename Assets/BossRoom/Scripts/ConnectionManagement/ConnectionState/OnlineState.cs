namespace Unity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// Base class representing an online connection state.
    /// </summary>
    abstract class OnlineState : ConnectionState
    {
        public override void OnUserRequestedShutdown()
        {
            // This behaviour will be the same for every online state
            MConnectStatusPublisher.Publish(ConnectStatus.UserRequestedDisconnect);
            MConnectionManager.ChangeState(MConnectionManager.MOffline);
        }

        public override void OnTransportFailure()
        {
            // This behaviour will be the same for every online state
            MConnectionManager.ChangeState(MConnectionManager.MOffline);
        }
    }
}
