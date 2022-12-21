
namespace GGFramework.GGNetwork
{
    /**
     * 网络回调接口。
     */
    public interface INetworkCallback
    {
        void Call(string response);
        void Call(string module, string func, string response);
    }
}
