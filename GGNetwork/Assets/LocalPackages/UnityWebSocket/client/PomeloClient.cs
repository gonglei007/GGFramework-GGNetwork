using SimpleJson;
using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;

namespace Pomelo.DotNetClient
{
    /// <summary>
    /// network state enum
    /// </summary>
    public enum NetWorkState
    {
        [Description("initial state")]
        CLOSED,

        [Description("connecting server")]
        CONNECTING,

        [Description("server connected")]
        CONNECTED,

        [Description("disconnected with server")]
        DISCONNECTED,

        [Description("connect timeout")]
        TIMEOUT,

        [Description("netwrok error")]
        ERROR
    }

    public class PomeloClient : IDisposable
    {
		public const string EVENT_DISCONNECT = "disconnect";
        /// <summary>
        /// netwrok changed event
        /// </summary>
        public event Action<NetWorkState> NetWorkStateChangedEvent;


        private NetWorkState netWorkState = NetWorkState.CLOSED;   //current network state

        protected EventManager eventManager;
        protected Socket socket;
        private Protocol protocol;
        public bool disposed = false;

        private static object reqIdLock = new object();
        private uint reqId = 1;

        private ManualResetEvent timeoutEvent = new ManualResetEvent(false);
        private int timeoutMSec = 8000;    //connect timeout count in millisecond

        public PomeloClient()
        {
        }

        public PomeloClient(int timeout) {
            timeoutMSec = timeout;
        }

        public NetWorkState NetworkState {
            get {
                return netWorkState;
            }
        }

        /// <summary>
        /// initialize pomelo client
        /// </summary>
        /// <param name="host">server name or server ip (www.xxx.com/127.0.0.1/::1/localhost etc.)</param>
        /// <param name="port">server port</param>
        /// <param name="callback">socket successfully connected callback(in network thread)</param>
        public void initClient(string host, int port, Action callback = null)
        {
            ClearUp();
            timeoutEvent.Reset();
            eventManager = new EventManager();
            NetWorkChanged(NetWorkState.CONNECTING);

            IPAddress ipAddress = null;
            IPAddress ipAddressV6 = null;
            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(host);
                foreach (var item in addresses)
                {
                    if (item.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        ipAddressV6 = item;
                        break;
                    }
                }
                if(ipAddressV6 == null){
	                foreach (var item in addresses)
	                {
	                    if (item.AddressFamily == AddressFamily.InterNetwork)
	                    {
	                        ipAddress = item;
	                        break;
	                    }
	                }
                }
            }
            catch (Exception e)
            {
                NetWorkChanged(NetWorkState.ERROR);
                return;
            }

            if (ipAddressV6 == null && ipAddress == null)
            {
                throw new Exception("can not parse host : " + host);
            }
 
            IPEndPoint ie = null;
            if(ipAddressV6 != null){
                this.socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                ie = new IPEndPoint(ipAddressV6, port);
            }
            else{
                this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ie = new IPEndPoint(ipAddress, port);
            }

            socket.BeginConnect(ie, new AsyncCallback((result) =>
            {
                try
                {
                    this.socket.EndConnect(result);
                    this.protocol = new Protocol(this, this.socket);
                    NetWorkChanged(NetWorkState.CONNECTED);
                    //InnerNetworkChanged(NetWorkState.CONNECTED);
                    //if (callback != null)
                    //{
                    //    callback();
                    //}
                }
                catch (SocketException e)
                {
                    if (netWorkState != NetWorkState.TIMEOUT)
                    {
                        NetWorkChanged(NetWorkState.ERROR);
                    }
                    Dispose();
                }
                finally
                {
                    timeoutEvent.Set();
                }
            }), this.socket);

            if (!timeoutEvent.WaitOne(timeoutMSec, false))
            {
                if (netWorkState != NetWorkState.CONNECTED && netWorkState != NetWorkState.ERROR)
                {
                    NetWorkChanged(NetWorkState.TIMEOUT);
                    Dispose();
                }
            }
        }

        /// <summary>
        /// 网络状态变化
        /// </summary>
        /// <param name="state"></param>
        private void NetWorkChanged(NetWorkState state)
        {
            netWorkState = state;

            if (NetWorkStateChangedEvent != null)
            {
                NetWorkStateChangedEvent(state);
            }
        }

        public void connect()
        {
            connect(null, null);
        }

        public void connect(JsonObject user)
        {
            connect(user, null);
        }

        public void connect(Action<JsonObject> handshakeCallback)
        {
            connect(null, handshakeCallback);
        }

        public bool connect(JsonObject user, Action<JsonObject> handshakeCallback)
        {
            try
            {
                protocol.start(user, handshakeCallback);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        private JsonObject emptyMsg = new JsonObject();
        public void request(string route, Action<JsonObject> action)
        {
            this.request(route, emptyMsg, action);
        }

        public void request(string route, JsonObject msg, Action<JsonObject> action)
        {
            //Debug.LogWarningFormat("Request {0} - reqId:{1}", route, reqId.ToString());
            lock (reqIdLock)
            {
                this.eventManager.AddCallBack(reqId, action);
			    //try
       //         {
            	    protocol.send(route, reqId, msg);
                //}
                //catch(Exception e)
                //         {
                //	////Debug.Log("protocol.send() exception: " + e.ToString());
                //	//JsonObject disconnectMsg = new JsonObject();
                //	//disconnectMsg["innerDisconnect"] = true;
                //	//eventManager.InvokeOnEvent(EVENT_DISCONNECT, disconnectMsg);

                //	//protocol.getPomeloClient().disconnect();
                //}
                reqId++;
            }
        }

        public void notify(string route, JsonObject msg)
        {
            protocol.send(route, msg);
        }

        public void on(string eventName, Action<JsonObject> action)
        {
            eventManager.AddOnEvent(eventName, action);
        }

        internal void processMessage(Message msg)
        {
            if (msg == null) {
                return;
            }
            if (msg.type == MessageType.MSG_RESPONSE)
            {
                eventManager.InvokeCallBack(msg.id, msg.data);
            }
            else if (msg.type == MessageType.MSG_PUSH)
            {
                eventManager.InvokeOnEvent(msg.route, msg.data);
            }
        }

        public void disconnect()
        {
            NetWorkChanged(NetWorkState.DISCONNECTED);
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ClearUp() {
            // free managed resources
            if (this.protocol != null)
            {
                this.protocol.close();
                this.protocol = null;
            }

            if (this.eventManager != null)
            {
                this.eventManager.Dispose();
            }

            try
            {
                if (this.socket != null) {
                    this.socket.Shutdown(SocketShutdown.Both);
                    this.socket.Close();
                    this.socket = null;
                }
            }
            catch (Exception e)
            {
                //todo : 有待确定这里是否会出现异常，这里是参考之前官方github上pull request。emptyMsg
                Debug.LogError(e);
            }
        }

        // The bulk of the clean-up code
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            if (disposing)
            {
                ClearUp();

                this.disposed = true;
            }
        }
    }
}