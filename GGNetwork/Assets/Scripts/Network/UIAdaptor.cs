using System;

namespace GGFramework.GGNetwork {
    /// <summary>
    /// UI适配器。实现网络系统跟UI对接的能力。
    /// TODO: 考虑是否给UI显示一个超时，使得在任何异常情况都能隐藏掉。
    /// </summary>
    public class UIAdaptor
    {
        public Action<string, string, bool, Action<bool>> onDialog = null;
        public Func<string, string> onGetText = null;
        public Action<bool> onWaiting = null;

        /// <summary>
        /// 获取（本地化）文本。
        /// 如果没有复制本地化文本回调，直接传key。
        /// </summary>
        /// <param name="text"></param>
        public string GetText(string text)
        {
            if (onGetText == null)
            {
                return text;
            }
            return onGetText(text);
        }

        public void ShowDialog(string title, string message, bool confirm, Action<bool> callback) {
            if (onDialog != null)
            {
                onDialog(GetText(title), GetText(message), confirm, callback);
            }
            else {
                callback(true);
            }
        }

        public void ShowWaiting(bool waiting) {
            if (onWaiting != null)
            {
                onWaiting(waiting);
            }
        }
    }
}
