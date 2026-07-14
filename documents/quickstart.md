# GGNetwork 快速接入指南

此框架提供灵活的使用方式。可以选择完全使用框架附带的第三方接入能力；也可以选择自定义接入网络底层的实现。

## 1. 导入

- 复制 `GGNetwork/Assets/Scripts/GGNetwork/` 目录到你的工程
- 复制 `GGNetwork/Assets/Scripts/Dependency/` 目录到你的工程
- 不要复制 `.asmdef` 文件到你的工程，或者根据需要修改引用

### 工程依赖

作为游戏产品框架中的一个系统，本系统没有另外对 Http/Socket 连接请求做实现。可以通过接口接入使用者自有的系统或者第三方网络库。

| 第三方库 | 说明 | 必选 |
|----------|------|------|
| SimpleJson | 一个简单轻便的 Json 库 | 是 |
| Best HTTP (Pro) | 一个优秀的 Http 网络库 | 否，可替换 |
| Unity WebSocket (Pomelo) | Pomelo 网络框架的客户端网络库 | 否，可替换 |

## 2. 初始化代码

在合适的地方（如游戏启动时）初始化网络系统：

```csharp
using GGFramework.GGNetwork;

void Start()
{
    // 配置全局参数
    NetworkConst.InitEx(
        secretKey: "your-secret-key",
        deviceUID: SystemInfo.deviceUniqueIdentifier,
        channel: "google-play",
        clientVersion: Application.version
    );

    // 初始化所有网络子系统
    GameNetworkSystem.Instance.Init();

    // 绑定 UI 回调（可选）
    HttpNetworkSystem.Instance.UIAdaptor.onWaiting = (bool show) => {
        // 显示/隐藏网络加载遮罩
    };
}
```

## 3. 调用网络接口

### HTTP GET 请求

```csharp
HttpNetworkSystem.Instance.GetWebRequest(
    "http://api.example.com",      // 服务器地址
    "/user/info?uid=12345",        // 请求路径和参数
    HttpNetworkSystem.ExceptionAction.ConfirmRetry,  // 异常处理方式
    (JsonObject response) => {
        // 处理返回的 JSON 响应
        Debug.Log(response.ToString());
    }
);
```

### HTTP POST 请求

```csharp
JsonObject param = new JsonObject();
param["username"] = "player1";
param["password"] = "123456";

HttpNetworkSystem.Instance.PostWebRequest(
    "http://api.example.com",      // 服务器地址
    "/user/login",                 // 请求路径
    param,                         // JSON 参数
    HttpNetworkSystem.ExceptionAction.ConfirmRetry,  // 异常处理方式
    (JsonObject response) => {
        // 处理登录响应
        if (response.ContainsKey("token")) {
            HttpNetworkSystem.Token = response["token"].ToString();
        }
    }
);
```

### 参数说明

| 参数 | 类型 | 说明 |
|------|------|------|
| `httpAddress` | `string` | 服务器基础 URL，如 `http://api.example.com` |
| `command` | `string` | 请求路径，如 `/user/login` |
| `paramObject` | `JsonObject` | JSON 格式的请求参数 |
| `exceptionAction` | `ExceptionAction` | 异常处理策略，默认为 `ConfirmRetry` |
| `callback` | `Action<JsonObject>` | 请求成功的回调函数 |

## 4. 关联 UI

绑定 UI 回调后，网络发生各种反馈时可以通过 UI 表现出来，获得更好的体验。

如果不设置，网络的各种情况就不会通过 UI 表现出来，但不影响网络系统的使用。

### 绑定本地化文本

```csharp
HttpNetworkSystem.Instance.UIAdaptor.onGetText = (string text) => {
    // 返回对应语言的本地化文本
    return Localization.GetText(text);
};
```

### 绑定对话框

```csharp
HttpNetworkSystem.Instance.UIAdaptor.onDialog = (string title, string msg, bool retry, Action<bool> callback) => {
    // 弹出对话框
    UIManager.ShowConfirmDialog(title, msg, () => {
        callback(true);   // 重试
    }, () => {
        callback(false);  // 取消
    });
};
```

### 绑定网络等待遮罩

```csharp
HttpNetworkSystem.Instance.UIAdaptor.onWaiting = (bool waiting) => {
    // 根据 waiting 的值，显示或者隐藏网络等待遮罩
    if (waiting) {
        LoadingMask.Show();
    } else {
        LoadingMask.Hide();
    }
};
```

## 5. WebSocket 长连接使用

```csharp
// 连接服务器
NetworkSystem.Instance.ConnectNetworkClient(
    "game",
    "127.0.0.1",
    3014,
    (JsonObject result) => {
        if (result["code"].ToString() == "200") {
            Debug.Log("连接成功!");
        }
    }
);

// 发送消息
JsonObject msg = new JsonObject();
msg["action"] = "chat";
msg["content"] = "Hello!";
NetworkSystem.Instance.SendRequest(
    "game",
    "chat.chatHandler.send",
    msg,
    (JsonObject response) => {
        Debug.Log("收到回复: " + response.ToString());
    }
);

// 断开连接
NetworkSystem.Instance.CloseClient("game");
```

## 6. 下载系统使用

```csharp
DownloadSystem.Instance.RequestDownload(
    "http://cdn.example.com/bundle.zip",           // 下载 URL
    Application.persistentDataPath + "/bundle.zip", // 保存路径
    (float progress) => {
        // 下载进度回调 (0.0 ~ 1.0)
        Debug.LogFormat("进度: {0:P}", progress);
    },
    (int totalSize) => {
        // 文件总大小回调
        Debug.LogFormat("总大小: {0} bytes", totalSize);
    },
    (string completedUrl) => {
        // 下载完成回调
        Debug.Log("下载完成: " + completedUrl);
    }
);
```

## 7. HTTPDNS 配置

```csharp
// 启用 HTTPDNS
HttpNetworkSystem.Instance.EnableHttpDNS = true;

// 设置 HTTPDNS API 地址
HTTPDNSSystem.Instance.SetAPIHost("dns-api.example.com:40021");

// 预解析关键域名
string[] domains = { "api.example.com", "cdn.example.com" };
HTTPDNSSystem.Instance.ParseHosts(domains, (caches, status, message) => {
    Debug.LogFormat("预解析完成: {0} 个域名, 状态: {1}", caches.Count, status);
});
```

## 8. 日志上报配置

```csharp
// 绑定网络异常上报
HttpNetworkSystem.Instance.LogAdaptor.onPostNetworkError = (string host, JsonObject param) => {
    string reportJson = SimpleJson.SimpleJson.SerializeObject(param);
    // 将错误信息上报到日志服务器
    LogServer.ReportError(host, reportJson);
};
```
# 快速使用
此框架提供灵活的使用方式。可以选择完全使用框架附带的第三方接入能力；也可以选择自定义接入网络底层的实现。

## 1.导入
* 复制GGNetwork目录到你的工程（先不要复制GGFramework.GGNetwork.asmdef文件到你的工程）。

### 工程依赖
作为游戏产品框架中的一个系统，本系统没有另外对Http/Socket连接请求做实现。可以通过接口接入使用者自有的系统或者第三方网络库。

比如下面的几个库就是

<div>
	<table>
	    <tr>
            <th>
            第三方库
            </th>
            <th>
            说明
            </th>
            <th>
            必选
            </th>
		</tr>
		<tr>
			<td>
			SimpleJson
			</td>
			<td>
			一个简单轻便的Json库。
			</td>
			<td>
			是
			</td>
		</tr>
		<tr>
			<td>
			Best Http
			</td>
			<td>
			一个优秀的Http网络库。
			</td>
			<td>
			否
			</td>
		</tr>
		<tr>
			<td>
			Unity Websocket
			</td>
			<td>
			一个Pomelo网络框架的客户端网络库。
			</td>
			<td>
			否
			</td>
		</tr>
	</table>
</div>

## 2.初始化代码
在合适的地方初始化网络系统。

### 示例代码
```csharp
GameNetworkSystem.Instance.Init();
```

## 3.调用网络接口。

### 参数说明
[TODO]

### 示例代码
```csharp
JsonObject param = new JsonObject();
HttpNetworkSystem.Instance.GetWebRequest("http://url", "command", HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response)=>{
	// 接收json，处理相关逻辑。
});

```

## 4.关联UI
绑定UI回调后。让网络发生各种反馈的时候，可以通过UI表现出来，获得更好的体验。

如果不设置，网络的各种情况就不会通过UI表现出来，但不影响网络系统的使用。

### 示例代码
```csharp
// 绑定本地化文本。
HttpNetworkSystem.Instance.UIAdaptor.onWaiting = (bool waiting){
	// 根据waiting的值，显示或者隐藏网络等待遮罩。
	UISystem.ShowWaiting(waiting);
}

// 绑定对话框。
string GetLocalText(string text){
	//...return localization text;
}
HttpNetworkSystem.Instance.UIAdaptor.onGetText = GetLocalText;

// 绑定网络等待框。
HttpNetworkSystem.Instance.UIAdaptor.onWaiting = (bool waiting){
	// 根据waiting的值，显示或者隐藏网络等待遮罩。
	UISystem.ShowWaiting(waiting);
}

```
