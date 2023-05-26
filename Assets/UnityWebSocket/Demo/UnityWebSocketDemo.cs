using Pomelo.UnityWebSocketPomelo;
using SimpleJson;
using System;
using UnityEngine;

namespace UnityWebSocket.Demo
{
    public class UnityWebSocketDemo : MonoBehaviour
    {
        /// <summary>
        /// pomelo webgl 客户端 
        /// </summary>
        public UnityWebSocketPomeloClient wpClient = null;
        private void Start()
        {
            if (wpClient == null)
            {
                wpClient = new UnityWebSocketPomeloClient();
                //监听网络状态变化事件
                wpClient.NetWorkStateChangedEvent += (state) =>
                {
                    Debug.Log("CurrentState is:" + state);
                };

                wpClient.initClient("ws://127.0.0.1:34590/", () =>
                {
                    //test 握手并登录
                    JsonObject msg = new JsonObject();
                    wpClient.connect(msg, (JsonObject json) =>
                    {
                        JsonObject userMessage = new JsonObject();
                        userMessage["nickname"] = "123456";
                        wpClient.request("room.room.login", userMessage, OnQuery);
                    });
                });

            }
        }

        void OnQuery(JsonObject result)
        {
            if (Convert.ToInt32(result["code"]) == 200)
            {
                wpClient.disconnect();

                string host = (string)result["host"];
                int port = Convert.ToInt32(result["port"]);

                wpClient = new UnityWebSocketPomeloClient();
                wpClient.initClient("ws://" + host + ":" + port.ToString() + "/", () =>
                {
                    JsonObject msg = new JsonObject();
                    wpClient.connect(msg, (JsonObject json) =>
                    {
                        JsonObject userMessage = new JsonObject();
                        userMessage["username"] = "123456";
                        if (wpClient != null)
                        {
                            wpClient.request("room.room.login", userMessage, OnEntry);
                        }
                    });
                });
            }
        }

        void OnEntry(JsonObject data)
        {
            //users = data;
            //isLoad = true;
        }

        private void OnApplicationQuit()
        {
            Debug.Log("OnApplicationQuit");
            wpClient.Dispose();
        }

    }
}
