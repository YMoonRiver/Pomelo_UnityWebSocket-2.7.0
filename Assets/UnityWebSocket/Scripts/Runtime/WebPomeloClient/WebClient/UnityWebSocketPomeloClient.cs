using SimpleJson;
using System;
using System.ComponentModel;
using UnityEngine;
using UnityWebSocket;

namespace Pomelo.UnityWebSocketPomelo
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

    public class UnityWebSocketPomeloClient : IDisposable
    {
        /// <summary>
        /// netwrok changed event
        /// </summary>
        public event Action<NetWorkState> NetWorkStateChangedEvent;


        private NetWorkState netWorkState = NetWorkState.CLOSED;   //current network state

        private EventManager eventManager;
        private WebSocket socket;
        private Protocol protocol;
        private bool disposed = false;
        private uint reqId = 1;
        private int timeoutMSec = 8000;    //connect timeout count in millisecond

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serverAddress">例如:　ws://127.0.0.1:3014/</param>
        public UnityWebSocketPomeloClient()
        {
            
        }

        /// <summary>
        /// initialize pomelo client
        /// </summary>
        /// <param name="callback">socket successfully connected callback(in network thread)</param>
        public void initClient(string address, Action callback = null)
        {
            eventManager = new EventManager();
            NetWorkChanged(NetWorkState.CONNECTING);

            this.socket = new WebSocket(address);
            socket.OnOpen += (sender, openEventArgs) => {
                try
                {
                    this.protocol = new Protocol(this, this.socket);
                    NetWorkChanged(NetWorkState.CONNECTED);
                    if (callback != null)
                    {
                        callback();
                    }
                }
                catch (Exception)
                {
                    if (netWorkState != NetWorkState.TIMEOUT)
                    {
                        NetWorkChanged(NetWorkState.ERROR);
                    }
                    Dispose();
                }
                finally
                {

                }
            };
            socket.ConnectAsync();
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

        /// <summary>
        /// 名为 connect，实为开启 HandShake
        /// </summary>
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
            this.eventManager.AddCallBack(reqId, action);
            protocol.PackAndSend(route, reqId, msg);

            reqId++;
        }

        public void notify(string route, JsonObject msg)
        {
            protocol.send(route, msg);
        }

        public void on(string eventName, Action<JsonObject> action)
        {
            eventManager.AddOnEvent(eventName, action);
        }

        /// <summary>
        /// 这个就是解包
        /// </summary>
        /// <param name="msg"></param>
        internal void processMessage(Message msg)
        {
            if (msg.type == MessageType.MSG_RESPONSE)
            {
                //msg.data["__route"] = msg.route;
                //msg.data["__type"] = "resp";
                eventManager.InvokeCallBack(msg.id, msg.data);
            }
            else if (msg.type == MessageType.MSG_PUSH)
            {
                //msg.data["__route"] = msg.route;
                //msg.data["__type"] = "push";
                eventManager.InvokeOnEvent(msg.route, msg.data);
            }
        }

        public void disconnect()
        {
            Dispose();
            NetWorkChanged(NetWorkState.DISCONNECTED);
        }

        public void Dispose()
        {
            //socket.Close();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // The bulk of the clean-up code
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            if (disposing)
            {
                // free managed resources
                if (this.protocol != null)
                {
                    this.protocol.close();
                }

                if (this.eventManager != null)
                {
                    this.eventManager.Dispose();
                }

                try
                {
                    this.socket.CloseAsync();
                    this.socket = null;
                }
                catch (Exception)
                {
                    //todo : 有待确定这里是否会出现异常，这里是参考之前官方github上pull request。emptyMsg
                }

                this.disposed = true;
            }
        }
    }
}