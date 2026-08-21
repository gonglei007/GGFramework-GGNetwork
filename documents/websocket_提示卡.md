# GGNetwork 内部 WebSocket + Pomelo 长连接 —— 提示卡

> 一句话：传输层用 `ClientWebSocket`，应用层内化 Pomelo 协议，全部在 `GGFramework.GGNetwork` 命名空间下，零外部库依赖，默认启用，可一键回退 TCP。
> 完整上下文（wire 格式 / 实现细节 / 陷阱 / go-live 检查单）都在 Hermes skill `gg-network-socket` 里。

## 关键提交
- `b7ca7fe` — WebSocket 内部模块（37 文件，1636 行）
- `84711a5` — README 同步更新

## 代码位置
```
GGNetwork/Assets/Scripts/GGNetwork/Socket/
├── WebSocket/                          ← 全部新模块（命名空间 GGFramework.GGNetwork.Socket）
│   ├── WebSocketClient.cs              ← ClientWebSocket 封装（传输层）
│   ├── PackageStream.cs                ← 粘包/拆包
│   ├── WebSocketPomeloClient.cs        ← 综合客户端 + 状态机（对外主入口）
│   ├── IProtocolSession.cs             ← 解耦握手/心跳与客户端
│   ├── HandShakeService.cs             ← 握手
│   ├── HeartBeatService.cs             ← 心跳
│   ├── EventManager.cs                 ← 请求回调 + 推送订阅
│   ├── PackageProtocol.cs / MessageProtocol.cs  ← 帧/消息编解码
│   └── Package.cs Message.cs 各枚举 WsEncoder/WsDecoder
├── Implementation/WebSocketNetworkClient.cs  ← BaseNetworkClient 桥接（业务用这个）
└── IClient.cs                          ← 仅改 1 行：OnConnectExceptionHandler private→protected
```

## 开/关（业务层唯一要动的开关）
```csharp
NetworkSystem.UseWebSocket = true;   // 默认 true = 用内部 WebSocket 实现
NetworkSystem.UseWebSocket = false;  // 回退到外部 TCP 版 PomeloNetworkClient
```

## 业务怎么用（和原来的 Pomelo TCP 用法完全一致）
```csharp
// 取客户端（BaseNetworkClient 类型，推送用 On 需要具体类型/强转）
var client = NetworkSystem.Instance.GetNetworkClient("game");

// 连接：Open(host, port) 内部自动构造 ws://host:port
client.Open("ws://你的服务器", 80);   // 或直接传 host + port
client.onConnected  = rep => Debug.Log("已连接+握手完成");
client.onClose      = _   => Debug.Log("已断开");
client.onError      = _   => Debug.Log("出错（会走自动重连）");

// 请求 / 响应
var req = new NetworkRequest(route, callback, null, msg);
client.Request(req);   // 走请求队列+超时+重连

// 推送订阅（WebSocketNetworkClient.On）
// 强转后调用：((WebSocketNetworkClient)client).On("某路由", data => {...});
```

## ⚠️ 上线前必查（go-live gate）
1. **Unity 编辑器编译验证**（本机无 Unity CLI，未编译过）
2. **服务端必须暴露 ws:// 端点** —— 若后端只收 TCP Pomelo 原始端口，`UseWebSocket=false`
3. **Android/iOS 真机验证**（ClientWebSocket 在 IL2CPP 下）
4. **WebGL 不支持** ClientWebSocket（浏览器强制 JS WebSocket），需 WebGL 再加传输抽象层

## 3 个最容易踩的坑
- **SendAsync 不能并发在途** → 用 `ConcurrentQueue<byte[]> + SemaphoreSlim` 发送循环串行（已实现），别改成每帧 Task.Run
- **WS 消息边界 ≠ Pomelo 包边界** → 必须经 `PackageStream` 缓冲切包（已实现）
- **回调必须在主线程触发** → `WebSocketNetworkClient` 已全部包 `InnerEventTrigger` 转主线程；你自己加回调时也照此办理

## 消息体说明
- **统一走 JSON**（SimpleJson），无 protobuf 依赖
- 若服务端开 protobuf 压缩 → 需在 `MessageProtocol` 扩展 protobuf 分支（注释已留位）
