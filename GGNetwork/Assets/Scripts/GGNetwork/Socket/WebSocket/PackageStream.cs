using System;
using System.Collections.Generic;

namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 协议包流解析器：把 WebSocket 收到的字节块按 Pomelo 包协议切分成完整包。
    ///
    /// WebSocket 消息边界 ≠ Pomelo 包边界：
    ///   - 一次 WS 消息可能包含多个 Pomelo 包（粘包）。
    ///   - 一个 Pomelo 包可能横跨多个 WS 消息（拆包）。
    /// 本解析器维护内部缓冲，逐字节按 `[type(1)][len(3)][body(len)]` 切包，
    /// 每切出一个完整包即回调 <see cref="OnPackage"/>。
    /// </summary>
    public class PackageStream
    {
        public const int HEADER_LENGTH = 4;

        private readonly List<byte> buffer = new List<byte>(4096);
        private Action<Package> onPackage;
        private bool disposed;

        public PackageStream(Action<Package> onPackage)
        {
            this.onPackage = onPackage;
        }

        /// <summary>接收一段新的字节块并立即尝试切包。</summary>
        public void Feed(byte[] data)
        {
            if (disposed || data == null || data.Length == 0) return;
            lock (buffer)
            {
                buffer.AddRange(data);
                Drain();
            }
        }

        private void Drain()
        {
            while (buffer.Count >= HEADER_LENGTH)
            {
                int len = (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
                int total = HEADER_LENGTH + len;
                if (total < HEADER_LENGTH || total > 0xFFFFFF)
                {
                    // 非法长度，丢弃整个缓冲以防死循环。
                    buffer.Clear();
                    return;
                }
                if (buffer.Count < total) break; // 等更多数据

                PackageType type = (PackageType)buffer[0];

                byte[] body = new byte[len];
                if (len > 0)
                {
                    buffer.CopyTo(HEADER_LENGTH, body, 0, len);
                }
                // 移除已消费部分
                buffer.RemoveRange(0, total);

                onPackage?.Invoke(new Package(type, body));
            }
        }

        public void Clear()
        {
            lock (buffer) buffer.Clear();
        }

        public void Dispose()
        {
            disposed = true;
            Clear();
            onPackage = null;
        }
    }
}
