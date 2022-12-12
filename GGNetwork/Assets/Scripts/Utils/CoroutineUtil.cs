using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineUtil : Singleton<CoroutineUtil>
{
    IEnumerator Perform(IEnumerator coroutine, Action callback)
    {
        yield return StartCoroutine(coroutine);
        if (callback != null)
        {
            callback();
        }
    }

    /// <summary>
    /// 开始一个协同
    /// </summary>
    /// <param name="coroutine">协同函数</param>
    /// <param name="callback">协同完成回调函数</param>
    public static void DoCoroutine(IEnumerator coroutine, Action callback = null)
    {
        CoroutineUtil.Instance.StartCoroutine(CoroutineUtil.Instance.Perform(coroutine, callback));

    }
}
