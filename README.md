# KeyFlow

**此项目由 AI 构建，使用 OpenAI Codex 生成代码、迭代界面、分析兼容协议并准备发布。** 项目由 `piwric77` 维护与发布。代码及测试结果应结合实际设备验证；AI 构建标记不代表已经过全面人工审计。

KeyFlow 是 Windows x64 上的 WinUI 3 按键启动工具，可以为 Copilot 键设置单击、双击和长按动作，并提供 Lenovo SmartKey 消息接口的兼容接收功能。

## 当前版本

`v3.1.0-beta.1` 是兼容测试版本。已完成逻辑测试和界面／消息协议检查，实体 SmartKey 接管与其他 Fn 功能保留仍需逐机验证。请优先在熟悉的设备上测试；不要为本功能停用整个联想 Fn 服务。

## 安装与使用

从本仓库 [Releases](https://github.com/piwric77/KeyFlow/releases) 下载 `KeyFlow-3.1.0-beta.1-Setup-x64.exe`，安装后从开始菜单打开 KeyFlow。安装为当前用户安装，无需管理员权限，包含 .NET 与 Windows App SDK 运行库。

安装前退出旧版 KeyFlow。设置单击、双击与长按目标后点击“保存并生效”。目标可以是网址、程序或快捷方式；默认单击打开 DeepSeek 网页版。

在“偏好设置”可配置隐藏托盘、自启动和外观。隐藏托盘后再次运行 KeyFlow 可找回设置。关闭窗口会保持后台运行，需要停止时使用“退出程序”。

Lenovo SmartKey 的兼容功能默认关闭。启用“联想 SmartKey 兼容接管”并保存，再按实体键检查接收计数与动作；保留原厂 Fn 服务。如果原厂 SmartKey 窗口占用接口，软件会提示冲突，不会结束原厂进程。

## 支持与边界

- 真正的 WinUI 3 导航、按钮、开关和输入控件，支持跟随系统、浅色与深色主题。
- Copilot / F23 和可选 Win + C 路径支持单击、双击、长按。
- 联想兼容路径来自用户提供的 Fn and Function Keys 2.0.13.10 样本的静态分析，仅支持单击／双击；该消息接口没有物理按下／松开信息。
- 部分设备会走 AI Now、XiaoLe 等其他入口，创建接收窗口成功不等于实体键已接管。
- 不包含联想原厂程序，不安装替代驱动、不注入 DLL、不修改固件或 Fn 服务。
- 安装包未进行商业代码签名。下载后可用 Release 附带的 SHA256SUMS.txt 校验文件。

## 卸载与配置

从 Windows“已安装的应用”或开始菜单卸载。卸载前退出 KeyFlow。属于当前安装目录的自启动快捷方式会被移除；用户的 `settings.json` 不随卸载主动删除。卸载后原厂 SmartKey 可继续使用原来的分发路径。

## 构建

需要 .NET 10 SDK、Windows x64、联网下载 NuGet 依赖；构建安装包还需要 Inno Setup 6.7.x。

```powershell
./src/build.ps1 -Output ./publish
```

构建后可运行 `publish/KeyFlow.exe --self-test`。界面检查可使用 `--smoke-test`，该模式会生成预览与测试结果并退出；应先退出其他 KeyFlow 实例。

安装脚本见 `installer/KeyFlow.iss`。原始静态分析结论和使用说明见 [docs](docs)。

## 验证范围

13 项组合测试覆盖手势、配置兼容、隔离目录中的自启动快捷方式，以及兼容窗口发现、消息过滤、冲突检测和释放。另检查了 WinUI 窗口、深浅色渲染、键盘钩子、托盘、暂停恢复和单实例窗口找回。合成消息测试不等于实体设备验收。

第三方运行库的许可与声明随安装包一并保留，见安装目录中的 `ThirdPartyNotices`。
