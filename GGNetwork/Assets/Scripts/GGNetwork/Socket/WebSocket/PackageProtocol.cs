using System;

namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 协议包编解码（Pomelo 传输层帧：`[type(1)] [len(3)] [body]`）。
    /// 长度字段用大端 3 字节，最大 0xFFFFFF。
    /// </summary>
    public static class PackageProtocol
    {
        public const int HEADER_LENGTH = 4;

        /// <summary>编码仅含类型的空包（心跳/握手 ack 等）。</summary>
        public static byte[] Encode(PackageType type)
        {
            return new byte[] { Convert.ToByte(type), 0, 0, 0 };
        }

        /// <summary>编码带包体的完整包。</summary>
        public static byte[] Encode(PackageType type, byte[] body)
        {
            int length = HEADER_LENGTH;
            if (body != null) length += body.Length;

            byte[] buf = new byte[length];
            int index = 0;

            buf[index++] = Convert.ToByte(type);
            buf[index++] = Convert.ToByte(body.Length >> 16 & 0xFF);
            buf[index++] = Convert.ToByte(body.Length >> 8 & 0xFF);
            buf[index++] = Convert.ToByte(body.Length & 0xFF);

            while (index < length)
            {
                buf[index] = body[index - HEADER_LENGTH];
                index++;
            }
            return buf;
        }

        /// <summary>从缓冲区解码单个包。</summary>
        public static Package Decode(byte[] buf)
        {
            if (buf == null || buf.Length < HEADER_LENGTH)
            {
                throw new ArgumentException("Package buffer is too short.");
            }
            PackageType type = (PackageType)buf[0];
            byte[] body = new byte[buf.Length - HEADER_LENGTH];
            for (int i = 0; i < body.Length; i++)
            {
                body[i] = buf[i + HEADER_LENGTH];
            }
            return new Package(type, body);
        }
    }
}
