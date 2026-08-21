namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 协议包（Pomelo 传输层帧结构：1 字节类型 + 3 字节长度 + 包体）。
    /// </summary>
    public class Package
    {
        public PackageType type;
        public int length;
        public byte[] body;

        public Package(PackageType type, byte[] body)
        {
            this.type = type;
            this.length = body != null ? body.Length : 0;
            this.body = body;
        }
    }
}
