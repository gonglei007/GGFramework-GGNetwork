using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGFramework.GGNetwork
{
    public class GameNetworkSystem : Singleton<GameNetworkSystem>
    {
        public void Init() {
            HttpNetworkSystem.Instance.Init();
            NetworkSystem.Instance.Init();
            ServiceCenter.Instance.Init();
        }
    }
}

