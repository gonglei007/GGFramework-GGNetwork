using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Timers;

namespace GGFramework.GGNetwork
{
    /**
     * 網絡測試類
     */
    public class NetworkTest
    {
        //private const double TEST_OUTTIME = 1500;
        //private const double TEST_DURATION = 20;
        private const float TEST_OUTTIME = 0.8f;

        private static string[] addressList = null;
        private static Dictionary<string, UnityEngine.Ping> pingTable = null;
        //private static double testTimer = 0;
        //private static Timer timer = null;
        //private static bool sDirectly = false;
        private static string foundUrl = null;
        private static Action<string> finishCallback = null;
        private static float uTimer = 0.0f;
        private static bool start = false;

        /**
         * 此方法在主綫程調用。對於不能直接在子綫程返回結果的。在這裏返回結果。
         */
        static public void Update()
        {
            if (start)
            {
                uTimer += Time.fixedDeltaTime;
                if (uTimer > TEST_OUTTIME)
                {
                    OnTimeOut();
                }
                else
                {
                    LookupAddress();
                }
            }
            /*
        if (!sDirectly) {
            if (finishCallback != null) {
                finishCallback(foundUrl);
                finishCallback = null;
            }
        }
             */
        }

        static void OnTimeOut()
        {
            start = false;
            uTimer = 0.0f;
            StopPings();
            /*
            timer.Stop();
            timer = null;
             */
            if (addressList.Length <= 0)
            {
                // 傳入的地址列表有問題。
                GameDebugger.Instance.PushLog("addreslist is empty!");
                foundUrl = null;
                /*
                if (directly)
                {
                    callback(foundUrl);
                }
                else
                {
                    finishCallback = callback;
                }
                 */
            }
            else
            {
                // 都沒有返回結果，給第一個地址
                GameDebugger.Instance.PushLogFormat("Network speed test timeout! Use the default url:{0}", addressList[0]);
                foundUrl = addressList[0];
                /*
                if (directly)
                {
                    callback(foundUrl);
                }
                else
                {
                    finishCallback = callback;
                }
                 */
            }
            finishCallback(foundUrl);
            foundUrl = null;
        }

        static void LookupAddress()
        {
            foreach (string url in pingTable.Keys)
            {
                Ping ping = pingTable[url];
                // 因爲檢測頻率較高（10ms），所以第一個找到的就可以用，其他不用再做檢測了。
                GameDebugger.Instance.PushLog("--->" + ping.time);
                if (ping.isDone)
                {
                    foundUrl = url;
                    break;
                }
            }
            // 如果找到了就停止檢測了。
            if (foundUrl != null)
            {
                start = false;
                uTimer = 0.0f;
                StopPings();
                finishCallback(foundUrl);
                foundUrl = null;
                /*
                timer.Stop();
                timer = null;
                if (directly)
                {
                    callback(foundUrl);
                }
                else
                {
                    finishCallback = callback;
                }
                */
            }
        }

        /**
         * 找到网速最好的链接。
         * @param directly: 是否直接返回回調結果。如果不可以子綫程回調，則用默認值false。
         */
        static public void FindBestNetwork(string[] addressList, Action<string> callback, bool directly = false)
        {
            foundUrl = null;
            start = true;
            NetworkTest.addressList = addressList;
            finishCallback = callback;
            pingTable = new Dictionary<string, UnityEngine.Ping>();
            /*
            sDirectly = directly;
            timer = new Timer(TEST_DURATION);
             */
            // 準備Ping
            foreach (string url in addressList)
            {
                string host = NetworkUtil.GetHostFromUrl(url);
                string ip = NetworkUtil.GetIPFromHost(host);
                Ping ping = new UnityEngine.Ping(ip);
                pingTable[url] = ping;
            }
            /*
            timer.AutoReset = true;
            timer.Start();
            timer.Elapsed += (System.Object source, ElapsedEventArgs e) =>
            {
                testTimer += timer.Interval;
                // 如果超時了，就按如下邏輯處理。
                if (testTimer > TEST_OUTTIME)
                {
                    OnTimeOut(addressList, callback, directly);
                }
                else {
                    LookupAddress(callback, directly);
                }
            };
             */
        }

        /**
         * 停止ping并清空pingTable。
         */
        static public void StopPings()
        {
            foreach (string url in pingTable.Keys)
            {
                Ping ping = pingTable[url];
                ping.DestroyPing();
                ping = null;
            }
            pingTable.Clear();
        }
    }
}
