# GGNetwork 使用手册

## 目录
- [集成](#集成)
- [初始化](#初始化)
- [基础使用](#基础使用)
  - [HTTP GET 请求](#http-get-请求)
  - [HTTP POST 请求](#http-post-请求)
  - [异常处理类型](#异常处理类型)
- [高级使用](#高级使用)
  - [HTTPDNS 配置](#httpdns-配置)
  - [UI 适配器绑定](#ui-适配器绑定)
  - [日志适配器绑定](#日志适配器绑定)
  - [请求参数预处理（签名）](#请求参数预处理签名)
  - [WebSocket 长连接](#websocket-长连接)
  - [文件下载](#文件下载)
- [错误处理](#错误处理)
- [API 参考](#api-参考)

---

## 集成

复制 `/GGNetwork/Assets/Scripts/GGNetwork/` 和 `/GGNetwork/Assets/Scripts/Dependency/` 目录到您的 Unity 项目中。

## 初始化

在游戏启动时调用一次初始化：

```csharp
using GGFramework.GGNetwork;

// 初始化全局参数（可选）
NetworkConst.InitEx(
    secretKey: "your-secret-key",   // HTTP 签名密钥
    deviceUID: "device-unique-id",  // 设备唯一标识
    channel: "app-store",           // 渠道标识
    clientVersion: "1.0.0"         // 客户端版本
);

// 初始化游戏网络系统
GameNetworkSystem.Instance.Init();
```

## 基础使用

### HTTP GET 请求

```csharp
HttpNetworkSystem.Instance.GetWebRequest(
    "http://api.example.com",
    "/user/info",
    HttpNetworkSystem.ExceptionAction.ConfirmRetry,
    (JsonObject response) => {
        Debug.Log("Response: " + response.ToString());
    }
);
```

### HTTP POST 请求

使用 JSON 参数：
```csharp
JsonObject param = new JsonObject();
param["username"] = "player1";
param["password"] = "123456";

HttpNetworkSystem.Instance.PostWebRequest(
    "http://api.example.com",
    "/user/login",
    param,
    HttpNetworkSystem.ExceptionAction.ConfirmRetry,
    (JsonObject response) => {
        Debug.Log("Login response: " + response.ToString());
    }
);
```

使用表单参数：
```csharp
WWWForm form = new WWWForm();
form.AddField("username", "player1");
form.AddField("password", "123456");

HttpNetworkSystem.Instance.PostWebRequest(
    "http://api.example.com",
    "/user/login",
    form,
    HttpNetworkSystem.ExceptionAction.ConfirmRetry,
    (JsonObject response) => {
        Debug.Log("Login response: " + response.ToString());
    }
);
```

### 异常处理类型

| 类型 | 说明 | 使用场景 |
|------|------|----------|
| `ExceptionAction.ConfirmRetry` | 弹出确认框，等待玩家操作决定是否重试 | 大部分业务请求 |
| `ExceptionAction.AutoRetry` | 自动静默重试 | 非关键数据同步 |
| `ExceptionAction.Ignore` | 忽略错误，将错误信息返回给回调自行处理 | 需要自定义错误处理的场景 |
| `ExceptionAction.Silence` | 静默发送，无 UI 反馈 | 日志上报、统计 |
| `ExceptionAction.Tips` | 仅提示错误信息，不重试 | 非关键操作 |

---

## 高级使用

### HTTPDNS 配置

启用 HTTPDNS 可以防止 DNS 劫持，提升网络稳定性：

```csharp
// 启用 HttpDNS
HttpNetworkSystem.Instance.EnableHttpDNS = true;

// 设置 HTTPDNS 服务 API 地址
HTTPDNSSystem.Instance.SetAPIHost("your-httpdns-api-host:port");

// 预解析域名（建议在游戏启动时调用）
string[] domains = { "api.example.com", "cdn.example.com" };
HTTPDNSSystem.Instance.ParseHosts(domains, (List<HTTPDNSSystem.Cache> caches, HTTPDNSSystem.EStatus status, string message) => {
    Debug.LogFormat("DNS pre-resolved: {0} hosts, status: {1}", caches.Count, status);
});
```

### UI 适配器绑定

绑定 UI 回调，让网络异常通过 UI 反馈给玩家：

```csharp
// 设置本地化文本回调
HttpNetworkSystem.Instance.UIAdaptor.onGetText = (string text) => {
    return LocalizationSystem.GetText(text);
};

// 设置对话框回调（网络异常时弹出）
HttpNetworkSystem.Instance.UIAdaptor.onDialog = (string title, string msg, bool retry, Action<bool> callback) => {
    UIManager.ShowDialog(title, msg, () => {
        callback(true);   // 用户选择重试
    }, () => {
        callback(false);  // 用户选择取消
    });
};

// 设置等待遮罩回调
HttpNetworkSystem.Instance.UIAdaptor.onWaiting = (bool waiting) => {
    UIManager.ShowLoadingMask(waiting);
};
```

### 日志适配器绑定

将网络异常上报到日志系统：

```csharp
HttpNetworkSystem.Instance.LogAdaptor.onPostNetworkError = (string host, JsonObject param) => {
    string reportJson = SimpleJson.SimpleJson.SerializeObject(param);
    LogSystem.ReportNetworkError(host, reportJson);
};
```

### 请求参数预处理（签名）

启用后，框架会自动为每个请求添加时间戳和签名：

```csharp
HttpNetworkSystem.Instance.EnablePreProcessParam = true;
// 签名密钥通过 NetworkConst.InitEx() 设置
```

### WebSocket 长连接

```csharp
// 连接 WebSocket 服务器
NetworkSystem.Instance.ConnectNetworkClient(
    "game",           // 连接名称
    "127.0.0.1",      // 主机地址
    3014,             // 端口
    (JsonObject result) => {
        if (result["code"].ToString() == "200") {
            Debug.Log("Connected to game server!");
        }
    }
);

// 发送消息
JsonObject msg = new JsonObject();
msg["action"] = "move";
msg["x"] = 100;
msg["y"] = 200;
NetworkSystem.Instance.SendRequest("game", "connector.entryHandler.move", msg, (JsonObject response) => {
    Debug.Log("Server response: " + response.ToString());
});

// 每帧更新网络状态
void Update() {
    NetworkSystem.Instance.OnUpdate();
}

// 断开连接
NetworkSystem.Instance.CloseClient("game");
```

### 文件下载

```csharp
DownloadSystem.Instance.RequestDownload(
    "http://cdn.example.com/asset_bundle_v2.zip",
    Application.persistentDataPath + "/asset_bundle_v2.zip",
    (float progress) => {
        Debug.LogFormat("Download progress: {0}%", progress * 100);
    },
    (int totalSize) => {
        Debug.LogFormat("Total size: {0}", totalSize);
    },
    (string completedUrl) => {
        Debug.Log("Download complete: " + completedUrl);
    }
);
```

---

## 错误处理

框架自动处理以下网络异常：

1. **连接超时** - 根据 `ExceptionAction` 配置自动重试或弹窗提示
2. **服务器错误** - 解析 HTTP 状态码，非 200 响应会触发错误处理
3. **数据解析错误** - JSON 解析失败会提示开发人员修复
4. **业务逻辑错误** - 启用 `CheckLogicErrorCode` 后，会检查响应中的 `code` 字段

```csharp
// 启用业务错误码检查
HttpNetworkSystem.Instance.CheckLogicErrorCode = true;

// 响应格式要求: { "code": 200, "msg": "success", "data": {...} }
```

---

## API 参考

### HttpNetworkSystem
- `GetWebRequest(url, command, exceptionAction, callback)` - 发送 GET 请求
- `PostWebRequest(url, command, param, exceptionAction, callback)` - 发送 POST 请求
- `Token` - 设置/获取 Bearer Token
- `EnableHttpDNS` - 启用 HTTPDNS
- `EnablePreProcessParam` - 启用参数签名预处理
- `CheckLogicErrorCode` - 启用业务错误码检查
- `UIAdaptor` - UI 适配器
- `LogAdaptor` - 日志适配器

### NetworkSystem
- `ConnectNetworkClient(name, host, port, callback)` - 连接 WebSocket
- `SendRequest(name, route, msg, callback)` - 发送消息
- `CloseClient(name)` - 关闭连接
- `OnUpdate()` - 每帧更新

### HTTPDNSSystem
- `SetAPIHost(host)` - 设置 DNS 服务地址
- `ParseHost(domain, callback)` - 解析单个域名
- `ParseHosts(domains, callback)` - 批量解析域名
- `GetURLByIP(url)` - 用缓存的 IP 替换 URL 中的域名
# 使用手册

## 集成
* 复制 /GGNetwork/Assets/Scripts/GGNetwork/代码到你的Unity工程中。

## 初始化

## 基础使用
### HTTP消息请求
Post、Get。

```cs
	// TODO
```

## 高级使用
### HTTPDNS
如果需要更稳定的网络。

```cs
	// TODO
```
