using System;
using System.Collections.Generic;
using SimpleJson;

namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 请求回调 + 推送事件订阅管理器（线程安全）。
    ///
    /// - callBackMap：按 reqId 记录请求回调（MSG_RESPONSE 时触发）。
    /// - eventMap：按 route 记录推送订阅（MSG_PUSH 时触发）。
    /// 内部所有字典操作用锁保护，可在收发工作线程安全调用。
    /// </summary>
    public class EventManager : IDisposable
    {
        private readonly Dictionary<uint, Action<JsonObject>> callBackMap = new Dictionary<uint, Action<JsonObject>>();
        private readonly Dictionary<string, List<Action<JsonObject>>> eventMap = new Dictionary<string, List<Action<JsonObject>>>();
        private readonly object lockObj = new object();

        public void AddCallBack(uint id, Action<JsonObject> callback)
        {
            if (id <= 0 || callback == null) return;
            lock (lockObj)
            {
                if (!callBackMap.ContainsKey(id)) callBackMap[id] = callback;
            }
        }

        /// <summary>触发请求响应回调。返回是否存在对应回调。</summary>
        public bool InvokeCallBack(uint id, JsonObject data)
        {
            Action<JsonObject> cb = null;
            lock (lockObj)
            {
                if (callBackMap.TryGetValue(id, out cb))
                {
                    callBackMap.Remove(id);
                }
            }
            if (cb != null)
            {
                try { cb.Invoke(data); } catch (Exception e) { UnityEngine.Debug.LogError(e); }
                return true;
            }
            return false;
        }

        public void AddOnEvent(string eventName, Action<JsonObject> callback)
        {
            if (string.IsNullOrEmpty(eventName) || callback == null) return;
            lock (lockObj)
            {
                List<Action<JsonObject>> list;
                if (!eventMap.TryGetValue(eventName, out list))
                {
                    list = new List<Action<JsonObject>>();
                    eventMap[eventName] = list;
                }
                list.Add(callback);
            }
        }

        /// <summary>触发推送事件。返回是否存在订阅。</summary>
        public bool InvokeOnEvent(string route, JsonObject data)
        {
            List<Action<JsonObject>> list;
            lock (lockObj)
            {
                if (!eventMap.TryGetValue(route, out list)) return false;
                // 复制快照，避免回调中再次增删订阅导致迭代异常。
                list = new List<Action<JsonObject>>(list);
            }
            foreach (Action<JsonObject> action in list)
            {
                try { action.Invoke(data); } catch (Exception e) { UnityEngine.Debug.LogError(e); }
            }
            return true;
        }

        public void Dispose()
        {
            lock (lockObj)
            {
                callBackMap.Clear();
                eventMap.Clear();
            }
        }
    }
}
