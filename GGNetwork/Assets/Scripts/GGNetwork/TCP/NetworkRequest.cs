using System;
using SimpleJson;

namespace GGFramework.GGNetwork
{
	/// <summary>
	/// TODO: GL - 实现状态记录。
	/// T 表示回调函数的类型。比如LuaFunction。
	/// </summary>
	public class NetworkRequest
	{
		public enum RequestStates
		{
			Initial,
			Processing,
			Finished,
			Error,
			TimedOut
		}

		public enum CallbackType
		{
			CT_CALLBACK1,
			CT_CALLBACK2,
			CT_LUACALLBACK,
			CT_LUACALLBACK2,
		};


		public CallbackType type;

		public CallbackType Type
		{
			get
			{
				return this.type;
			}
		}

		public Action<RequestStates> onStateChanged = null;

		public Action<JsonObject, JsonObject> callback2;

		//将发送给服务器的msg一并返回
		public Action<JsonObject> callback;

		public INetworkCallback iCallback;

		public string luaModule;

		public string luaFunc;

		public JsonObject data;

		public int msgID = 0;
		public string route;
		public JsonObject msg;

		public float timer = 0.0f;
		//public bool needMask = true;

		private RequestStates state = RequestStates.Initial;
		public string errorMessage = null;

		public NetworkRequest(string route)
		{
			this.route = route;
		}

		//在消息队列里只允许同时存在一个requestKey
		public NetworkRequest(Action<JsonObject> callback, JsonObject data, string route, JsonObject msg)
		{
			this.type = CallbackType.CT_CALLBACK1;
			this.callback2 = null;
			this.callback = callback;
			this.data = data;
			this.route = route;
			this.msg = msg;
		}

		public NetworkRequest(Action<JsonObject, JsonObject> callback2, JsonObject data, string route, JsonObject msg)
		{
			this.type = CallbackType.CT_CALLBACK2;
			this.callback2 = callback2;
			this.callback = null;
			this.data = data;
			this.route = route;
			this.msg = msg;
		}

		public NetworkRequest(string module, string func, JsonObject data, string route, JsonObject msg)
		{
			this.type = CallbackType.CT_LUACALLBACK2;
			this.luaModule = module;
			this.luaFunc = func;
			this.callback2 = null;
			this.callback = null;
			this.data = data;
			this.route = route;
			this.msg = msg;
		}

		public NetworkRequest(INetworkCallback iCallback, JsonObject data, string route, JsonObject msg)
		{
			this.type = CallbackType.CT_LUACALLBACK;
			this.iCallback = iCallback;
			this.luaModule = "";
			this.luaFunc = "";
			this.callback2 = null;
			this.callback = null;
			this.data = data;
			this.route = route;
			this.msg = msg;
		}

		public RequestStates State
		{
			get
			{
				return state;
			}
		}

		public void ChangeState(RequestStates state)
		{
			if (this.state != state)
			{
				if (this.onStateChanged != null)
				{
					this.onStateChanged(state);
				}

				this.state = state;
			}
		}

		public void DoCallback()
		{
			switch (this.type)
			{
				case CallbackType.CT_CALLBACK1:
					if (this.callback != null)
					{
						this.callback(this.data);
					}
					else
					{
						GameDebugger.Instance.PushLog(string.Format("{0} callback is null! ", this.route));
					}
					break;
				case CallbackType.CT_CALLBACK2:
					if (this.callback2 != null)
					{
						this.callback2(this.data, this.msg);
					}
					else
					{
						GameDebugger.Instance.PushLog(string.Format("{0} callback is null! ", this.route));
					}
					break;
				case CallbackType.CT_LUACALLBACK:
					if (this.iCallback != null)
					{
						this.iCallback.Call(this.data.ToString());
					}
					else
					{
						GameDebugger.Instance.PushLog(string.Format("{0} callback is null! ", this.route));
					}
					break;
				case CallbackType.CT_LUACALLBACK2:
					if (this.luaModule != null && this.luaFunc != null)
					{
						//LuaFramework.Util.CallMethod(this.luaModule, this.luaFunc, this.data.ToString());
						this.iCallback.Call(this.luaModule, this.luaFunc, this.data.ToString());
					}
					else
					{
						GameDebugger.Instance.PushLog(string.Format("{0} callback is null! ", this.route));
					}
					break;
			}
		}

		public void Update(float deltaTime)
		{
			switch (state)
			{
				case RequestStates.Processing:
					timer += deltaTime;
					break;
				default:
					break;
			}
		}
	}
}
