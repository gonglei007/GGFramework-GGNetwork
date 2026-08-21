using System;
using System.Text;

namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 网络连接状态。
    /// </summary>
    public enum WsNetWorkState
    {
        CLOSED,
        CONNECTING,
        CONNECTED,
        DISCONNECTED,
        TIMEOUT,
        ERROR,
    }
}
