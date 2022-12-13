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
                {// (_terminate.WaitOne(TaskSystem.TaskTimeout)) {
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
                //_terminate.Set();
                thread.Interrupt();
                //thread.Join(TaskSystem.TaskTimeout);
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
        /*
        public class Task {

            public Thread thread = null;
            public Task(ThreadStart threadStart) {
                thread = new Thread(threadStart);
            }
        }
        private Dictionary<string, Task> tasks = new Dictionary<string, Task>();
        */
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
        private bool IsJobRunning;

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
        /// 任务耗时检测,如果一个任务耗时超过5秒(暂定5秒)就返回false
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
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
                //Debug.Log("++++[Sub Thread]:");
                return job();
            });
#else
        var task = new GTask(() =>
        {
            //Debug.Log("++++[Sub Thread]:");
            job();
        });
#endif
            InnerTaskQueue.Enqueue(task);
            //Debug.LogWarning("任务进队列");
            //return task;
        }

        private void ProcessJobs()
        {
            while (true)
            {
                if (InnerTaskQueue.Count > 0 && !IsJobRunning)
                {
                    //Debug.LogWarning("取出任务");
#if DotNet40
                    Task task = null;
                    if (InnerTaskQueue.TryDequeue(out task))
                    {
                        task.Start();
                        IsJobRunning = true;
                        Task<bool> taskWait = TimeTask(task);
                        if (taskWait.Result)
                        {
                            task.ContinueWith(t => IsJobRunning = false);
                        }
                        else
                        {
                            task.Dispose();
                            IsJobRunning = false;
                        }
                    }
#else
                GTask task = InnerTaskQueue.Dequeue();
                if (task != null) {
                    task.Start(()=> {
                        task.Dispose();
                        IsJobRunning = false;
                    });
                    IsJobRunning = true;
                }
#endif
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
            //Stop all thread
            //thread.Abort();
#if DotNet40
#else
        rootThread.Abort();
        rootThread = null;
#endif
        }
        /*
        public void AddTask(string name, ThreadStart threadStart) {
            if (string.IsNullOrEmpty(name)) {
                return;
            }
            if (!tasks.ContainsKey(name)) {
                tasks[name] = new Task(threadStart);
            }
        }

        public Task GetTask(string name) {
            if (!tasks.ContainsKey(name))
            {
                return null;
            }
            return tasks[name];
        }

        public void StartTask(string name) {
            Task task = GetTask(name);
            if (task != null) {
                task.thread.Start();
            }
        }

        public void StopTask(string name) {
            Task task = GetTask(name);
            if (task != null)
            {
                task.thread.Abort();
            }
        }
        */
    }
}

