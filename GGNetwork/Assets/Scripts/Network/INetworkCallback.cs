
namespace GGFramework.GGNetwork
{
    /**
     * 网络回调接口。
     */
    public interface INetworkCallback
    {
        public virtual void Call(string response) { }
        public virtual void Call(string module, string func, string response) { }
    }
}
