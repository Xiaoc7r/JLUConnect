# DrMain：只保留对实现有用的分析

## 方法与边界

工具：REA 6.8.0，独立 Node 26.12.1 运行环境。使用 `inspect-desktop-application`、`inspect-native-exports`、`inspect-pe-imports`，对程序副本离线读 PE 头、导入/导出表。未恢复保存的密码，未做完整反编译，也未确认服务器协议、心跳周期或函数 ABI。

原始证据曾包含本机文件路径，因此不直接公开。以下为去除私人路径后的事实摘要。原程序由用户从学校渠道自行获取，本仓库不提供它们。

## 样本基线

| 文件 | 文件版本 | SHA-256 |
|---|---|---|
| DrMain.exe | 5.2.1.20191030 | `03ac3a5ffe33eea45663b454add4527d37c0cc7a617f4d5ed0f3a45e5deb3627` |
| DrClient.exe | 5.2.1.20140928 | `3db4a5deb5555b51c3dbb97cec17fa7c2b2abc902d62b47fa731db867624e9ed` |
| DrAuthSvr.dll | 0.8.44.20161028 | `87ae58789f1030546448d1cad47ff356f92f4b310055dd69855415b42d84653b` |

所检查模块是 32 位 PE。该样本集里 DrAuthSvr.dll 与 drauth_0x.dll 的完整 SHA-256 相同，说明两份文件内容一致，不代表所有安装都这样。

## DrClient → 认证 DLL 的接口边界

DrClient 的导入表通过序号引用 `drauthsvr.dll`。按被分析 DLL 的导出表对应如下：

| 序号 | 导出名 |
|---|---|
| 669 | StartDrcomAuthSvr |
| 671 | StopDrcomAuthSvr |
| 673 | SetAuthSvrParam |
| 675 | SetAuthRulesConfig |
| 676 | SetLocaleMesgPort |
| 677 | CancleBindAuthPort |
| 678 | SetAuthConfig |
| 680 | CustomLanguageString |

这些是文件表中的可观察事实。它们不证明运行时全部被加载，不证明 Start/Stop 等同于登录/注销，也不提供可安全调用的参数定义。当前实现没有加载或调用这些导出。

另一个认证模块 `drauth_1x.dll` 导入 `wpcap.dll`、`packet.dll`；这只能说明有相关依赖，不能断言当前实际协议是 802.1X、需要安装新抓包驱动或所有流量都走该路径。DrMain 导入动态加载及窗口消息函数，也不能单凭这一点假设存在公开控制 API。

## 后续本机窗口观察（不是 REA 反编译结论）

- DrClient 的主状态区域包含 `NetStateWin` 与标准按钮；同一个按钮的文字会在“登录”和“注销”间变化。
- 主界面隐藏到托盘时，整窗不可见不代表断线；需要结合子控件自身样式和状态区域。
- 独立登录窗口中存在外网登录按钮（样本控件 ID 1002）。
- 注销可能弹出确认框。助手主动注销前先持久化暂停，并仅确认识别到的对应窗口。
- 此样本在隐藏状态下直接模拟按钮点击不够稳定；向已识别父窗口发送对应 `WM_COMMAND/BN_CLICKED` 在一次受控观察中触发了官方登录。
- Windows 权限隔离会拒绝低权限程序控制较高权限客户端，因此安装版使用当前用户、最高可用权限的计划任务。

这不是通用 UI 规范。代码按已知文件哈希进行限制，未知版本保守停止；不采用屏幕坐标，也不把未知错误弹窗自动点掉。

## 为什么首版复用官方客户端

静态证据不足以安全重写认证协议。复用官方登录流程可以避免重复保存密码，也无需猜心跳规则。但它仍依赖客户端行为、运行权限和界面结构，无法保证不同版本兼容。

曾直接触发官方登录并观察到在线状态；另一些恢复测试受网络中断和人工/官方恢复影响，不能用来证明完整自动恢复链路。没有完成所有休眠/切网/VPN 场景验证。后续验证应使用隔离测试环境或自然恢复日志，避免主动注销承载开发任务的网络。

## 参考

- [REA 项目](https://github.com/morluto/rea)
- [REA 桌面静态检查](https://github.com/morluto/rea/blob/main/docs/desktop-applications.md)
- [Microsoft：WM_COMMAND](https://learn.microsoft.com/en-us/windows/win32/menurc/wm-command)
- [Microsoft：PostMessageW 与权限隔离](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-postmessagew)
