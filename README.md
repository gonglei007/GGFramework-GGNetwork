<p align="center">
    <h1 align="center">GGNetwork (内测版)</h1>
  <p align="center">
      <a href="https://github.com/gonglei007/GGFramework-GGNetwork/watchers" target="_blank"><img src="https://img.shields.io/github/watchers/gonglei007/GGFramework-GGNetwork.svg" style="display: inherit;"/></a>
      <a href="https://github.com/gonglei007/GGFramework-GGNetwork/stargazers" target="_blank"><img src="https://img.shields.io/github/stars/gonglei007/GGFramework-GGNetwork.svg" style="display: inherit;"/></a>
      <a href="https://github.com/gonglei007/GGFramework-GGNetwork/network/members" target="_blank"><img src="https://img.shields.io/github/forks/gonglei007/GGFramework-GGNetwork.svg" style="display: inherit;"/></a>
      <img src="https://img.shields.io/github/repo-size/gonglei007/GGFramework-GGNetwork.svg" style="display: inherit;"/>
      <a href="https://github.com/gonglei007/GGFramework-GGNetwork/graphs/contributors" target="_blank"><img src="https://img.shields.io/github/contributors/gonglei007/GGFramework-GGNetwork.svg" style="display: inherit;"/></a>
  </p>
    <p align="center">
        一个更适用于游戏客户端的网络框架
    </p>
</p>

![GGNetwork](documents/exports/GGNetwork_TDD.png?raw=true)

## 简介
> 它不是一个网络功能的底层实现，它封装了游戏客户端所需的一些网络特性，让游戏网络的稳定性和体验感更好。并且可以很方便的挂载第三方或者自定义的网络底层模块。

针对不同的业务层级，实现相应的功能。
* 游戏业务层 | 交给游戏业务开发环节处理
* 游戏网络层 | 框架提供异常检查与处理
* 网络通信层 | 框架提供异常检查与处理

### 目标
为游戏客户端提供：
* 更好的网络稳定性
* 更好的网络交互体验

## 功能与特性

* 网络交互体验
    - 支持UI反馈回调挂载，接入简单，当网络连接、请求发生异常或等待的时候，可以获得更好的交互体验。
    - 支持多线程请求，避免网络卡顿对UI产生影响。
    - 支持请求异常响应，例如失败后自动重试、手动重试、忽略等。
    - 支持断线重连，包括自动重连、手动重连。
* 网络质量保障
    - 支持Http DNS，避免玩家端的DNS劫持。
    - 支持网络异常上报，让开发者了解分布各地的玩家的网络状况。
* 第三方支持
    - 支持Http连接。可以使用预置的BestHttp。也可以挂载自定义或者其它第三方的HTTP模块。
    - [TODO]支持Socket连接。可以使用预置的PomeloClient。也可以挂载自定义或者其它第三方的TCP连接模块。

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

## 文档
* [如何快速接入?](/documents/quickstart.md)
* [技术手册](/documents/manual.md)
* 参考文档(TODO)

## 进群沟通
|  |  | |
| --- | -------- | -------- |
| QQ群: | 242500383 | [![GLTOP游戏研发与技术1群](https://pub.idqqimg.com/wpa/images/group.png)](https://qm.qq.com/cgi-bin/qm/qr?k=fy4Z65nE-5Jd1ay8FkJpDc9iPJyW3d38&jump_from=webapi) |
|  |  | |

## 应用案例
当前版本的GGNetwork适用于实时性要求不高的SLG、休闲等类型的游戏。虽然是内测版本，但已经在商业项目中应用。

|    |    |
| --- | ---- |
| [小小三国2](https://play.google.com/store/apps/details?id=com.gltop.wj.xxsg2.tw2) | 一款已经运营了8年的SLG手游。 |
| 游戏化心理评测系统 | 上海某知名高校的游戏化心理评测项目。 |
|    |    |

## TODO-List
* 整理代码，把PomeloClient充分剥离出来。作为可选插件。
* 更完整的Demo演示。

## 更多资料
* [游戏开发图谱](https://github.com/gonglei007/GameDevMind)
  * [客户端网络系统](https://github.com/gonglei007/GameDevMind/blob/main/mds/3.1.4.%E5%AE%A2%E6%88%B7%E7%AB%AF%E7%BD%91%E7%BB%9C%E7%B3%BB%E7%BB%9F.md)
