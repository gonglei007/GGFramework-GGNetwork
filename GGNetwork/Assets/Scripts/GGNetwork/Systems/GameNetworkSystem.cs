using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GGFramework.GGNetwork.HTTPDNS;

namespace GGFramework.GGNetwork
{
    public class GameNetworkSystem : Singleton<GameNetworkSystem>
    {
        /// <summary>
        /// 初始化游戏的网络系统
        /// </summary>
        public void Init() {
            // 下面的调用顺序不能随便改动。
            NetworkConst.InitEx();
            HttpNetworkSystem.Instance.Init<BestHTTPFactory>(new BestHTTPFactory());
            HTTPDNSSystem.Instance.Init(HTTPDNSFactory.Provider.CY);
            NetworkSystem.Instance.Init();
            ServiceCenter.Instance.Init();
            NetworkRecorder.Instance.Init();
        }
    }
}

