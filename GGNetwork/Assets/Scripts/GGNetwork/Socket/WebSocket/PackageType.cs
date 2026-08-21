namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 协议包类型（与 Pomelo 协议定义一致）。
    /// </summary>
    public enum PackageType
    {
        PKG_HANDSHAKE = 1,
        PKG_HANDSHAKE_ACK = 2,
        PKG_HEARTBEAT = 3,
        PKG_DATA = 4,
        PKG_KICK = 5,
    }
}
