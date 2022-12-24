# GGNetwork

**[内测版]**

一个游戏客户端网络框架，为你的游戏项目提供了网络稳定性和体验增强的能力。

它不是一个网络功能的底层实现，它封装了游戏客户端所需的一些网络网络特性，让游戏的稳定性、体验感更好。并且可以很方便的挂载第三方或者自定义的网络底层系统。

当前版本适用于实时性要求不高的SLG、休闲等类型的游戏。

TODO: 图形化展示
* 游戏业务层 | 交给业务处理
* 游戏网络层 | 异常检查处理
* 网络通信层 | 异常检查处理

## 特性
* 支持UI反馈回调挂载。
* 支持各种异常响应，例如失败后自动重试、手动重试、忽略等。
* 支持断线重连，包括自动重连、手动重连。
* 支持多线程请求，避免网络卡顿对UI产生影响。
* 支持Http DNS。
* 支持网络异常上报。
* 支持Http连接。
* 可以使用内嵌的BestHttp。
* 可以挂载自定义或者其它第三方的HTTP模块。
* [TODO]-支持Socket连接。

## 工程内容
<table>
    <tr><th>目录</th><th>内容</th><th>说明</th></tr>
    <tr>
        <td>GGNetwork/Assets/Scripts/GGNetwork</td>
        <td>框架代码</td>
        <td>可以直接复制到目标工程中使用。</td>
    </tr>
    <tr>
        <td>GGNetwork/Assets/Demo</td>
        <td>演示工程</td>
        <td>可以作为框架使用的参考。</td>
    </tr>
</table>

## 快速手册 
* [如何快速接入](/documents/quickstart.md)

## 进群沟通
* QQ群: 242500383 [![GLTOP游戏研发与技术1群](https://pub.idqqimg.com/wpa/images/group.png)](https://qm.qq.com/cgi-bin/qm/qr?k=fy4Z65nE-5Jd1ay8FkJpDc9iPJyW3d38&jump_from=webapi) 

## TODO-List
* 整理代码，把BestHttp和PomeloClient充分剥离出来。作为可选插件。
