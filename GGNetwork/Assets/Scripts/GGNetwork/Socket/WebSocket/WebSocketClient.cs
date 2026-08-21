using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 基于 System.Net.WebSockets.ClientWebSocket 的传输层封装。
    ///
    /// 职责：
    ///   - 建立/关闭 WebSocket 连接（Connect/Close）。
    ///   - 发送完整二进制消息（线程安全串行化）。
    ///   - 后台接收循环，把收到的完整字节缓冲上抛 OnDataReceived。
    ///   - 连接状态事件（OnConnected / OnError / OnClosed）上抛。
    ///
    /// 说明：
    ///   - 所有底层读写均在 .NET 线程池上异步执行，不阻塞 Unity 主线程。
    ///   - 收到的字节是「完整的 WebSocket 二进制消息」，上层再按 Pomelo 包协议切分。
    ///   - 协议层对网络帧的粘包/拆包由 PomeloPackageStream 处理（见 PackageStream）。
    /// </summary>
    public class WebSocketClient : IDisposable
    {
        private ClientWebSocket ws;
        private Uri uri;
        private readonly System.Collections.Concurrent.ConcurrentQueue<byte[]> sendQueue =
            new System.Collections.Concurrent.ConcurrentQueue<byte[]>();
        private readonly SemaphoreSlim sendSignal = new SemaphoreSlim(0);
        private CancellationTokenSource recvCts;
        private volatile bool running;
        private volatile bool disposed;

        /// <summary>连接成功建立。</summary>
        public event Action OnConnected;
        /// <summary>连接失败或链接中断。</summary>
        public event Action<string> OnError;
        /// <summary>连接被关闭（对端关闭或本地主动）。</summary>
        public event Action OnClosed;
        /// <summary>收到完整二进制消息。</summary>
        public event Action<byte[]> OnDataReceived;

        public bool IsOpen
        {
            get
            {
                return ws != null && ws.State == WebSocketState.Open && running;
            }
        }

        /// <summary>建立连接。url 形如 ws://host:port/path。</summary>
        public void Connect(string url)
        {
            if (running) return;
            uri = new Uri(url);

            try
            {
                ws = new ClientWebSocket();
            }
            catch (Exception e)
            {
                Debug.LogError("[WS] create ClientWebSocket failed: " + e.Message);
                FireError("create_clientwebsocket_failed");
                return;
            }

            running = true;
            _ = Task.Run(() => ConnectAsyncSafe());
        }

        private async Task ConnectAsyncSafe()
        {
            try
            {
                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(8)))
                {
                    await ws.ConnectAsync(uri, timeoutCts.Token);
                }
                if (ws.State != WebSocketState.Open)
                {
                    FireError("connect_not_open");
                    return;
                }
                recvCts = new CancellationTokenSource();
                SafeMain(() => OnConnected?.Invoke());
                _ = Task.Run(() => ReceiveLoopSafe());
                _ = Task.Run(() => SendLoopSafe());
            }
            catch (Exception e)
            {
                FireError("connect_error:" + e.Message);
            }
        }

        private async Task ReceiveLoopSafe()
        {
            byte[] buffer = new byte[16 * 1024];
            try
            {
                while (running && ws.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;
                    byte[] full;
                    using (var ms = new System.IO.MemoryStream())
                    {
                        while (true)
                        {
                            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), recvCts.Token);
                            if (result.MessageType == WebSocketMessageType.Close && !result.EndOfMessage)
                            {
                                // 对端关闭
                                break;
                            }
                            ms.Write(buffer, 0, result.Count);
                            if (result.EndOfMessage) break;
                        }

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            if (result.CloseStatus.HasValue)
                                Debug.LogWarning("[WS] peer closed: " + result.CloseStatus.Value);
                            break;
                        }
                        if (result.MessageType == WebSocketMessageType.Binary || result.MessageType == WebSocketMessageType.Text)
                        {
                            full = ms.ToArray();
                            if (full.Length > 0)
                            {
                                byte[] copy = full;
                                SafeMain(() => OnDataReceived?.Invoke(copy));
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 主动关闭
            }
            catch (Exception e)
            {
                if (running) FireError("receive_error:" + e.Message);
            }
            finally
            {
                FireClosed();
            }
        }

        /// <summary>投递一条二进制消息到发送队列（由内部发送任务串行发出）。</summary>
        public void SendBinary(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            if (!IsOpen) return;

            sendQueue.Enqueue(data);
            try { sendSignal.Release(); } catch (SemaphoreFullException) { }
        }

        private async Task SendLoopSafe()
        {
            try
            {
                while (running && ws != null && ws.State == WebSocketState.Open)
                {
                    await sendSignal.WaitAsync(recvCts.Token).ConfigureAwait(false);

                    byte[] item;
                    if (!sendQueue.TryDequeue(out item)) continue;
                    if (item == null || item.Length == 0) continue;

                    await ws.SendAsync(new ArraySegment<byte>(item),
                        WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // 主动关闭
            }
            catch (Exception e)
            {
                if (running) FireError("send_error:" + e.Message);
            }
        }

        /// <summary>主动关闭连接。</summary>
        public void Close()
        {
            if (!running) return;
            running = false;

            try { recvCts?.Cancel(); } catch { }
            var wsLocal = ws;
            if (wsLocal != null && wsLocal.State != WebSocketState.Closed)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                        {
                            await wsLocal.CloseAsync(WebSocketCloseStatus.NormalClosure, "client_close", cts.Token);
                        }
                    }
                    catch { /* ignore close errors */ }
                    finally
                    {
                        try { wsLocal.Dispose(); } catch { }
                    }
                });
            }
            ws = null;
        }

        public void Dispose()
        {
            disposed = true;
            running = false;
            try { recvCts?.Cancel(); } catch { }
            try { ws?.Dispose(); } catch { }
            ws = null;
            try { sendSignal.Dispose(); } catch { }
            try { while (sendQueue.TryDequeue(out _)) { } } catch { }
        }

        private void FireError(string msg)
        {
            running = false;
            Debug.LogError("[WS] " + msg);
            SafeMain(() => OnError?.Invoke(msg));
            FireClosed();
        }

        private void FireClosed()
        {
            if (disposed) return;
            SafeMain(() => OnClosed?.Invoke());
        }

        private static void SafeMain(Action act)
        {
            try { act(); } catch (Exception e) { Debug.LogError("[WS] main callback: " + e.Message); }
        }
    }
}
