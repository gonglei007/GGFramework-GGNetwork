using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGFramework.GGNetwork
{
    public class NetworkConst
    {
        public const int CODE_OK = 200;                     // 成功
        public const int CODE_FAILED = 500;                 // 失败
        public const int CODE_FA_TIMEOUT = 501;             // 超时
        public const int CODE_Silent = 600;                 // 此消息静默处理。
        public const int CODE_FA_REPEAT_MSG = 2007;        //TODO: GL - 要调整这个机制
        public const int CODE_RESPONSE_MSG_ERROR = 3001;           // 响应消息错误（比如不是JSON）

        public const string ERROR_MESSAGE = "network_error";
    }
}
