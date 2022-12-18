using System;

namespace GGFramework.GGNetwork
{
    public class LogAdaptor
    {
        public Action<string, string> onPostError = null;

        public void PostError(string id, string info) {
            if (onPostError != null) {
                onPostError(id, info);
            }
        }
    }
}

