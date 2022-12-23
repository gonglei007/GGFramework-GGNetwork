# GGNetwork

一个游戏客户端网络框架。

它不是一个网络功能的底层实现，它封装了游戏客户端所需的一些网络网络特性，让游戏的稳定性、体验感更好。适用于实时性要求不高的SLG、MMO、休闲等类型的游戏。

## 特性
* 支持UI表现回调。
* 支持各种异常响应，例如失败后自动重试、手动重试、忽略等。
* 支持断线重连，包括自动重连、手动重连。
* 支持多线程连接。
* 支持Http DNS。
* 支持网络异常上报。
* 支持Http连接和Socket连接。

## 工程内容
<table>
    <tr><th>目录</th><th>内容</th></tr>
    <tr>
        <td>Scripts</td>
        <td>代码</td>
    </tr>
    <tr>
        <td>Demo</td>
        <td>演示工程</td>
    </tr>
</table>

## 快速手册 
* [如何快速接入](/documents/quickstart.md)

## 进群沟通
* QQ群: 242500383 [![GLTOP游戏研发与技术1群](https://pub.idqqimg.com/wpa/images/group.png)](https://qm.qq.com/cgi-bin/qm/qr?k=fy4Z65nE-5Jd1ay8FkJpDc9iPJyW3d38&jump_from=webapi) 

## TODO-List
* 整理代码，把BestHttp和PomeloClient充分剥离出来。作为可选插件。
