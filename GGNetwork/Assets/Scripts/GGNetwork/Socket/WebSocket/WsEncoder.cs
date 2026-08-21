using System;
using System.Collections.Generic;

namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 变长整数编码（Pomelo 协议扩展 varint）。
    /// </summary>
    public static class WsEncoder
    {
        /// <summary>编码 UInt32（7-bit base128 varint）。</summary>
        public static byte[] EncodeUInt32(uint n)
        {
            List<byte> byteList = new List<byte>();
            do
            {
                uint tmp = n % 128;
                uint next = n >> 7;
                if (next != 0) tmp = tmp + 128;
                byteList.Add(Convert.ToByte(tmp));
                n = next;
            } while (n != 0);
            return byteList.ToArray();
        }

        /// <summary>编码 SInt32（ZigZag）。</summary>
        public static byte[] EncodeSInt32(int n)
        {
            uint num = (uint)(n < 0 ? (Math.Abs(n) * 2 - 1) : n * 2);
            return EncodeUInt32(num);
        }

        /// <summary>获取字符串 UTF-8 字节长度。</summary>
        public static int ByteLength(string msg)
        {
            return Encoding.UTF8.GetBytes(msg).Length;
        }
    }
}
