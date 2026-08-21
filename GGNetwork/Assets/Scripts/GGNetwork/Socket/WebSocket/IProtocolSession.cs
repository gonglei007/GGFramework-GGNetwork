namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 协议会话接口，供握手/心跳等协议服务向传输层发送原始协议包。
    /// 由 WebSocketPomeloClient 实现。
    /// </summary>
    public interface IProtocolSession
    {
        /// <summary>发送一个协议包（含包头的完整包）。</summary>
        void Send(PackageType type, byte[] body);

        /// <summary>发送无包体的协议包（如心跳、握手 ack）。</summary>
        void Send(PackageType type);

        /// <summary>触发连接状态事件。</summary>
        void OnStateChanged(WsNetWorkState state);
    }
}
