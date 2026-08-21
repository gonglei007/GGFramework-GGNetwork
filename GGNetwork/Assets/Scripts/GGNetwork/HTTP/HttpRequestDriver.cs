using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// HTTP 请求主线程调度器。
///
/// 背景：UnityWebRequest 必须在主线程创建与发送，而框架的
/// HttpNetworkSystem.DoSendRequest 会在子线程（TaskSystem）调用
/// request.SendRequest()。本调度器负责把「真正发送协程」的动作，
/// 从任意线程安全地投递到主线程执行。
///
/// 用法：
///   - Awake 阶段（主线程）调用 EnsureInstance() 创建单例。
///   - 任意线程调用 Enqueue(Action) 把要在主线程执行的逻辑入队，不碰 Unity API。
///   - Update()（主线程）逐个出队执行；协程用 StartCoroutine 启动。
/// </summary>
public class HttpRequestDriver : MonoBehaviour
{
    private static readonly object lockObj = new object();
    private static HttpRequestDriver _instance;
    private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

    public static HttpRequestDriver Instance
    {
        get
        {
            if (_instance == null)
            {
                // 必须在主线程创建；由 HttpNetworkSystem.Awake 保证首次在主线程触发。
                var go = new GameObject("(singleton) HttpRequestDriver");
                _instance = go.AddComponent<HttpRequestDriver>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    /// <summary>确保单例已创建（主线程调用，避免竞态下子线程 new GameObject）。</summary>
    public static void EnsureInstance()
    {
        if (_instance == null) { lock (lockObj) { if (_instance == null) { _ = Instance; } } }
    }

    /// <summary>把要在主线程执行的逻辑入队（线程安全，可任意线程调用）。</summary>
    public static void Enqueue(Action action)
    {
        if (action == null) return;
        // 确保实例存在；若首次由子线程调用，只入队不创建（创建由 EnsureInstance 在主线程完成）。
        if (_instance == null) return;
        _instance.queue.Enqueue(action);
    }

    private void Update()
    {
        Action action;
        while (queue.TryDequeue(out action))
        {
            try { action?.Invoke(); }
            catch (Exception e) { Debug.LogError("[HTTP] dispatch exception: " + e.Message); }
        }
    }

    /// <summary>在主线程启动一个协程（必须在主线程 Update 回调内调用）。</summary>
    public Coroutine StartCoroutineSafe(IEnumerator routine)
    {
        return StartCoroutine(routine);
    }

    private void OnDestroy()
    {
        _instance = null;
    }
}
