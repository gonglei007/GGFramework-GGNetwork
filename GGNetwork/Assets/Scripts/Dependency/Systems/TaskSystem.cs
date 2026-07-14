#if UNITY_2017_1_OR_NEWER
    #define DotNet40
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
#if DotNet40
using System.Threading.Tasks;
using System.Collections.Concurrent;
#endif

namespace GGFramework.GGTask
{
    /// <summary>
    /// 任务类。
    /// TODO: GL - 实现超时处理。
    /// </summary>
    public class GTask
    {
        private ManualResetEvent _terminate = new ManualResetEvent(false);
        private Thread thread = null;
        private Action onFinish = null;
        public GTask(ThreadStart threadStart)
        {
            thread = new Thread(() => {
                if (true)
                {
                    threadStart();
                    if (onFinish != null)
                    {
                        onFinish();
                    }
                }
            });
        }

        public void Start(Action onFinish)
        {
            if (thread != null)
            {
                thread.Start();
            }
            this.onFinish = onFinish;
        }

        public void Dispose()
        {
            if (thread != null)
            {
                thread.Interrupt();
                thread = null;
            }
        }
    };

    /// <summary>
    /// 任务系统。用于线程管理。
    /// </summary>
    public class TaskSystem : Singleton<TaskSystem>
    {
        public static int TaskTimeout = 30000;

        public int TaskNumber
        {
            get
            {
                return InnerTaskQueue.Count;
            }
        }
#if DotNet40
        private ConcurrentQueue<Task> InnerTaskQueue = new ConcurrentQueue<Task>();
#else
        private Queue<GTask> InnerTaskQueue = new Queue<GTask>();
        private Thread rootThread;
#endif
        private readonly object jobLock = new object();
        private bool isJobRunning;

        public void Init()
        {
#if DotNet40
            Task.Factory.StartNew(ProcessJobs);
#else
            rootThread = new Thread(ProcessJobs);
            rootThread.Start();
#endif
        }
#if DotNet40
        /// <summary>
        /// 任务耗时检测
        /// </summary>
        public static async Task<bool> TimeTask(Task task)
        {
            if (await Task.WhenAny(task, Task.Delay(TaskTimeout)) == task)
            {
                return true;
            }
            return false;
        }
#endif

        public void QueueJob(Func<Type> job)
        {
#if DotNet40
            var task = new Task<Type>(() => {
                return job();
            });
#else
            var task = new GTask(() =>
            {
                job();
            });
#endif
            InnerTaskQueue.Enqueue(task);
        }

        private void ProcessJobs()
        {
            while (true)
            {
                if (InnerTaskQueue.Count > 0)
                {
                    bool shouldStartJob = false;
                    lock (jobLock)
                    {
                        if (!isJobRunning)
                        {
                            isJobRunning = true;
                            shouldStartJob = true;
                        }
                    }
                    if (shouldStartJob)
                    {
#if DotNet40
                        Task task = null;
                        if (InnerTaskQueue.TryDequeue(out task))
                        {
                            task.Start();
                            Task<bool> taskWait = TimeTask(task);
                            if (taskWait.Result)
                            {
                                task.ContinueWith(t => { lock (jobLock) { isJobRunning = false; } });
                            }
                            else
                            {
                                task.Dispose();
                                lock (jobLock) { isJobRunning = false; }
                            }
                        }
#else
                        GTask task = InnerTaskQueue.Dequeue();
                        if (task != null)
                        {
                            task.Start(() => {
                                task.Dispose();
                                lock (jobLock) { isJobRunning = false; }
                            });
                        }
#endif
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        private void Start()
        {
            Init();
        }

        void OnDestroy()
        {
#if !DotNet40
            rootThread.Abort();
            rootThread = null;
#endif
        }
    }
}
