# GGNetwork

GGFramework框架下的游戏网络框架。提供了游戏客户端所需的网络特性。
适用于实时性要求不高的SLG、MMO、休闲等类型的游戏。

## 特性
* 支持Http连接和Socket连接。
* Http支持失败后自动重试、手动重试、忽略。
* Socket支持断线重连。
* 支持Pomelo Client。
* 支持多线程连接。

## 工程内容
* Scripts: 代码。
* Demo: 演示工程。

## 工程依赖
<div>
	<table>
		<tr>
			<td>
			Best Http
			</td>
			<td>
			</td>
		<tr>
			<td>
			UnityZip
			</td>
			<td>
			</td>
		</tr>
		</tr>
	</table>
</div>

## 快速使用
### 1.导入
* [TODO]复制DLL到游戏工程。

### 2.初始化代码
在合适的地方初始化网络系统。

```
GameNetworkSystem.Instance.Init();
```

### 3.关联UI
绑定UI回调。让网络发生各种反馈的时候，可以反馈给UI。如果不设置，网络的各种情况就不会通过UI表现出来，但不影响网络系统的使用。

```
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

### 4.调用所需网络接口。
```
JsonObject param = new JsonObject();
HttpNetworkSystem.Instance.GetWebRequest("http://url", "command", HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response)=>{
	// 接收json，处理相关逻辑。
});

```

## 进群沟通
* QQ群: 242500383 [![GLTOP游戏研发与技术1群](https://pub.idqqimg.com/wpa/images/group.png)](https://qm.qq.com/cgi-bin/qm/qr?k=fy4Z65nE-5Jd1ay8FkJpDc9iPJyW3d38&jump_from=webapi) 
