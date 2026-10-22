namespace EduLab.Shared.Enums
{
    public enum CommandType : ushort
    {
        // 1 - 9: Heartbeat & Health Check
        Ping = 1,
        Pong = 2,

        // 10 - 19: Handshake & Identity
        RegisterClient = 10,
        ClientRegistered = 11,
        StudentCheckIn = 12,

        // 20 - 29: Lock / Unlock Security
        LockScreen = 20,
        UnlockScreen = 21,
        LockInput = 22,
        UnlockInput = 23,

        // 30 - 39: Power Control
        PowerShutdown = 30,
        PowerRestart = 31,
        PowerLogoff = 32,

        // 40 - 49: Messaging & Notification
        BroadcastMessage = 40,
        SendPrivateMessage = 41,

        // 50 - 59: Screen Monitoring & Streaming
        RequestThumbnail = 50,
        ThumbnailData = 51,
        StartHighQualityStream = 52,
        StopHighQualityStream = 53,
        ScreenStreamData = 54,

        // 60 - 69: File Distribution & Collection
        SendFileStart = 60,
        SendFileChunk = 61,
        SendFileComplete = 62,
        SubmitFileStart = 63,
        SubmitFileChunk = 64,
        SubmitFileComplete = 65,

        // 70 - 79: Restrictions
        BlockInternet = 70,
        UnblockInternet = 71,
        KillProcess = 72
    }
}
