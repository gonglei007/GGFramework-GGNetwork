using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJson;
using System.IO;
using System;

namespace GGFramework.GGNetwork
{
	public class NetworkRecorder : MonoBehaviour
	{
		public static NetworkRecorder instance = null;

		public static NetworkRecorder Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindObjectOfType<NetworkRecorder>();
					if (instance == null)
					{
						GameObject gameObject = new GameObject("NetworkRecorder");
						instance = gameObject.AddComponent<NetworkRecorder>();
					}
				}
				return instance;
			}
		}

		public JsonArray recordData = new JsonArray();

		public double timer = 0.0;

		public void Update()
		{
#if UNITY_EDITOR
			timer += Time.deltaTime;
			if (Input.GetKeyDown(KeyCode.F5))
			{
				SaveRecord();
				Init();
			}
#endif
		}

		public void Init()
		{
#if UNITY_EDITOR
			timer = 0;
			recordData.Clear();
#endif
		}

		public void RecordNetMessage(string requestKey, JsonObject msg)
		{
#if UNITY_EDITOR
			JsonObject messageObject = new JsonObject();
			messageObject["requestKey"] = requestKey;
			messageObject["msg"] = msg;
			messageObject["time"] = timer;
			recordData.Add(messageObject);
#endif
		}

		public void SaveRecord()
		{
#if UNITY_EDITOR
			DateTime dt = new DateTime(0L);

			string filePath = Path.Combine(Application.dataPath, string.Format("../messsage_{0}.json", dt.ToString("yyyy-MM-dd")));
			Debug.Log("记录文件:" + filePath);
			//CommonTools.SaveStringToFile(recordData.ToString(), filePath);
			//TODO: 实现文本存文件。
#endif
		}
	}
}
