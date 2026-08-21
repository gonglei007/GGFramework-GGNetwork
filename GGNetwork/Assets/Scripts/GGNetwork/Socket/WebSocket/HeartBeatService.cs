using System;
using System.Timers;

namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 心跳服务（Pomelo 协议心跳）。
    ///
    /// 按服务端下发的 heartbeat 间隔（毫秒）周期发送 PKG_HEARTBEAT。
    /// 若在 2 个周期内未收到任何数据（resetTimeout 未被调用），则判定心跳超时，
    /// 通知上层断开连接（HeatbeatBroken）。
    /// </summary>
    public class HeartBeatService
    {
        private readonly int intervalMS;
        private readonly IProtocolSession session;
        private Timer timer;
        private DateTime lastReceiveTime;
        private readonly object timeLock = new object();
        private volatile bool disposed;

        public HeartBeatService(int intervalMS, IProtocolSession session)
        {
            this.intervalMS = intervalMS;
            this.session = session;
        }

        /// <summary>收到任何数据时重置超时计时。</summary>
        public void ResetTimeout()
        {
            lock (timeLock) lastReceiveTime = DateTime.Now;
        }

        public void Start()
        {
            if (intervalMS < 1000) return;

            Stop();
            disposed = false;
            ResetTimeout();

            timer = new Timer { Interval = intervalMS, AutoReset = true };
            timer.Elapsed += OnTick;
            timer.Enabled = true;
        }

        private void OnTick(object source, ElapsedEventArgs e)
        {
            if (disposed) return;

            double elapsed;
            lock (timeLock) elapsed = (DateTime.Now - lastReceiveTime).TotalMilliseconds;

            // 超过 2 个周期未收到数据，心跳断裂，通知上层断开。
            if (elapsed > intervalMS * 2)
            {
                UnityEngine.Debug.LogWarning("[WS] HeartBeat timeout, disconnect.");
                Stop();
                session.OnStateChanged(WsNetWorkState.DISCONNECTED);
                return;
            }

            // 发送心跳
            session.Send(PackageType.PKG_HEARTBEAT);
        }

        public void Stop()
        {
            if (timer != null)
            {
                timer.Enabled = false;
                timer.Elapsed -= OnTick;
                timer.Dispose();
                timer = null;
            }
            disposed = true;
        }
    }
}
