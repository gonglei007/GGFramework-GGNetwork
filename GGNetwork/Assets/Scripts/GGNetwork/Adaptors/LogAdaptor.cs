using System;
using SimpleJson;

namespace GGFramework.GGNetwork
{
    public class LogAdaptor
    {
        public Action<string, string> onPostError = null;
        public Action<string, JsonObject> onPostNetworkError = null;


        public void PostError(string id, string info) {
            if (onPostError != null) {
                onPostError(id, info);
            }
        }

        public void PostError(string host, JsonObject param) {
            if (onPostNetworkError != null) {
                onPostNetworkError(host, param);
            }
        }
    }
}

