using System;

namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 变长整数解码（Pomelo 协议扩展 varint）。
    /// </summary>
    public static class WsDecoder
    {
        /// <summary>从指定偏移解码 UInt32，length 输出实际字节数。</summary>
        public static uint DecodeUInt32(int offset, byte[] bytes, out int length)
        {
            uint n = 0;
            length = 0;
            for (int i = offset; i < bytes.Length; i++)
            {
                length++;
                uint m = Convert.ToUInt32(bytes[i]);
                n = n + Convert.ToUInt32((m & 0x7f) * Math.Pow(2, 7 * (i - offset)));
                if (m < 128) break;
            }
            return n;
        }

        /// <summary>从开头解码 UInt32。</summary>
        public static uint DecodeUInt32(byte[] bytes)
        {
            int length;
            return DecodeUInt32(0, bytes, out length);
        }

        /// <summary>解码 SInt32（ZigZag）。</summary>
        public static int DecodeSInt32(byte[] bytes)
        {
            uint n = DecodeUInt32(bytes);
            int flag = ((n % 2) == 1) ? -1 : 1;
            int result = Convert.ToInt32(((n % 2 + n) / 2) * flag);
            return result;
        }
    }
}
