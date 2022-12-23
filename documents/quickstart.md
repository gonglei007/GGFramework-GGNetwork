# 快速使用
## 1.导入
* 复制GGNetwork目录到你的工程。

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
